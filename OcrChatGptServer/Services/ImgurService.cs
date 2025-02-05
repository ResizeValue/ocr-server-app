using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

namespace OcrChatGptServer.Services
{
    public class ImgurService
    {
        private readonly string _key = "";
        private const string _imgurUploadUrl = "https://api.imgur.com/3/upload";
        private readonly ILogger<ImgurService> _logger;

        public ImgurService(IConfiguration configuration, ILogger<ImgurService> logger)
        {
            _key = configuration.GetSection("IMGUR_API_KEY").Value;
            _logger = logger;
        }

        public async Task<string> UploadImageAsync(string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                _logger.LogError("Image file not found: {ImagePath}", imagePath);
                throw new FileNotFoundException("Image file not found", imagePath);
            }

            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _key);
                    _logger.LogInformation("Uploading image: {ImagePath}", imagePath);

                    using (var form = new MultipartFormDataContent())
                    {
                        byte[] imageBytes = File.ReadAllBytes(imagePath);
                        var imageContent = new ByteArrayContent(imageBytes);
                        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png"); // Change type if needed

                        form.Add(imageContent, "image", Path.GetFileName(imagePath));

                        HttpResponseMessage response = await httpClient.PostAsync(_imgurUploadUrl, form);
                        string jsonResponse = await response.Content.ReadAsStringAsync();

                        if (!response.IsSuccessStatusCode)
                        {
                            _logger.LogError("Error uploading to Imgur: {StatusCode} - {Response}", response.StatusCode, jsonResponse);
                            throw new Exception($"Error uploading to Imgur: {response.StatusCode} - {jsonResponse}");
                        }

                        JObject json = JObject.Parse(jsonResponse);
                        string imageUrl = json["data"]["link"].ToString();

                        _logger.LogInformation("Image uploaded successfully: {ImageUrl}", imageUrl);
                        return imageUrl;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while uploading image: {ImagePath}", imagePath);
                throw;
            }
        }
    }
}
