using Microsoft.AspNetCore.Mvc;
using OcrChatGptServer.Models;
using OcrChatGptServer.Services;

namespace OcrChatGptServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OcrChatController : ControllerBase
    {
        private readonly OcrChatGptQueue _queue;
        private readonly ILogger<OcrChatController> _logger;

        public OcrChatController(OcrChatGptQueue queue, ILogger<OcrChatController> logger)
        {
            _queue = queue;
            _logger = logger;
        }

        /// <summary>
        /// POST /OcrChat/Submit
        /// Submits prompt + image to be processed.
        /// Returns a unique requestId to track the status/result.
        /// </summary>
        [HttpPost("Submit")]
        public IActionResult Submit([FromForm] OcrChatRequest request)
        {
            if (request.File == null || request.File.Length == 0)
            {
                _logger.LogWarning("Submit request failed: No image file provided.");
                return BadRequest("No image file provided.");
            }

            // Generate a unique ID
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation("Received OCR request with ID: {RequestId}, Language: {Language}, Size: {Size}", requestId, request.Language, request.Size);

            try
            {
                // Save the file to a temp location
                var tempFilePath = Path.GetTempFileName();
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    request.File.CopyTo(stream);
                }
                _logger.LogInformation("Saved uploaded file for request {RequestId} at {FilePath}", requestId, tempFilePath);

                // Enqueue the request, but pass the file path, not the file itself
                _queue.EnqueueRequest(requestId, request.Language, tempFilePath, request.Size, false);
                _logger.LogInformation("Enqueued OCR processing request: {RequestId}", requestId);

                return Ok(new { requestId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing request {RequestId}", requestId);
                return StatusCode(500, "Internal server error.");
            }
        }

        /// <summary>
        /// GET /OcrChat/Result/{requestId}
        /// Retrieves the result of the OCR+ChatGPT processing.
        /// </summary>
        [HttpGet("Result/{requestId}")]
        public IActionResult GetResult(string requestId)
        {
            _logger.LogInformation("Fetching result for request {RequestId}", requestId);
            var result = _queue.GetResult(requestId);

            if (result == null)
            {
                _logger.LogWarning("Result not found or still processing for request {RequestId}", requestId);
                return NotFound("Result not found or still processing.");
            }

            _logger.LogInformation("Returning processed result for request {RequestId}", requestId);
            return Ok(result.ChatGptResponse);
        }
    }
}
