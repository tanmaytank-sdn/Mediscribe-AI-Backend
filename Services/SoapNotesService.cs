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
                You are a clinical assistant focused exclusively on creating SOAP notes.
                -- Only provide content related to healthcare SOAP note creation. Do not include any unrelated or non-medical information.
                -- If the user query is not related to healthcare SOAP notes (e.g., about GPT, finance, technology, or other topics), politely respond: 
                   'I’m sorry, but I can only provide answers related to healthcare. Please let me know if your query is healthcare-related, and I’ll be happy to assist you.'
                -- Always generate responses in the structure of SOAP notes: Subjective, Objective, Assessment, and Plan.
                -- Ensure responses are polite, patient-friendly, and written in a professional medical tone.
                -- Keep each section concise, clear, and easy to read so that it fits well in a chatbot UI.
                -- Limit sentences to short, simple statements suitable for patients and clinicians.
                -- In addition to JSON, also generate an HTML version of the same content under the key htmlFormat.
                -- The HTML must include proper headings (SOAP, Subjective, Objective, Assessment, Plan) and subheadings under Plan (Investigations, Medications, Lifestyle     Advice,             Referrals, Monitoring).
                -- Ensure the HTML is clean, minimal, and directly renderable in a chatbot UI.
                -- If the input is not about the helth care domain please bind the data into only **RawResponse** and **HtmlFormat fields** of Output JSON Structure.
                
                Always output valid JSON strictly following this schema:
                
                Patient Narrative:
                {patientNarrative}
                
                Output JSON structure:
                {{
                  ""RawResponse"": ""..."",
                  ""Subjective"": ""..."",
                  ""Objective"": ""..."",
                  ""Assessment"": ""..."",
                  ""Plan"": {{
                    ""Investigations"": [""string""],
                    ""Medications"": [""string""],
                    ""LifestyleAdvice"": [""string""],
                    ""Referrals"": [""string""],
                    ""Monitoring"": [""string""]
                  }},
                  ""htmlFormat"": ""<div>...</div>""
                }}
                ";


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

            if (!string.IsNullOrEmpty(text))
            {
                text = text.Replace("```json", "")
                           .Replace("```", "")
                           .Trim();
            }

            SoapNotesResponse soapNote = null;

            bool looksLikeJson = text.StartsWith("{") || text.StartsWith("[");

            if (looksLikeJson)
            {
                try
                {
                    soapNote = JsonSerializer.Deserialize<SoapNotesResponse>(text, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch
                {
                    // fallback if parsing fails
                    soapNote = BuildFallback(text);
                }
            }
            else
            {
                // Direct fallback if not JSON
                soapNote = BuildFallback(text);
            }

            var conciseResponse = new SoapNotesResponse
            {
                RawResponse = soapNote.RawResponse,
                Subjective = soapNote.Subjective,
                Objective = soapNote.Objective,
                Assessment = soapNote.Assessment,
                HtmlFormat = soapNote.HtmlFormat,
                Plan = new PlanDetail
                {
                    Investigations = soapNote.Plan.Investigations.Take(3).ToList(),
                    Medications = soapNote.Plan.Medications.Take(3).ToList(),
                    LifestyleAdvice = soapNote.Plan.LifestyleAdvice.Take(3).ToList(),
                    Referrals = soapNote.Plan.Referrals.Take(1).ToList(),
                    Monitoring = soapNote.Plan.Monitoring.Take(3).ToList()
                }
            };

            return conciseResponse ?? new SoapNotesResponse();
        }

        // Helper method for fallback
        private SoapNotesResponse BuildFallback(string text)
        {
            return new SoapNotesResponse
            {
                RawResponse = text,
                Subjective = string.Empty,
                Objective = string.Empty,
                Assessment = string.Empty,
                Plan = new PlanDetail
                {
                    Investigations = new List<string>(),
                    Medications = new List<string>(),
                    LifestyleAdvice = new List<string>(),
                    Referrals = new List<string>(),
                    Monitoring = new List<string>()
                },
                HtmlFormat = $"<div><p>{System.Net.WebUtility.HtmlEncode(text)}</p></div>"
            };
        }
    }
}
