using Mediscribe_AI.Models.ResponseVM;
using Mediscribe_AI.Serives.Interfaces;
using System.Numerics;
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
                --Keep the response polite, easy to read, and concise so that it fits in a chatbot UI.
                --Limit each section to short, clear sentences.
                --In addition to JSON, also generate an HTML version of the same content under the key 'htmlFormat', formatted with proper headings (SOAP, Subjective, Objective, Assessment, Plan) and subheadings for the plan (Investigations, Medications, Lifestyle Advice, Referrals, Monitoring). 
                --Ensure the HTML is clean and suitable for direct rendering in a chatbot UI.

                Always output valid JSON strictly following this schema:
                
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
                ""htmlFormat"": ""<div>...</div>""
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

            var conciseResponse = new SoapNotesResponse
            {
                Subjective = soapNote.Subjective,
                Objective = soapNote.Objective,
                Assessment = soapNote.Assessment,
                HtmlFormat = soapNote.HtmlFormat,
                Plan = new PlanDetail
                {
                    Investigations = soapNote.Plan.Investigations.Take(2).ToList(),
                    Medications = soapNote.Plan.Medications.Take(2).ToList(),
                    LifestyleAdvice = soapNote.Plan.LifestyleAdvice.Take(2).ToList(),
                    Referrals = soapNote.Plan.Referrals.Take(1).ToList(),
                    Monitoring = soapNote.Plan.Monitoring.Take(2).ToList()
                }
            };

            return conciseResponse ?? new SoapNotesResponse();
        }
    }
}
