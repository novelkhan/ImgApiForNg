using ImgApiForNg.Data;
using ImgApiForNg.DTOs.ChunkedUpload;
using ImgApiForNg.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace ImgApiForNg.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChunkedUploadController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        private string TempChunksFolder => Path.Combine(_webHostEnvironment.WebRootPath, "chunks_temp");
        private string MergedFilesFolder => Path.Combine(_webHostEnvironment.WebRootPath, "chunked_uploads");

        public ChunkedUploadController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }


        // ===========================================================
        // STEP 1: Initialize Upload
        // ===========================================================
        [HttpPost("initialize")]
        public async Task<IActionResult> InitializeUpload([FromBody] InitializeUploadRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UploadId))
                return BadRequest("UploadId is required.");
            if (string.IsNullOrWhiteSpace(request.FileName))
                return BadRequest("FileName is required.");
            if (request.TotalChunks <= 0)
                return BadRequest("TotalChunks must be greater than 0.");

            try
            {
                var sessionDir = GetSessionDir(request.UploadId);
                if (!Directory.Exists(sessionDir))
                    Directory.CreateDirectory(sessionDir);

                // Meta file এ uploadMethod ও clientStartTime সেভ করুন
                var metaPath = Path.Combine(sessionDir, "_meta.txt");
                await System.IO.File.WriteAllTextAsync(metaPath,
                    $"{request.FileName}|{request.FileType}|{request.FileSize}|{request.TotalChunks}|{request.UploadMethod}|{request.ClientStartTimeMs}");

                Console.WriteLine($"[ChunkedUpload] Initialized: {request.UploadId} | {request.FileName} | {request.TotalChunks} chunks | Method: {request.UploadMethod}");

                return Ok(new
                {
                    uploadId = request.UploadId,
                    message = "Upload initialized successfully",
                    totalChunks = request.TotalChunks
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Initialization failed: {ex.Message}");
            }
        }


        // ===========================================================
        // STEP 2: Upload Chunk
        // ===========================================================
        [HttpPost("upload-chunk")]
        [RequestSizeLimit(52_428_800)]
        public async Task<IActionResult> UploadChunk([FromForm] IFormCollection form)
        {
            var uploadId = form["uploadId"].ToString();
            var fileName = form["fileName"].ToString();

            if (!int.TryParse(form["chunkIndex"], out int chunkIndex))
                return BadRequest("Invalid chunkIndex.");
            if (!int.TryParse(form["totalChunks"], out int totalChunks))
                return BadRequest("Invalid totalChunks.");

            var chunkFile = form.Files.GetFile("chunk");
            if (chunkFile == null || chunkFile.Length == 0)
                return BadRequest("Chunk file is missing or empty.");
            if (string.IsNullOrWhiteSpace(uploadId))
                return BadRequest("uploadId is required.");

            try
            {
                var sessionDir = GetSessionDir(uploadId);
                if (!Directory.Exists(sessionDir))
                    Directory.CreateDirectory(sessionDir);

                var chunkPath = Path.Combine(sessionDir, $"chunk_{chunkIndex:D4}");
                using (var stream = new FileStream(chunkPath, FileMode.Create, FileAccess.Write))
                {
                    await chunkFile.CopyToAsync(stream);
                }

                Console.WriteLine($"[ChunkedUpload] Chunk {chunkIndex + 1}/{totalChunks} saved | UploadId: {uploadId}");

                return Ok(new
                {
                    chunkIndex,
                    totalChunks,
                    message = $"Chunk {chunkIndex + 1} of {totalChunks} received"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Chunk upload failed at index {chunkIndex}: {ex.Message}");
            }
        }


        // ===========================================================
        // STEP 3: Finalize — Merge Chunks
        // ===========================================================
        [HttpPost("finalize")]
        public async Task<IActionResult> FinalizeUpload([FromBody] FinalizeUploadRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UploadId))
                return BadRequest("UploadId is required.");

            var sessionDir = GetSessionDir(request.UploadId);
            if (!Directory.Exists(sessionDir))
                return BadRequest($"No upload session found for uploadId: {request.UploadId}");

            try
            {
                var chunkFiles = Directory.GetFiles(sessionDir, "chunk_*")
                    .OrderBy(f => f)
                    .ToArray();

                if (chunkFiles.Length != request.TotalChunks)
                    return BadRequest($"Expected {request.TotalChunks} chunks but found {chunkFiles.Length}.");

                if (!Directory.Exists(MergedFilesFolder))
                    Directory.CreateDirectory(MergedFilesFolder);

                var uniqueFileName = Guid.NewGuid().ToString("N") + "_" + SanitizeFileName(request.FileName);
                var mergedFilePath = Path.Combine(MergedFilesFolder, uniqueFileName);
                var fileUrl = "/chunked_uploads/" + uniqueFileName;

                // Merge chunks
                var mergeStopwatch = Stopwatch.StartNew();
                using (var outputStream = new FileStream(mergedFilePath, FileMode.Create, FileAccess.Write))
                {
                    foreach (var chunkPath in chunkFiles)
                    {
                        using var chunkStream = new FileStream(chunkPath, FileMode.Open, FileAccess.Read);
                        await chunkStream.CopyToAsync(outputStream);
                    }
                }
                mergeStopwatch.Stop();

                var actualSize = new FileInfo(mergedFilePath).Length;

                // Duration হিসাব করুন
                // Client থেকে start time পাঠালে সেটা ব্যবহার করি, নাহলে server এর merge time
                long durationMs;
                if (request.ClientStartTimeMs > 0)
                {
                    var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    durationMs = nowMs - request.ClientStartTimeMs;
                }
                else
                {
                    durationMs = mergeStopwatch.ElapsedMilliseconds;
                }

                // Temp directory মুছুন
                Directory.Delete(sessionDir, recursive: true);

                // Meta file থেকে uploadMethod পড়ুন (fallback: request থেকে)
                var uploadMethod = string.IsNullOrWhiteSpace(request.UploadMethod)
                    ? "LocalFile"
                    : request.UploadMethod;

                // DB তে save করুন
                var record = new ChunkedFileRecord
                {
                    FileName = request.FileName,
                    FileType = request.FileType,
                    FileSize = FormatFileSize(actualSize),
                    RawFileSize = actualSize,
                    FileUrl = fileUrl,
                    TotalChunks = request.TotalChunks,
                    UploadId = request.UploadId,
                    UploadedAt = DateTime.UtcNow,
                    UploadMethod = uploadMethod,
                    UploadDurationMs = durationMs,
                    DownloadToken = string.Empty,
                    DownloadTokenExpiration = null
                };

                _context.ChunkedFileRecords.Add(record);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[ChunkedUpload] Finalized | Id: {record.Id} | Method: {uploadMethod} | Duration: {FormatDuration(durationMs)}");

                return Ok(new
                {
                    id = record.Id,
                    fileName = record.FileName,
                    fileSize = record.FileSize,
                    uploadMethod = record.UploadMethod,
                    uploadDuration = FormatDuration(durationMs),
                    uploadDurationMs = durationMs,
                    message = "File uploaded and assembled successfully!"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChunkedUpload] Finalize error: {ex.Message}");
                return StatusCode(500, $"Finalize failed: {ex.Message}");
            }
        }


        // ===========================================================
        // URL → Backend Direct Download
        // ===========================================================
        [HttpPost("upload-from-url")]
        public async Task<IActionResult> UploadFromUrl([FromBody] ChunkedUrlUploadRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Url))
                return BadRequest("URL is required.");

            var serverStopwatch = Stopwatch.StartNew();

            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(30);

                using var response = await httpClient.GetAsync(request.Url, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                    return BadRequest($"Failed to reach URL. Status: {response.StatusCode}");

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var fileName = GetFileNameFromUrl(request.Url, response);
                var fileType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

                if (!Directory.Exists(MergedFilesFolder))
                    Directory.CreateDirectory(MergedFilesFolder);

                var uniqueFileName = Guid.NewGuid().ToString("N") + "_" + SanitizeFileName(fileName);
                var outputPath = Path.Combine(MergedFilesFolder, uniqueFileName);
                var fileUrl = "/chunked_uploads/" + uniqueFileName;

                long totalRead = 0;
                var buffer = new byte[81920];
                int lastReportedProgress = -1;

                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);

                int bytesRead;
                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalRead += bytesRead;

                    if (totalBytes > 0)
                    {
                        int progress = (int)((totalRead * 100) / totalBytes);
                        if (progress != lastReportedProgress)
                        {
                            lastReportedProgress = progress;
                            Console.WriteLine($"[ChunkedUpload] URL Download: {progress}%");
                        }
                    }
                }

                serverStopwatch.Stop();

                // Duration হিসাব
                long durationMs;
                if (request.ClientStartTimeMs > 0)
                {
                    var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    durationMs = nowMs - request.ClientStartTimeMs;
                }
                else
                {
                    durationMs = serverStopwatch.ElapsedMilliseconds;
                }

                var uploadMethod = string.IsNullOrWhiteSpace(request.UploadMethod)
                    ? "UrlBackend"
                    : request.UploadMethod;

                var record = new ChunkedFileRecord
                {
                    FileName = fileName,
                    FileType = fileType,
                    FileSize = FormatFileSize(totalRead),
                    RawFileSize = totalRead,
                    FileUrl = fileUrl,
                    TotalChunks = 1,
                    UploadId = "url-direct-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                    UploadedAt = DateTime.UtcNow,
                    UploadMethod = uploadMethod,
                    UploadDurationMs = durationMs,
                    DownloadToken = string.Empty,
                    DownloadTokenExpiration = null
                };

                _context.ChunkedFileRecords.Add(record);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[ChunkedUpload] URL Download complete | Id: {record.Id} | Duration: {FormatDuration(durationMs)}");

                return Ok(new
                {
                    id = record.Id,
                    fileName = record.FileName,
                    fileSize = record.FileSize,
                    uploadMethod = record.UploadMethod,
                    uploadDuration = FormatDuration(durationMs),
                    uploadDurationMs = durationMs,
                    message = "File downloaded from URL and saved successfully!"
                });
            }
            catch (TaskCanceledException)
            {
                return StatusCode(504, "Download timed out.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Server error: {ex.Message}");
            }
        }


        // ===========================================================
        // GET All Files
        // ===========================================================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ChunkedFileRecordDTO>>> GetAll()
        {
            var records = await _context.ChunkedFileRecords
                .OrderByDescending(r => r.UploadedAt)
                .ToListAsync();

            var dtos = new List<ChunkedFileRecordDTO>();

            foreach (var r in records)
            {
                string? base64 = null;
                if (r.FileType.StartsWith("image/"))
                    base64 = await GetFileBase64Async(r.FileUrl);

                dtos.Add(new ChunkedFileRecordDTO
                {
                    Id = r.Id,
                    Filename = r.FileName,
                    Filetype = r.FileType,
                    Filesize = r.FileSize,
                    RawFileSize = r.RawFileSize,
                    TotalChunks = r.TotalChunks,
                    UploadId = r.UploadId,
                    UploadedAt = r.UploadedAt.ToString("yyyy-MM-dd HH:mm:ss") + " UTC",
                    UploadMethod = r.UploadMethod,
                    UploadDurationMs = r.UploadDurationMs,
                    UploadDuration = FormatDuration(r.UploadDurationMs),
                    Filestring = base64
                });
            }

            return Ok(dtos);
        }


        // ===========================================================
        // GET Single File
        // ===========================================================
        [HttpGet("{id}")]
        public async Task<ActionResult<ChunkedFileRecordDTO>> GetById(int id)
        {
            var r = await _context.ChunkedFileRecords.FindAsync(id);
            if (r == null) return NotFound();

            string? base64 = null;
            if (r.FileType.StartsWith("image/"))
                base64 = await GetFileBase64Async(r.FileUrl);

            return Ok(new ChunkedFileRecordDTO
            {
                Id = r.Id,
                Filename = r.FileName,
                Filetype = r.FileType,
                Filesize = r.FileSize,
                RawFileSize = r.RawFileSize,
                TotalChunks = r.TotalChunks,
                UploadId = r.UploadId,
                UploadedAt = r.UploadedAt.ToString("yyyy-MM-dd HH:mm:ss") + " UTC",
                UploadMethod = r.UploadMethod,
                UploadDurationMs = r.UploadDurationMs,
                UploadDuration = FormatDuration(r.UploadDurationMs),
                Filestring = base64
            });
        }


        // ===========================================================
        // Download File
        // ===========================================================
        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadById(int id)
        {
            var record = await _context.ChunkedFileRecords.FindAsync(id);
            if (record == null) return NotFound();

            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, record.FileUrl.TrimStart('/'));
            if (!System.IO.File.Exists(filePath)) return NotFound("File not found on disk.");

            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(bytes, record.FileType, record.FileName);
        }


        // ===========================================================
        // Generate Expiring Download Link
        // ===========================================================
        [HttpPost("generate-download-link/{id}")]
        public async Task<IActionResult> GenerateDownloadLink(int id)
        {
            var record = await _context.ChunkedFileRecords.FindAsync(id);
            if (record == null) return NotFound();

            var token = Guid.NewGuid().ToString("N");
            var encodedFileName = Uri.EscapeDataString(record.FileName);

            record.DownloadToken = token;
            record.DownloadTokenExpiration = DateTime.UtcNow.AddHours(8);
            _context.Entry(record).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            var downloadLink = $"{Request.Scheme}://{Request.Host}/api/chunkedupload/download-by-token/{token}/{encodedFileName}";
            return Ok(new { downloadLink });
        }


        // ===========================================================
        // Download by Token
        // ===========================================================
        [HttpGet("download-by-token/{token}/{encodedFileName}")]
        public async Task<IActionResult> DownloadByToken(string token, string encodedFileName)
        {
            var fileName = HttpUtility.UrlDecode(encodedFileName);
            var record = await _context.ChunkedFileRecords
                .FirstOrDefaultAsync(r => r.DownloadToken == token);

            if (record == null || record.DownloadTokenExpiration < DateTime.UtcNow)
                return NotFound("Download link is invalid or expired.");

            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, record.FileUrl.TrimStart('/'));
            if (!System.IO.File.Exists(filePath)) return NotFound("File not found.");

            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(bytes, record.FileType, fileName);
        }


        // ===========================================================
        // DELETE File
        // ===========================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var record = await _context.ChunkedFileRecords.FindAsync(id);
            if (record == null) return NotFound();

            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, record.FileUrl.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

            _context.ChunkedFileRecords.Remove(record);
            await _context.SaveChangesAsync();

            return NoContent();
        }


        // ===========================================================
        // Private Helpers
        // ===========================================================
        private string GetSessionDir(string uploadId)
        {
            var safeId = string.Concat(uploadId.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));
            return Path.Combine(TempChunksFolder, safeId);
        }

        private static string SanitizeFileName(string fileName)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return string.Concat(fileName.Select(c => invalid.Contains(c) ? '_' : c));
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{Math.Round(bytes / 1024.0, 2)} KBs";
            if (bytes < 1024L * 1024 * 1024) return $"{Math.Round(bytes / (1024.0 * 1024), 2)} MBs";
            return $"{Math.Round(bytes / (1024.0 * 1024 * 1024), 2)} GBs";
        }

        // ✅ Duration কে human readable format এ রূপান্তর
        private static string FormatDuration(long ms)
        {
            if (ms <= 0) return "—";
            if (ms < 1000) return $"{ms}ms";

            var totalSeconds = ms / 1000.0;
            if (totalSeconds < 60) return $"{Math.Round(totalSeconds, 1)}s";

            var minutes = (int)(totalSeconds / 60);
            var seconds = (int)(totalSeconds % 60);
            if (minutes < 60) return $"{minutes}m {seconds}s";

            var hours = (int)(minutes / 60);
            var mins = minutes % 60;
            return $"{hours}h {mins}m {seconds}s";
        }

        private async Task<string?> GetFileBase64Async(string fileUrl)
        {
            try
            {
                var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, fileUrl.TrimStart('/'));
                if (!System.IO.File.Exists(fullPath)) return null;
                var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
                return Convert.ToBase64String(bytes);
            }
            catch { return null; }
        }

        private static string GetFileNameFromUrl(string url, HttpResponseMessage response)
        {
            var contentDisposition = response.Content.Headers.ContentDisposition;
            if (contentDisposition?.FileName != null)
                return SanitizeFileName(contentDisposition.FileName.Trim('"'));

            try
            {
                var uri = new Uri(url);
                var segment = uri.Segments.LastOrDefault() ?? "file";
                var decoded = Uri.UnescapeDataString(segment);
                if (!string.IsNullOrWhiteSpace(decoded) && decoded != "/")
                    return SanitizeFileName(decoded);
            }
            catch { }

            return "downloaded-file-" + DateTime.UtcNow.Ticks;
        }
    }
}