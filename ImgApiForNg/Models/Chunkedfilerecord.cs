using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ImgApiForNg.Models
{
    /// <summary>
    /// Chunked upload এ সফলভাবে merge হওয়া ফাইলের ডেটাবেজ রেকর্ড
    /// </summary>
    public class ChunkedFileRecord
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public string FileSize { get; set; } = string.Empty;
        public long RawFileSize { get; set; }

        // ✅ Storage Type: "Folder" অথবা "Database"
        public string StorageType { get; set; } = "Folder";

        // Folder storage — file path (StorageType="Folder" হলে populated)
        public string? FileUrl { get; set; } = null;

        // Database storage — binary data (StorageType="Database" হলে populated)
        public byte[]? FileData { get; set; } = null;

        // ✅ কখন Storage switch হয়েছে (null = কখনো switch হয়নি)
        public DateTime? StorageSwitchedAt { get; set; } = null;

        // ✅ Switch এর আগে কোন storage এ ছিল
        public string? PreviousStorageType { get; set; } = null;

        public int TotalChunks { get; set; }
        public string UploadId { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public string UploadMethod { get; set; } = string.Empty;
        public long UploadDurationMs { get; set; } = 0;

        public string DownloadToken { get; set; } = string.Empty;
        public DateTime? DownloadTokenExpiration { get; set; } = null;
    }
}