namespace OcrChatGptServer.Services
{
    public interface IOcrService
    {
        public Task<string> PerformOcr(string filePath);
    }
}
