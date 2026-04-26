namespace ImgApiForNg.DTOs.ChunkedUpload
{
    // ============================================================
    // InitializeUploadRequest
    // ============================================================
    public class InitializeUploadRequest
    {
        public string UploadId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public int TotalChunks { get; set; }
        public long ChunkSize { get; set; }
        public string UploadMethod { get; set; } = "LocalFile";
        public long ClientStartTimeMs { get; set; } = 0;

        // ✅ User কোথায় store করতে চায়: "Folder" অথবা "Database"
        public string StorageType { get; set; } = "Folder";
    }

    // ============================================================
    // FinalizeUploadRequest
    // ============================================================
    public class FinalizeUploadRequest
    {
        public string UploadId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public int TotalChunks { get; set; }
        public string UploadMethod { get; set; } = "LocalFile";
        public long ClientStartTimeMs { get; set; } = 0;

        // ✅ কোথায় store করবে
        public string StorageType { get; set; } = "Folder";
    }

    // ============================================================
    // ChunkedUrlUploadRequest
    // ============================================================
    public class ChunkedUrlUploadRequest
    {
        public string Url { get; set; } = string.Empty;
        public string UploadMethod { get; set; } = "UrlBackend";
        public long ClientStartTimeMs { get; set; } = 0;

        // ✅ কোথায় store করবে
        public string StorageType { get; set; } = "Folder";
    }

    // ============================================================
    // SwitchStorageRequest — Storage switch করার জন্য
    // ============================================================
    public class SwitchStorageRequest
    {
        // "Folder" অথবা "Database"
        public string TargetStorage { get; set; } = string.Empty;
    }

    // ============================================================
    // ChunkedFileRecordDTO — Response
    // ============================================================
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
        public string UploadMethod { get; set; } = string.Empty;
        public long UploadDurationMs { get; set; }
        public string UploadDuration { get; set; } = string.Empty;

        // ✅ Storage info
        public string StorageType { get; set; } = string.Empty;
        public string? PreviousStorageType { get; set; } = null;
        public string? StorageSwitchedAt { get; set; } = null;

        // Image preview এর জন্য base64 (image type হলে)
        public string? Filestring { get; set; } = null;
    }
}