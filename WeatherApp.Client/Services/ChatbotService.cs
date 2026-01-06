using System.Net.Http.Json;

namespace WeatherApp.Client.Services
{
    public class ChatbotService
    {
        private readonly HttpClient _httpClient;

        public ChatbotService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> SendMessageAsync(string message, string? userId = null)
        {
            try
            {
                var request = new
                {
                    Message = message,
                    UserId = userId
                };

                var response = await _httpClient.PostAsJsonAsync("api/chatbot/query", request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ChatbotResponse>();
                    return result?.Response ?? "Sorry, I couldn't process that request.";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Chatbot API error: {response.StatusCode} - {errorContent}");
                    return "Sorry, I'm having trouble connecting. Please try again.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message to chatbot: {ex.Message}");
                return "Sorry, an error occurred. Please try again.";
            }
        }
    }

    public class ChatbotResponse
    {
        public string Response { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}

