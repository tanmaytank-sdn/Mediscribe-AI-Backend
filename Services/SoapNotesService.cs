using Mediscribe_AI.Models.ResponseVM;
using Mediscribe_AI.Serives.Interfaces;
using System.Text;
using System.Text.Json;

namespace Mediscribe_AI.Serives
{
    public class SoapNotesService : ISoapNotesService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public SoapNotesService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Gemini:ApiKey"];
        }

        public async Task<SoapNotesResponse> GenerateSoapNotesAsync(string patientNarrative)
        {
            var prompt = $@"
                You are a clinical assistant helping to create SOAP notes.
                --Please generate polite and patient-friendly responses while keeping a professional medical tone. 
                --The goal is to make the output easy for patients and clinicians to read, not blunt or overly technical. 
                --Always output valid JSON strictly following this schema:
                
                Patient Narrative:
                {patientNarrative}
                
                Output JSON structure:
                {{
                  ""Subjective"": ""..."",
                  ""Objective"": ""..."",
                  ""Assessment"": ""..."",
                  ""Plan"": {{
                    ""Investigations"": [""string""],
                    ""Medications"": [""string""],
                    ""LifestyleAdvice"": [""string""],
                    ""Referrals"": [""string""],
                    ""Monitoring"": [""string""]
                  }}
                }}"
            ;

            var requestBody = new
            {
                contents = new[]
                {
                   new
                   {
                       parts = new[]
                       {
                           new { text = prompt }
                       }
                   }
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post,
                "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent");

            request.Headers.Add("X-Goog-Api-Key", _apiKey);

            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();

            var aiResponse = JsonDocument.Parse(jsonResponse);

            var text = aiResponse.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text").GetString();

            // Remove markdown code fences (```json ... ```)
            if (!string.IsNullOrEmpty(text))
            {
                text = text.Replace("```json", "")
                           .Replace("```", "")
                           .Trim();
            }

            // Deserialize into strongly-typed class
            var soapNote = JsonSerializer.Deserialize<SoapNotesResponse>(text, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return soapNote ?? new SoapNotesResponse();
        }
    }
}
