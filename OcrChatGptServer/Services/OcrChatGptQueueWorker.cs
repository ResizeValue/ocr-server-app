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

        public OcrChatGptQueueWorker(OcrChatGptQueue queue, GptService gptService, IOcrService ocrService, ImgurService imgurService)
        {
            _queue = queue;
            _gptService = gptService;
            _ocrService = ocrService;
            _imgurService = imgurService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_queue.TryDequeueRequest(out var item))
                {
                    var url = await _imgurService.UploadImageAsync(item.FilePath);

                    if (item.TextOnly)
                    {
                        var ocrText = await _ocrService.PerformOcr(url);
                        var chatGptResponse = await _gptService.ProcessTextTasksAsync(ocrText, item.Size);
                        var result = new OcrChatResult(item.RequestId, ocrText, chatGptResponse);
                        _queue.StoreResult(result);
                    }
                    else
                    {
                        var chatGptResponse = await _gptService.ProcessImageTasksAsync(url, item.Size, item.Language);
                        var result = new OcrChatResult(item.RequestId, url, chatGptResponse);
                        _queue.StoreResult(result);
                    }
                }
                else
                {
                    await Task.Delay(500, stoppingToken);
                }
            }
        }

    }
}
