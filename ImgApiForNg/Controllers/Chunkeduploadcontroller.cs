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

        private string TempChunksFolder =>
            Path.Combine(_webHostEnvironment.WebRootPath, "chunks_temp");

        private string MergedFilesFolder =>
            Path.Combine(_webHostEnvironment.WebRootPath, "chunked_uploads");

        public ChunkedUploadController(
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment)
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

                // Meta file এ সব তথ্য সংরক্ষণ
                var metaPath = Path.Combine(sessionDir, "_meta.txt");
                await System.IO.File.WriteAllTextAsync(metaPath,
                    $"{request.FileName}|{request.FileType}|{request.FileSize}" +
                    $"|{request.TotalChunks}|{request.UploadMethod}" +
                    $"|{request.ClientStartTimeMs}|{request.StorageType}");

                Console.WriteLine($"[ChunkedUpload] Init: {request.UploadId} | {request.FileName} " +
                                  $"| {request.TotalChunks} chunks | Storage: {request.StorageType}");

                return Ok(new
                {
                    uploadId = request.UploadId,
                    message = "Upload initialized",
                    totalChunks = request.TotalChunks,
                    storageType = request.StorageType
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
            if (!int.TryParse(form["chunkIndex"], out int chunkIndex))
                return BadRequest("Invalid chunkIndex.");
            if (!int.TryParse(form["totalChunks"], out int totalChunks))
                return BadRequest("Invalid totalChunks.");

            var chunkFile = form.Files.GetFile("chunk");
            if (chunkFile == null || chunkFile.Length == 0)
                return BadRequest("Chunk is missing.");
            if (string.IsNullOrWhiteSpace(uploadId))
                return BadRequest("uploadId is required.");

            try
            {
                var sessionDir = GetSessionDir(uploadId);
                if (!Directory.Exists(sessionDir))
                    Directory.CreateDirectory(sessionDir);

                var chunkPath = Path.Combine(sessionDir, $"chunk_{chunkIndex:D4}");
                using var stream = new FileStream(chunkPath, FileMode.Create, FileAccess.Write);
                await chunkFile.CopyToAsync(stream);

                return Ok(new { chunkIndex, totalChunks, message = $"Chunk {chunkIndex + 1}/{totalChunks} received" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Chunk {chunkIndex} failed: {ex.Message}");
            }
        }


        // ===========================================================
        // STEP 3: Finalize — Merge + Store
        // ===========================================================
        [HttpPost("finalize")]
        public async Task<IActionResult> FinalizeUpload([FromBody] FinalizeUploadRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UploadId))
                return BadRequest("UploadId is required.");

            var sessionDir = GetSessionDir(request.UploadId);
            if (!Directory.Exists(sessionDir))
                return BadRequest($"No session found for: {request.UploadId}");

            try
            {
                var chunkFiles = Directory.GetFiles(sessionDir, "chunk_*")
                    .OrderBy(f => f).ToArray();

                if (chunkFiles.Length != request.TotalChunks)
                    return BadRequest($"Expected {request.TotalChunks} chunks, found {chunkFiles.Length}.");

                // ===== Chunks কে একটি byte[] এ merge করুন =====
                using var mergedStream = new MemoryStream();
                foreach (var chunkPath in chunkFiles)
                {
                    using var cs = new FileStream(chunkPath, FileMode.Open, FileAccess.Read);
                    await cs.CopyToAsync(mergedStream);
                }
                var fileBytes = mergedStream.ToArray();
                var actualSize = fileBytes.Length;

                // Temp directory মুছুন
                Directory.Delete(sessionDir, recursive: true);

                // Duration
                long durationMs = request.ClientStartTimeMs > 0
                    ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - request.ClientStartTimeMs
                    : 0;

                var storageType = string.IsNullOrWhiteSpace(request.StorageType)
                    ? "Folder" : request.StorageType;

                var uploadMethod = string.IsNullOrWhiteSpace(request.UploadMethod)
                    ? "LocalFile" : request.UploadMethod;

                var record = new ChunkedFileRecord
                {
                    FileName = request.FileName,
                    FileType = request.FileType,
                    FileSize = FormatFileSize(actualSize),
                    RawFileSize = actualSize,
                    TotalChunks = request.TotalChunks,
                    UploadId = request.UploadId,
                    UploadedAt = DateTime.UtcNow,
                    UploadMethod = uploadMethod,
                    UploadDurationMs = durationMs,
                    StorageType = storageType,
                    StorageSwitchedAt = null,
                    PreviousStorageType = null,
                    DownloadToken = string.Empty,
                    DownloadTokenExpiration = null
                };

                // ===== Storage type অনুযায়ী সংরক্ষণ =====
                if (storageType == "Database")
                {
                    // Database এ byte[] হিসেবে store
                    record.FileData = fileBytes;
                    record.FileUrl = null;
                    Console.WriteLine($"[ChunkedUpload] Stored in DATABASE | Size: {FormatFileSize(actualSize)}");
                }
                else
                {
                    // Folder এ file হিসেবে store
                    record.FileUrl = await SaveBytesToFolderAsync(fileBytes, request.FileName);
                    record.FileData = null;
                    Console.WriteLine($"[ChunkedUpload] Stored in FOLDER | Path: {record.FileUrl}");
                }

                _context.ChunkedFileRecords.Add(record);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[ChunkedUpload] Saved | Id: {record.Id} | Method: {uploadMethod} | Duration: {FormatDuration(durationMs)}");

                return Ok(new
                {
                    id = record.Id,
                    fileName = record.FileName,
                    fileSize = record.FileSize,
                    storageType = record.StorageType,
                    uploadMethod = record.UploadMethod,
                    uploadDuration = FormatDuration(durationMs),
                    message = "File uploaded successfully!"
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

            var sw = Stopwatch.StartNew();

            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(30);

                using var response = await httpClient.GetAsync(
                    request.Url, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                    return BadRequest($"Failed to reach URL. Status: {response.StatusCode}");

                var fileName = GetFileNameFromUrl(request.Url, response);
                var fileType = response.Content.Headers.ContentType?.MediaType
                               ?? "application/octet-stream";

                // MemoryStream এ download করুন
                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var ms = new MemoryStream();
                var buffer = new byte[81920];
                int bytesRead;
                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    await ms.WriteAsync(buffer, 0, bytesRead);

                var fileBytes = ms.ToArray();
                sw.Stop();

                long durationMs = request.ClientStartTimeMs > 0
                    ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - request.ClientStartTimeMs
                    : sw.ElapsedMilliseconds;

                var storageType = string.IsNullOrWhiteSpace(request.StorageType)
                    ? "Folder" : request.StorageType;

                var uploadMethod = string.IsNullOrWhiteSpace(request.UploadMethod)
                    ? "UrlBackend" : request.UploadMethod;

                var record = new ChunkedFileRecord
                {
                    FileName = fileName,
                    FileType = fileType,
                    FileSize = FormatFileSize(fileBytes.Length),
                    RawFileSize = fileBytes.Length,
                    TotalChunks = 1,
                    UploadId = "url-" + Guid.NewGuid().ToString("N")[..8],
                    UploadedAt = DateTime.UtcNow,
                    UploadMethod = uploadMethod,
                    UploadDurationMs = durationMs,
                    StorageType = storageType,
                    StorageSwitchedAt = null,
                    PreviousStorageType = null,
                    DownloadToken = string.Empty,
                    DownloadTokenExpiration = null
                };

                if (storageType == "Database")
                {
                    record.FileData = fileBytes;
                    record.FileUrl = null;
                }
                else
                {
                    record.FileUrl = await SaveBytesToFolderAsync(fileBytes, fileName);
                    record.FileData = null;
                }

                _context.ChunkedFileRecords.Add(record);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    id = record.Id,
                    fileName = record.FileName,
                    fileSize = record.FileSize,
                    storageType = record.StorageType,
                    uploadMethod = record.UploadMethod,
                    uploadDuration = FormatDuration(durationMs),
                    message = "File downloaded from URL and saved!"
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
        // ✅ STORAGE SWITCH — এক ক্লিকে Folder ↔ Database
        // POST /api/chunkedupload/switch-storage/{id}
        // ===========================================================
        [HttpPost("switch-storage/{id}")]
        public async Task<IActionResult> SwitchStorage(int id, [FromBody] SwitchStorageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TargetStorage))
                return BadRequest("TargetStorage is required. Use 'Folder' or 'Database'.");

            if (request.TargetStorage != "Folder" && request.TargetStorage != "Database")
                return BadRequest("TargetStorage must be 'Folder' or 'Database'.");

            var record = await _context.ChunkedFileRecords.FindAsync(id);
            if (record == null)
                return NotFound($"File with id {id} not found.");

            if (record.StorageType == request.TargetStorage)
                return Ok(new { message = $"Already stored in {request.TargetStorage}. No change needed." });

            try
            {
                // ===== Folder → Database =====
                if (request.TargetStorage == "Database")
                {
                    if (string.IsNullOrEmpty(record.FileUrl))
                        return BadRequest("File URL is missing. Cannot read from folder.");

                    var filePath = Path.Combine(
                        _webHostEnvironment.WebRootPath,
                        record.FileUrl.TrimStart('/'));

                    if (!System.IO.File.Exists(filePath))
                        return NotFound("File not found in folder.");

                    // Folder থেকে পড়ুন → DB তে রাখুন
                    var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

                    // Folder থেকে মুছুন
                    System.IO.File.Delete(filePath);
                    Console.WriteLine($"[StorageSwitch] Deleted from folder: {filePath}");

                    // DB তে store করুন
                    record.FileData = fileBytes;
                    record.PreviousStorageType = record.StorageType;
                    record.StorageType = "Database";
                    record.FileUrl = null;
                    record.StorageSwitchedAt = DateTime.UtcNow;

                    Console.WriteLine($"[StorageSwitch] Id:{id} | Folder → Database | Size: {FormatFileSize(fileBytes.Length)}");
                }
                // ===== Database → Folder =====
                else
                {
                    if (record.FileData == null || record.FileData.Length == 0)
                        return BadRequest("File data is missing in database.");

                    // DB থেকে পড়ুন → Folder এ রাখুন
                    var fileUrl = await SaveBytesToFolderAsync(record.FileData, record.FileName);

                    // DB থেকে binary data মুছুন (memory বাঁচাতে)
                    record.FileUrl = fileUrl;
                    record.FileData = null;
                    record.PreviousStorageType = record.StorageType;
                    record.StorageType = "Folder";
                    record.StorageSwitchedAt = DateTime.UtcNow;

                    Console.WriteLine($"[StorageSwitch] Id:{id} | Database → Folder | Path: {fileUrl}");
                }

                _context.Entry(record).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    id = record.Id,
                    previousStorage = record.PreviousStorageType,
                    currentStorage = record.StorageType,
                    switchedAt = record.StorageSwitchedAt?.ToString("yyyy-MM-dd HH:mm:ss") + " UTC",
                    message = $"Storage switched: {record.PreviousStorageType} → {record.StorageType}"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StorageSwitch] Error: {ex.Message}");
                return StatusCode(500, $"Storage switch failed: {ex.Message}");
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
                    base64 = await GetBase64Async(r);

                dtos.Add(MapToDTO(r, base64));
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
                base64 = await GetBase64Async(r);

            return Ok(MapToDTO(r, base64));
        }


        // ===========================================================
        // Download File
        // ===========================================================
        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadById(int id)
        {
            var record = await _context.ChunkedFileRecords.FindAsync(id);
            if (record == null) return NotFound();

            byte[] bytes;

            if (record.StorageType == "Database")
            {
                if (record.FileData == null || record.FileData.Length == 0)
                    return NotFound("File data not found in database.");
                bytes = record.FileData;
            }
            else
            {
                if (string.IsNullOrEmpty(record.FileUrl))
                    return NotFound("File URL is missing.");
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, record.FileUrl.TrimStart('/'));
                if (!System.IO.File.Exists(filePath))
                    return NotFound("File not found on disk.");
                bytes = await System.IO.File.ReadAllBytesAsync(filePath);
            }

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

            var link = $"{Request.Scheme}://{Request.Host}/api/chunkedupload/download-by-token/{token}/{encodedFileName}";
            return Ok(new { downloadLink = link });
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

            byte[] bytes;
            if (record.StorageType == "Database")
            {
                if (record.FileData == null) return NotFound("File data not found.");
                bytes = record.FileData;
            }
            else
            {
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, record.FileUrl!.TrimStart('/'));
                if (!System.IO.File.Exists(filePath)) return NotFound("File not found.");
                bytes = await System.IO.File.ReadAllBytesAsync(filePath);
            }

            return File(bytes, record.FileType, fileName);
        }


        // ===========================================================
        // DELETE
        // ===========================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var record = await _context.ChunkedFileRecords.FindAsync(id);
            if (record == null) return NotFound();

            // Folder এ থাকলে disk থেকে মুছুন
            if (record.StorageType == "Folder" && !string.IsNullOrEmpty(record.FileUrl))
            {
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, record.FileUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }

            _context.ChunkedFileRecords.Remove(record);
            await _context.SaveChangesAsync();
            return NoContent();
        }


        // ===========================================================
        // Private Helpers
        // ===========================================================

        private string GetSessionDir(string uploadId)
        {
            var safeId = string.Concat(
                uploadId.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));
            return Path.Combine(TempChunksFolder, safeId);
        }

        private async Task<string> SaveBytesToFolderAsync(byte[] fileBytes, string fileName)
        {
            if (!Directory.Exists(MergedFilesFolder))
                Directory.CreateDirectory(MergedFilesFolder);

            var uniqueName = Guid.NewGuid().ToString("N") + "_" + SanitizeFileName(fileName);
            var outputPath = Path.Combine(MergedFilesFolder, uniqueName);
            await System.IO.File.WriteAllBytesAsync(outputPath, fileBytes);
            return "/chunked_uploads/" + uniqueName;
        }

        private async Task<string?> GetBase64Async(ChunkedFileRecord r)
        {
            try
            {
                if (r.StorageType == "Database")
                {
                    if (r.FileData == null) return null;
                    return Convert.ToBase64String(r.FileData);
                }
                else
                {
                    if (string.IsNullOrEmpty(r.FileUrl)) return null;
                    var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, r.FileUrl.TrimStart('/'));
                    if (!System.IO.File.Exists(fullPath)) return null;
                    var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
                    return Convert.ToBase64String(bytes);
                }
            }
            catch { return null; }
        }

        private static ChunkedFileRecordDTO MapToDTO(ChunkedFileRecord r, string? base64)
        {
            return new ChunkedFileRecordDTO
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
                StorageType = r.StorageType,
                PreviousStorageType = r.PreviousStorageType,
                StorageSwitchedAt = r.StorageSwitchedAt.HasValue
                    ? r.StorageSwitchedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") + " UTC"
                    : null,
                Filestring = base64
            };
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

        private static string FormatDuration(long ms)
        {
            if (ms <= 0) return "—";
            if (ms < 1000) return $"{ms}ms";
            var s = ms / 1000.0;
            if (s < 60) return $"{Math.Round(s, 1)}s";
            var m = (int)(s / 60);
            var sec = (int)(s % 60);
            if (m < 60) return $"{m}m {sec}s";
            var h = m / 60;
            return $"{h}h {m % 60}m {sec}s";
        }

        private static string GetFileNameFromUrl(string url, HttpResponseMessage response)
        {
            var cd = response.Content.Headers.ContentDisposition;
            if (cd?.FileName != null)
                return SanitizeFileName(cd.FileName.Trim('"'));
            try
            {
                var seg = new Uri(url).Segments.LastOrDefault() ?? "file";
                var dec = Uri.UnescapeDataString(seg);
                if (!string.IsNullOrWhiteSpace(dec) && dec != "/")
                    return SanitizeFileName(dec);
            }
            catch { }
            return "file-" + DateTime.UtcNow.Ticks;
        }
    }
}