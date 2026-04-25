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
        public string UploadId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public int TotalChunks { get; set; }
        public long ChunkSize { get; set; }

        // ✅ কোন method এ upload শুরু হয়েছে
        public string UploadMethod { get; set; } = "LocalFile";

        // ✅ Client side এ upload শুরুর সময় (Unix ms)
        public long ClientStartTimeMs { get; set; } = 0;
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

        // ✅ কোন method এ upload হয়েছে
        public string UploadMethod { get; set; } = "LocalFile";

        // ✅ Client side এ upload শুরুর সময় (Unix ms) — duration হিসাব করতে
        public long ClientStartTimeMs { get; set; } = 0;
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

        // ✅ কখন আপলোড হয়েছে (formatted string)
        public string UploadedAt { get; set; } = string.Empty;

        // ✅ কোন method এ আপলোড হয়েছে
        public string UploadMethod { get; set; } = string.Empty;

        // ✅ Duration — milliseconds
        public long UploadDurationMs { get; set; }

        // ✅ Duration — human readable (e.g. "3.2s", "1m 12s")
        public string UploadDuration { get; set; } = string.Empty;

        public string? Filestring { get; set; } = null;
    }


    // ===========================================================
    // DTO: ChunkedUrlUploadRequest
    // DTOs/ChunkedUpload/ChunkedUploadDTOs.cs এ যোগ করুন
    // ===========================================================
    public class ChunkedUrlUploadRequest
    {
        public string Url { get; set; } = string.Empty;

        // ✅ "UrlFrontend" অথবা "UrlBackend"
        public string UploadMethod { get; set; } = "UrlBackend";

        // ✅ Client side এ শুরুর সময় (Unix ms)
        public long ClientStartTimeMs { get; set; } = 0;
    }
    
}