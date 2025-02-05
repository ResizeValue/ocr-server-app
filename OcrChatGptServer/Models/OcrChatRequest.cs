namespace OcrChatGptServer.Models
{
    public class OcrChatRequest
    {
        public string Language { get; set; }

        public IFormFile File { get; set; }

        public string Size { get; set; }
    }
}
