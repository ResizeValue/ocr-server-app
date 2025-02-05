namespace OcrChatGptServer.Services
{
    /// <summary>
    /// Background service that continuously processes items in the queue.
    /// </summary>
    public class OcrChatGptQueueWorker : BackgroundService
    {
        private readonly OcrChatGptQueue _queue;
        private readonly GptService _gptService;
        private readonly IOcrService _ocrService;
        private readonly ImgurService _imgurService;
        private readonly ILogger<OcrChatGptQueueWorker> _logger;

        public OcrChatGptQueueWorker(OcrChatGptQueue queue, GptService gptService, IOcrService ocrService, ImgurService imgurService, ILogger<OcrChatGptQueueWorker> logger)
        {
            _queue = queue;
            _gptService = gptService;
            _ocrService = ocrService;
            _imgurService = imgurService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OcrChatGptQueueWorker started.");
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_queue.TryDequeueRequest(out var item))
                {
                    try
                    {
                        _logger.LogInformation("Processing request {RequestId}", item.RequestId);
                        var url = await _imgurService.UploadImageAsync(item.FilePath);
                        _logger.LogInformation("Uploaded image for request {RequestId}: {Url}", item.RequestId, url);

                        if (item.TextOnly)
                        {
                            var ocrText = await _ocrService.PerformOcr(url);
                            _logger.LogInformation("OCR completed for request {RequestId}", item.RequestId);
                            var chatGptResponse = await _gptService.ProcessTextTasksAsync(ocrText, item.Size);
                            _logger.LogInformation("ChatGPT processed text for request {RequestId}", item.RequestId);
                            var result = new OcrChatResult(item.RequestId, ocrText, chatGptResponse);
                            _queue.StoreResult(result);
                        }
                        else
                        {
                            var chatGptResponse = await _gptService.ProcessImageTasksAsync(url, item.Size, item.Language);
                            _logger.LogInformation("ChatGPT processed image for request {RequestId}", item.RequestId);
                            var result = new OcrChatResult(item.RequestId, url, chatGptResponse);
                            _queue.StoreResult(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing request {RequestId}", item.RequestId);
                    }
                }
                else
                {
                    await Task.Delay(500, stoppingToken);
                }
            }
            _logger.LogInformation("OcrChatGptQueueWorker stopping.");
        }
    }
}