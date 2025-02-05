using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

namespace OcrChatGptServer.Services
{
    public class ImgurService
    {
        private readonly string _key = "";
        private const string _imgurUploadUrl = "https://api.imgur.com/3/upload";

        public ImgurService(IConfiguration configuration)
        {
            _key = configuration.GetSection("IMGUR_API_KEY").Value;
        }

        public async Task<string> UploadImageAsync(string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException("Image file not found", imagePath);
            }

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _key);

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
                        throw new Exception($"Error uploading to Imgur: {response.StatusCode} - {jsonResponse}");
                    }

                    JObject json = JObject.Parse(jsonResponse);
                    string imageUrl = json["data"]["link"].ToString();
                    return imageUrl;
                }
            }
        }
    }
}
