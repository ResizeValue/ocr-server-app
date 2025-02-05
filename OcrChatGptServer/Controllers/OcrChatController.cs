using Microsoft.AspNetCore.Mvc;
using OcrChatGptServer.Models;
using OcrChatGptServer.Services;

namespace OcrChatGptServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OcrChatController : ControllerBase
    {
        private readonly OcrChatGptQueue _queue;

        public OcrChatController(OcrChatGptQueue queue)
        {
            _queue = queue;
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
                return BadRequest("No image file provided.");

            // Generate a unique ID
            var requestId = Guid.NewGuid().ToString();

            // 1. Save the file to a temp location
            var tempFilePath = Path.GetTempFileName();
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                request.File.CopyTo(stream);
            }

            // 2. Enqueue the request, but pass the file path, not the file itself
            _queue.EnqueueRequest(requestId, request.Language, tempFilePath, request.Size, false);

            return Ok(new { requestId });
        }


        /// <summary>
        /// GET /OcrChat/Result/{requestId}
        /// Retrieves the result of the OCR+ChatGPT processing.
        /// </summary>
        [HttpGet("Result/{requestId}")]
        public IActionResult GetResult(string requestId)
        {
            var result = _queue.GetResult(requestId);
            if (result == null)
            {
                return NotFound("Result not found or still processing.");
            }
            return Ok(result.ChatGptResponse);
        }
    }
}
