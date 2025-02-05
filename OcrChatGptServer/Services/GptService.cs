

using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.VisualBasic;
using OpenAI.Chat;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Buffers.Text;
using System.ComponentModel;
using System.Security.Principal;

namespace OcrChatGptServer.Services
{
    public class GptService
    {
        private readonly ChatClient _openAIApi;
        private readonly string _promptTextTask = "Look at the text, find the questions and answer to them in the language the text on the image is written.\n\n";

        private readonly string _promptImageTask = @"
                    You are an advanced AI capable of processing images and extracting meaningful information. Your task is as follows:

                    1. **Image Analysis:** Extract all text from the image, including questions, instructions, and multiple-choice options.

                    2. **Question Solving:** Answer **all** identified questions clearly and concisely.
                       - Instead of rewriting the full question, provide a **short summary** of its meaning before answering.
                       - If a question has multiple-choice options, select the correct answer(s) and explain the reasoning.
                       - If the question requires eliminating incorrect answers, provide the **remaining valid options**.

                    3. **Language Consistency:** Write responses in the same language as the text found in the image. Write a summary in the same language as the text found in the image.

                    4. **Structured Responses:** Present answers in a numbered list or bullet points for clarity.

                    5. **Formatting:**
                       - Use a **short summary of the question** instead of repeating it in full.
                       - For open-ended questions, provide a brief but well-structured explanation.
                       - For multiple-choice questions, list the correct choice(s) with reasoning.
                       - For elimination-type questions, list only the valid options.

                    6. **Answer Quality:** Ensure that your answers are accurate, relevant, and complete. If a question is ambiguous, provide a clear explanation of the assumptions made.

                    7. **IMPORTANT:** Do not provide answers that are factually incorrect, misleading, or inappropriate.

                    8. **IMPORTANT:** Write in a language that on the image is written.

                    9. **IMPORTANT:** If you write math formulas, please use LaTeX formatting.

                    **Note:** If the image contains multiple questions, answer each question separately. If the image contains a passage with questions, answer each question based on the passage content. If the image contains a single question, provide a detailed and accurate response.
                    ";



        public GptService(IConfiguration configuration)
        {
            _openAIApi = new(model: "gpt-4o", apiKey: configuration.GetSection("OPENAI_API_KEY").Value);
        }

        public async Task<string> ProcessTextTasksAsync(string text, string additionalPrompt = "")
        {
            var message = _promptTextTask + text + "\n\n" + additionalPrompt;

            var completion = await _openAIApi.CompleteChatAsync(message);

            return completion.Value.Content[0].Text;
        }

        public async Task<string> ProcessImageTasksAsync(string imageUrl, string language, string size)
        {
            var sizePrompt = size switch
            {
                "small" => "The responses should be brief and to the point. (small)",
                "medium" => "The responses should be detailed and comprehensive. (medium)",
                "large" => "The responses should be thorough and in-depth. (large)",
            };

            var languagePrompt = language switch
            {
                "en" => "Respond in english. This is important!",
                "ru" => "Отвечай на русском. Это важно!",
                "ua" => "Відповідай українською. Це важливо!",
            };

            var systemMessage = ChatMessage.CreateSystemMessage(_promptImageTask);
            var userMessage = ChatMessage.CreateUserMessage(_promptImageTask, ChatMessageContentPart.CreateImagePart(new Uri(imageUrl)), sizePrompt, languagePrompt);

            // Create a list of chat messages
            var messages = new List<ChatMessage>
            {
                systemMessage,
                userMessage
            };

            // Send the structured message list to your OpenAI API endpoint
            var response = await _openAIApi.CompleteChatAsync(messages);

            Console.WriteLine(response.Value.Content[0].Text);

            // Extract and return the assistant's reply
            return response.Value.Content[0].Text;
        }
    }
}
