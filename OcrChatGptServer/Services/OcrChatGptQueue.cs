using System.Collections.Concurrent;

namespace OcrChatGptServer.Services
{
    public record OcrChatQueueItem(
        string RequestId,
        string Size,
        string FilePath,
        string Language,
        bool TextOnly
    );

    public record OcrChatResult(
        string RequestId,
        string OcrText,
        string ChatGptResponse
    );

    /// <summary>
    /// A thread-safe in-memory queue and storage.
    /// </summary>
    public class OcrChatGptQueue
    {
        private readonly ConcurrentQueue<OcrChatQueueItem> _requestQueue = new();
        private readonly ConcurrentDictionary<string, OcrChatResult> _results = new();
        private readonly ILogger<OcrChatGptQueue> _logger;

        public OcrChatGptQueue(ILogger<OcrChatGptQueue> logger)
        {
            _logger = logger;
        }

        public void EnqueueRequest(string requestId, string language, string filePath, string size, bool textOnly)
        {
            var item = new OcrChatQueueItem(requestId, language, filePath, size, textOnly);
            _requestQueue.Enqueue(item);
            _logger.LogInformation("Enqueued request {RequestId}: Language={Language}, Size={Size}, TextOnly={TextOnly}", requestId, language, size, textOnly);
        }

        public bool TryDequeueRequest(out OcrChatQueueItem item)
        {
            var result = _requestQueue.TryDequeue(out item);
            if (result)
            {
                _logger.LogInformation("Dequeued request {RequestId}: Language={Language}, Size={Size}, TextOnly={TextOnly}", item.RequestId, item.Language, item.Size, item.TextOnly);
            }
            else
            {
                _logger.LogWarning("Attempted to dequeue from an empty queue.");
            }
            return result;
        }

        public void StoreResult(OcrChatResult result)
        {
            _results[result.RequestId] = result;
            _logger.LogInformation("Stored OCR result for request {RequestId}", result.RequestId);
        }

        public OcrChatResult? GetResult(string requestId)
        {
            if (_results.TryGetValue(requestId, out var result))
            {
                _logger.LogInformation("Retrieved OCR result for request {RequestId}", requestId);
                return result;
            }
            else
            {
                _logger.LogWarning("Requested OCR result for {RequestId} not found", requestId);
                return null;
            }
        }
    }
}
