using System.Collections.Generic;

namespace ImgApiForNg.DTOs.ChunkedUpload
{
    // ============================================================
    // InitializeUploadRequest.cs — POST /chunkedupload/initialize
    // ============================================================
    /// <summary>
    /// Client প্রথমে এই request পাঠায় upload শুরু করতে।
    /// Server জানে: কতটি chunk আসবে, file এর নাম/type/size কী।
    /// </summary>
    public class InitializeUploadRequest
    {
        /// <summary>Client generated unique ID for this upload session</summary>
        public string UploadId { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;

        /// <summary>Total file size in bytes</summary>
        public long FileSize { get; set; }

        /// <summary>How many chunks the file will be split into</summary>
        public int TotalChunks { get; set; }

        /// <summary>Size of each chunk in bytes</summary>
        public long ChunkSize { get; set; }
    }


    // ============================================================
    // FinalizeUploadRequest.cs — POST /chunkedupload/finalize
    // ============================================================
    /// <summary>
    /// সব chunk আসার পরে এই request পাঠালে server chunks merge করে।
    /// </summary>
    public class FinalizeUploadRequest
    {
        public string UploadId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public int TotalChunks { get; set; }
    }


    // ============================================================
    // ChunkedFileRecordDTO.cs — Response DTO
    // ============================================================
    /// <summary>
    /// File list এবং single file get করার সময় এই DTO return করা হয়।
    /// </summary>
    public class ChunkedFileRecordDTO
    {
        public int Id { get; set; }
        public string Filename { get; set; } = string.Empty;
        public string Filetype { get; set; } = string.Empty;
        public string Filesize { get; set; } = string.Empty;
        public long RawFileSize { get; set; }
        public int TotalChunks { get; set; }
        public string UploadId { get; set; } = string.Empty;
        public string UploadedAt { get; set; } = string.Empty;

        /// <summary>Base64 string — শুধু single file get এ পাঠানো হয় (list এ নয়)</summary>
        public string? Filestring { get; set; } = null;
    }


    // ===========================================================
    // DTO: ChunkedUrlUploadRequest
    // DTOs/ChunkedUpload/ChunkedUploadDTOs.cs এ যোগ করুন
    // ===========================================================
    public class ChunkedUrlUploadRequest
    {
        public string Url { get; set; } = string.Empty;
    }
    
}