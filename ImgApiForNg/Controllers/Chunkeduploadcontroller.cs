using ImgApiForNg.Data;
using ImgApiForNg.DTOs.ChunkedUpload;
using ImgApiForNg.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace ImgApiForNg.Controllers
{
    /// <summary>
    /// Chunked Upload Controller
    ///
    /// ফাইল আপলোডের তিনটি ধাপ:
    ///   1. POST /chunkedupload/initialize     — upload session শুরু করুন
    ///   2. POST /chunkedupload/upload-chunk   — প্রতিটি chunk পাঠান (multipart/form-data)
    ///   3. POST /chunkedupload/finalize       — সব chunk merge করুন
    ///
    /// অন্যান্য:
    ///   GET  /chunkedupload                  — সব ফাইলের তালিকা
    ///   GET  /chunkedupload/{id}             — একটি ফাইলের তথ্য (base64 সহ)
    ///   GET  /chunkedupload/download/{id}    — ফাইল সরাসরি ডাউনলোড
    ///   POST /chunkedupload/generate-download-link/{id} — expiring link তৈরি
    ///   GET  /chunkedupload/download-by-token/{token}/{filename}
    ///   DELETE /chunkedupload/{id}           — ফাইল মুছুন
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ChunkedUploadController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        // চাংক ফাইলগুলো temp ফোল্ডারে রাখা হবে
        private string TempChunksFolder => Path.Combine(_webHostEnvironment.WebRootPath, "chunks_temp");
        // মার্জ করা ফাইল এখানে রাখা হবে
        private string MergedFilesFolder => Path.Combine(_webHostEnvironment.WebRootPath, "chunked_uploads");

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
        /// <summary>
        /// Upload session initialize করুন।
        /// Server temp directory তৈরি করে রাখে।
        /// </summary>
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
                // এই upload session এর জন্য temp directory তৈরি করুন
                var sessionDir = GetSessionDir(request.UploadId);
                if (!Directory.Exists(sessionDir))
                    Directory.CreateDirectory(sessionDir);

                // Session metadata একটি file এ লিখে রাখুন (optional, for verification)
                var metaPath = Path.Combine(sessionDir, "_meta.txt");
                await System.IO.File.WriteAllTextAsync(metaPath,
                    $"{request.FileName}|{request.FileType}|{request.FileSize}|{request.TotalChunks}");

                Console.WriteLine($"[ChunkedUpload] Initialized: {request.UploadId} | {request.FileName} | {request.TotalChunks} chunks");

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
        /// <summary>
        /// একটি করে chunk পাঠান। multipart/form-data হিসেবে পাঠাতে হবে।
        /// Form fields: uploadId, chunkIndex, totalChunks, fileName, chunk (file)
        /// </summary>
        [HttpPost("upload-chunk")]
        [RequestSizeLimit(52_428_800)] // 50MB max per chunk (safety)
        public async Task<IActionResult> UploadChunk([FromForm] IFormCollection form)
        {
            // Form data parse করুন
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

                // Session directory অবশ্যই থাকতে হবে (initialize এ তৈরি হয়েছে)
                if (!Directory.Exists(sessionDir))
                {
                    // initialize না করলেও চলে, auto-create
                    Directory.CreateDirectory(sessionDir);
                }

                // Chunk ফাইল সংরক্ষণ করুন: chunk_0000, chunk_0001, ...
                var chunkFileName = $"chunk_{chunkIndex:D4}";
                var chunkPath = Path.Combine(sessionDir, chunkFileName);

                using (var stream = new FileStream(chunkPath, FileMode.Create, FileAccess.Write))
                {
                    await chunkFile.CopyToAsync(stream);
                }

                Console.WriteLine($"[ChunkedUpload] Chunk {chunkIndex + 1}/{totalChunks} saved | UploadId: {uploadId}");

                return Ok(new
                {
                    chunkIndex = chunkIndex,
                    totalChunks = totalChunks,
                    message = $"Chunk {chunkIndex + 1} of {totalChunks} received"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Chunk upload failed at index {chunkIndex}: {ex.Message}");
            }
        }


        // ===========================================================
        // STEP 3: Finalize (Merge Chunks)
        // ===========================================================
        /// <summary>
        /// সব chunk এসে গেলে এই endpoint call করুন।
        /// Server সব chunk merge করে একটি ফাইল তৈরি করে।
        /// ডেটাবেজে record সেভ হয়।
        /// </summary>
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
                // সব chunk ফাইল খুঁজুন এবং সংখ্যা যাচাই করুন
                var chunkFiles = Directory.GetFiles(sessionDir, "chunk_*")
                    .OrderBy(f => f)
                    .ToArray();

                if (chunkFiles.Length != request.TotalChunks)
                {
                    return BadRequest(
                        $"Expected {request.TotalChunks} chunks but found {chunkFiles.Length}. " +
                        "Please re-upload missing chunks.");
                }

                // Merged file এর জন্য output path তৈরি করুন
                if (!Directory.Exists(MergedFilesFolder))
                    Directory.CreateDirectory(MergedFilesFolder);

                var uniqueFileName = Guid.NewGuid().ToString("N") + "_" + SanitizeFileName(request.FileName);
                var mergedFilePath = Path.Combine(MergedFilesFolder, uniqueFileName);
                var fileUrl = "/chunked_uploads/" + uniqueFileName;

                // Chunks একে একে merge করুন
                Console.WriteLine($"[ChunkedUpload] Merging {chunkFiles.Length} chunks → {mergedFilePath}");

                using (var outputStream = new FileStream(mergedFilePath, FileMode.Create, FileAccess.Write))
                {
                    foreach (var chunkPath in chunkFiles)
                    {
                        using (var chunkStream = new FileStream(chunkPath, FileMode.Open, FileAccess.Read))
                        {
                            await chunkStream.CopyToAsync(outputStream);
                        }
                    }
                }

                // Merged ফাইলের actual size যাচাই করুন
                var mergedFileInfo = new FileInfo(mergedFilePath);
                var actualSize = mergedFileInfo.Length;

                Console.WriteLine($"[ChunkedUpload] Merge complete | Size: {actualSize} bytes");

                // Temp session directory মুছে দিন
                Directory.Delete(sessionDir, recursive: true);

                // ডেটাবেজে record সেভ করুন
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
                    DownloadToken = string.Empty,
                    DownloadTokenExpiration = null
                };

                _context.ChunkedFileRecords.Add(record);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[ChunkedUpload] Saved to DB | Id: {record.Id}");

                return Ok(new
                {
                    id = record.Id,
                    fileName = record.FileName,
                    fileSize = record.FileSize,
                    totalChunks = record.TotalChunks,
                    message = "File uploaded and assembled successfully!"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChunkedUpload] Finalize error: {ex.Message}");
                return StatusCode(500, $"Finalize failed: {ex.Message}");
            }
        }


        //// ===========================================================
        //// GET All Files
        //// ===========================================================
        //[HttpGet]
        //public async Task<ActionResult<IEnumerable<ChunkedFileRecordDTO>>> GetAll()
        //{
        //    var records = await _context.ChunkedFileRecords
        //        .OrderByDescending(r => r.UploadedAt)
        //        .ToListAsync();

        //    var dtos = records.Select(r => new ChunkedFileRecordDTO
        //    {
        //        Id = r.Id,
        //        Filename = r.FileName,
        //        Filetype = r.FileType,
        //        Filesize = r.FileSize,
        //        RawFileSize = r.RawFileSize,
        //        TotalChunks = r.TotalChunks,
        //        UploadId = r.UploadId,
        //        UploadedAt = r.UploadedAt.ToString("yyyy-MM-dd HH:mm:ss"),
        //        Filestring = null // List তে base64 পাঠাই না (performance)
        //    }).ToList();

        //    return Ok(dtos);
        //}



        [HttpGet]
        public async Task<ActionResult<IEnumerable<ChunkedFileRecordDTO>>> GetAll()
        {
            var records = await _context.ChunkedFileRecords
                .OrderByDescending(r => r.UploadedAt)
                .ToListAsync();

            var dtos = new List<ChunkedFileRecordDTO>();

            foreach (var r in records)
            {
                // ✅ শুধু image type এর জন্য base64 পাঠাও
                string? base64 = null;
                if (r.FileType.StartsWith("image/"))
                {
                    base64 = await GetFileBase64Async(r.FileUrl);
                }

                dtos.Add(new ChunkedFileRecordDTO
                {
                    Id = r.Id,
                    Filename = r.FileName,
                    Filetype = r.FileType,
                    Filesize = r.FileSize,
                    RawFileSize = r.RawFileSize,
                    TotalChunks = r.TotalChunks,
                    UploadId = r.UploadId,
                    UploadedAt = r.UploadedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    Filestring = base64  // ✅ এখন image হলে base64 আসবে
                });
            }

            return Ok(dtos);
        }



        // ===========================================================
        // GET Single File (with base64 for preview)
        // ===========================================================
        [HttpGet("{id}")]
        public async Task<ActionResult<ChunkedFileRecordDTO>> GetById(int id)
        {
            var record = await _context.ChunkedFileRecords.FindAsync(id);
            if (record == null) return NotFound();

            string? base64 = null;
            if (record.FileType.StartsWith("image/"))
            {
                base64 = await GetFileBase64Async(record.FileUrl);
            }

            return Ok(new ChunkedFileRecordDTO
            {
                Id = record.Id,
                Filename = record.FileName,
                Filetype = record.FileType,
                Filesize = record.FileSize,
                RawFileSize = record.RawFileSize,
                TotalChunks = record.TotalChunks,
                UploadId = record.UploadId,
                UploadedAt = record.UploadedAt.ToString("yyyy-MM-dd HH:mm:ss"),
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
        // Download by Token (expiring link)
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

            // Disk থেকে ফাইল মুছুন
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

        /// <summary>এই upload session এর temporary directory path</summary>
        private string GetSessionDir(string uploadId)
        {
            // uploadId sanitize করুন (security)
            var safeId = string.Concat(uploadId.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));
            return Path.Combine(TempChunksFolder, safeId);
        }

        /// <summary>ফাইল নাম থেকে unsafe characters সরিয়ে দিন</summary>
        private static string SanitizeFileName(string fileName)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return string.Concat(fileName.Select(c => invalid.Contains(c) ? '_' : c));
        }

        /// <summary>File size কে human readable string এ রূপান্তর</summary>
        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{Math.Round(bytes / 1024.0, 2)} KBs";
            if (bytes < 1024L * 1024 * 1024) return $"{Math.Round(bytes / (1024.0 * 1024), 2)} MBs";
            return $"{Math.Round(bytes / (1024.0 * 1024 * 1024), 2)} GBs";
        }

        /// <summary>Server এর file path থেকে base64 string তৈরি করুন</summary>
        private async Task<string?> GetFileBase64Async(string fileUrl)
        {
            try
            {
                var relPath = fileUrl.TrimStart('/');
                var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, relPath);
                if (!System.IO.File.Exists(fullPath)) return null;
                var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
                return Convert.ToBase64String(bytes);
            }
            catch
            {
                return null;
            }
        }


        // ===========================================================
        // URL → Backend Direct Download
        // POST /api/chunkedupload/upload-from-url
        // ===========================================================
        /// <summary>
        /// Backend নিজেই URL থেকে file download করে disk এ save করে।
        /// Browser কে কিছু করতে হয় না।
        /// </summary>
        [HttpPost("upload-from-url")]
        public async Task<IActionResult> UploadFromUrl([FromBody] ChunkedUrlUploadRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Url))
                return BadRequest("URL is required.");

            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(30); // বড় ফাইলের জন্য

                // Response headers পড়তে শুরু করুন (streaming)
                using var response = await httpClient.GetAsync(request.Url, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                    return BadRequest($"Failed to reach URL. Status: {response.StatusCode}");

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var fileName = GetFileNameFromUrl(request.Url, response);
                var fileType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

                // Output directory তৈরি করুন
                if (!Directory.Exists(MergedFilesFolder))
                    Directory.CreateDirectory(MergedFilesFolder);

                var uniqueFileName = Guid.NewGuid().ToString("N") + "_" + SanitizeFileName(fileName);
                var outputPath = Path.Combine(MergedFilesFolder, uniqueFileName);
                var fileUrl = "/chunked_uploads/" + uniqueFileName;

                long totalRead = 0;
                var buffer = new byte[81920]; // 80KB chunks

                // Streaming download — memory এ পুরো ফাইল না রেখে সরাসরি disk এ লিখুন
                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);

                int bytesRead;
                int lastReportedProgress = -1;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalRead += bytesRead;

                    // Progress log (SignalR দিয়ে পাঠাতে চাইলে এখানে যোগ করুন)
                    if (totalBytes > 0)
                    {
                        int progress = (int)((totalRead * 100) / totalBytes);
                        if (progress != lastReportedProgress)
                        {
                            lastReportedProgress = progress;
                            Console.WriteLine($"[ChunkedUpload] URL Download: {progress}% ({totalRead}/{totalBytes} bytes)");
                        }
                    }
                }

                var actualSize = totalRead;
                Console.WriteLine($"[ChunkedUpload] URL Download complete: {actualSize} bytes → {outputPath}");

                // DB তে save করুন
                var record = new ChunkedFileRecord
                {
                    FileName = fileName,
                    FileType = fileType,
                    FileSize = FormatFileSize(actualSize),
                    RawFileSize = actualSize,
                    FileUrl = fileUrl,
                    TotalChunks = 1, // backend direct = 1 piece (no chunking)
                    UploadId = "url-direct-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                    UploadedAt = DateTime.UtcNow,
                    DownloadToken = string.Empty,
                    DownloadTokenExpiration = null
                };

                _context.ChunkedFileRecords.Add(record);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    id = record.Id,
                    fileName = record.FileName,
                    fileSize = record.FileSize,
                    message = "File downloaded and saved successfully from URL!"
                });
            }
            catch (TaskCanceledException)
            {
                return StatusCode(504, "Download timed out. The URL may be too slow or the file too large.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChunkedUpload] URL download error: {ex.Message}");
                return StatusCode(500, $"Server error: {ex.Message}");
            }
        }


        // ===========================================================
        // Helper: URL থেকে file name বের করুন
        // ===========================================================
        private static string GetFileNameFromUrl(string url, HttpResponseMessage response)
        {
            // Content-Disposition header থেকে নাম নেওয়ার চেষ্টা করুন
            var contentDisposition = response.Content.Headers.ContentDisposition;
            if (contentDisposition?.FileName != null)
            {
                return SanitizeFileName(contentDisposition.FileName.Trim('"'));
            }

            // URL path থেকে নাম বের করুন
            try
            {
                var uri = new Uri(url);
                var pathSegment = uri.Segments.LastOrDefault() ?? "file";
                var decoded = Uri.UnescapeDataString(pathSegment);
                if (!string.IsNullOrWhiteSpace(decoded) && decoded != "/")
                    return SanitizeFileName(decoded);
            }
            catch { }

            return "downloaded-file-" + DateTime.UtcNow.Ticks;
        }
    }
}