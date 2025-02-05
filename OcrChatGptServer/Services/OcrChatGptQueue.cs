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
        // Concurrent queue for incoming requests
        private readonly ConcurrentQueue<OcrChatQueueItem> _requestQueue = new();

        // In-memory dictionary for results
        private readonly ConcurrentDictionary<string, OcrChatResult> _results = new();

        public void EnqueueRequest(string requestId, string language, string filePath, string size, bool textOnly)
        {
            _requestQueue.Enqueue(new OcrChatQueueItem(requestId, language, filePath, size, textOnly));
        }

        public bool TryDequeueRequest(out OcrChatQueueItem item)
        {
            return _requestQueue.TryDequeue(out item);
        }

        public void StoreResult(OcrChatResult result)
        {
            _results[result.RequestId] = result;
        }

        public OcrChatResult? GetResult(string requestId)
        {
            _results.TryGetValue(requestId, out var result);
            return result;
        }
    }
}
