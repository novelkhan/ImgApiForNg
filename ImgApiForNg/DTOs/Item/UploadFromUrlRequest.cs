namespace ImgApiForNg.DTOs.Item
{
    public class UploadFromUrlRequest
    {
        public string Url { get; set; }
        public string? ConnectionId { get; set; }  // Add connectionId to the request
    }
}