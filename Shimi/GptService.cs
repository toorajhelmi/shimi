using OpenAI.Chat;

namespace Shimi
{
    public class GptService
    {
        private ChatClient chatClient;

        public GptService(string apiKey)
        {
            chatClient = new(model: "gpt-4o", apiKey);
        }

        public async Task<string> SendMessage(string prompt, float tempreture = 0.7f)
        {
            var completion = await chatClient.CompleteChatAsync([prompt], new ChatCompletionOptions
            {
                Temperature = tempreture
            });
            return completion.Value.Content[0].Text;
        }
    }
}
