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
        //[Key]
        //[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        //public int Id { get; set; }

        ///// <summary>Original file name</summary>
        //public string FileName { get; set; } = string.Empty;

        ///// <summary>MIME type e.g. image/jpeg</summary>
        //public string FileType { get; set; } = string.Empty;

        ///// <summary>Human readable size e.g. "2.5 MBs"</summary>
        //public string FileSize { get; set; } = string.Empty;

        ///// <summary>Raw file size in bytes</summary>
        //public long RawFileSize { get; set; }

        ///// <summary>Path to the merged file in wwwroot</summary>
        //public string FileUrl { get; set; } = string.Empty;

        ///// <summary>Total number of chunks this file was split into</summary>
        //public int TotalChunks { get; set; }

        ///// <summary>Unique upload session ID from client</summary>
        //public string UploadId { get; set; } = string.Empty;

        ///// <summary>When the upload was completed</summary>
        //public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        ///// <summary>Token for expiring download link generation</summary>
        //public string DownloadToken { get; set; } = string.Empty;

        ///// <summary>Expiration for the download token</summary>
        //public DateTime? DownloadTokenExpiration { get; set; } = null;


        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public string FileSize { get; set; } = string.Empty;
        public long RawFileSize { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public int TotalChunks { get; set; }
        public string UploadId { get; set; } = string.Empty;

        // ✅ কখন আপলোড হয়েছে
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // ✅ কোন method এ আপলোড হয়েছে
        // "LocalFile" | "UrlFrontend" | "UrlBackend"
        public string UploadMethod { get; set; } = string.Empty;

        // ✅ আপলোড হতে কতো সময় লেগেছে (milliseconds)
        public long UploadDurationMs { get; set; } = 0;

        public string DownloadToken { get; set; } = string.Empty;
        public DateTime? DownloadTokenExpiration { get; set; } = null;
    }
}