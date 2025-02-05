using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace OcrChatGptServer.Services
{
    public class AzureOcrService : IOcrService
    {
        private readonly string _subscriptionKey = "";
        private readonly string _endpoint = "";


        public async Task<string> PerformOcr(string url)
        {
            var client = Authenticate(_endpoint, _subscriptionKey);

            return await ReadFileUrl(client, url);
        }

        public static ComputerVisionClient Authenticate(string endpoint, string key)
        {
            ComputerVisionClient client =
              new ComputerVisionClient(new ApiKeyServiceClientCredentials(key))
              { Endpoint = endpoint };
            return client;
        }

        public static async Task<string> ReadFileUrl(ComputerVisionClient client, string urlFile)
        {
            Console.WriteLine("----------------------------------------------------------");
            Console.WriteLine("READ FILE FROM URL");
            Console.WriteLine();

            // Read text from URL
            var textHeaders = await client.RecognizePrintedTextAsync(true, urlFile, OcrLanguages.Ru);
            // After the request, get the operation location (operation ID)
            Thread.Sleep(2000);


            // Display the found text.
            Console.WriteLine();
            var textUrlFileResults = textHeaders.Regions;
            var lines = new List<string>();
            foreach (OcrRegion page in textUrlFileResults)
            {
                lines = page.Lines.SelectMany(x => x.Words.Select(x => x.Text)).ToList();
            }
            Console.WriteLine();

            return lines.Aggregate((x, y) => x + " " + y);
        }
    }
}
