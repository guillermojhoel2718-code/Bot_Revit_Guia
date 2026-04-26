using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Autodesk.Revit.UI;

namespace RevitTutor
{
    public class IaSimpleAnswer
    {
        public string Text { get; set; } = string.Empty;
        public string? Message { get; set; }
        public Destination? SuggestedDestination { get; set; }
    }

    public class LegacyBackend : IRevitTutorBackend
    {
        private readonly UIApplication _uiApp;

        public LegacyBackend(UIApplication uiApp)
        {
            _uiApp = uiApp;
        }

        public async Task<TutorAnswer> AskQuestionAsync(string question, ModelContext context)
        {
            string apiKey = ConfigService.LoadApiKey();
            bool hasKey = !string.IsNullOrEmpty(apiKey);

            if (hasKey && DetectHowToQuestion(question))
            {
                try
                {
                    var geminiResponse = await CallGeminiAsync(question, context, apiKey);
                    if (geminiResponse != null) 
                    {
                        return new TutorAnswer 
                        { 
                            Message = geminiResponse.Message ?? geminiResponse.Text,
                            Text = geminiResponse.Text,
                            SuggestedDestination = geminiResponse.SuggestedDestination
                        };
                    }
                }
                catch (Exception ex)
                {
                    var fallback = BuildLocalAnswer(question, context, hasKey);
                    fallback.Text = $"{fallback.Message}\n\nNota: No pude contactar a la IA: {ex.Message}.";
                    return fallback;
                }
            }

            return BuildLocalAnswer(question, context, hasKey);
        }

        public Task<ModelContext> GetModelContextAsync()
        {
            return Task.FromResult(ModelContextService.BuildContext(_uiApp));
        }

        public Task NavigateToDestinationAsync(Destination destination)
        {
            return NavigationService.NavigateToAsync(_uiApp, destination);
        }

        private bool DetectHowToQuestion(string question)
        {
            if (string.IsNullOrWhiteSpace(question)) return false;
            var q = question.ToLowerInvariant();
            // Si es una pregunta de "Cómo", intentamos IA
            return q.Contains("como") || q.Contains("cómo") || q.Contains("paso") || q.Contains("explica");
        }

        private TutorAnswer BuildLocalAnswer(string question, ModelContext context, bool hasKey)
        {
            var suggestedDestination = new Destination();
            string message = "Entendido. Procesando tu solicitud...";
            string q = question.ToLower()
                .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u");

            // 1. Detección de Categoría
            if (q.Contains("muro")) suggestedDestination.CategoryName = "OST_Walls";
            else if (q.Contains("suelo") || q.Contains("losa")) suggestedDestination.CategoryName = "OST_Floors";
            else if (q.Contains("pilar") || q.Contains("columna")) suggestedDestination.CategoryName = "OST_StructuralColumns";
            else if (q.Contains("puerta")) suggestedDestination.CategoryName = "OST_Doors";

            // 2. Detección de Acción
            if (q.Contains("ocult") || q.Contains("apagar") || q.Contains("escond"))
            {
                suggestedDestination.Action = "HIDE_CATEGORY";
                message = "Ocultando la categoría solicitada.";
            }
            else if (q.Contains("mostr") || q.Contains("prend") || q.Contains("ver") || q.Contains("selecc"))
            {
                if (q.Contains("todo")) suggestedDestination.Action = "SHOW_ALL";
                else suggestedDestination.Action = "SHOW_CATEGORY";
                suggestedDestination.Highlight = true;
                message = "Mostrando y resaltando elementos.";
            }
            else if (q.Contains("3d"))
            {
                suggestedDestination.ViewId = "3D";
                message = "Cambiando a vista 3D.";
            }

            return new TutorAnswer 
            { 
                Message = message, 
                Text = message, 
                SuggestedDestination = string.IsNullOrEmpty(suggestedDestination.CategoryName) && 
                                        string.IsNullOrEmpty(suggestedDestination.ViewId) && 
                                        string.IsNullOrEmpty(suggestedDestination.Action) ? null : suggestedDestination 
            };
        }

        private async Task<IaSimpleAnswer?> CallGeminiAsync(string question, ModelContext context, string apiKey)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var prompt = BuildPromptForGemini(question, context);
            
            var payload = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } }
            };

            string json = JsonSerializer.Serialize(payload);
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(20);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Error Gemini: {response.StatusCode}");
            }

            string responseJson = await response.Content.ReadAsStringAsync();
            var gemini = JsonSerializer.Deserialize<GeminiResponse>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            string? text = gemini?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrWhiteSpace(text)) return null;

            try
            {
                var cleanJson = text.Replace("```json", "").Replace("```", "").Trim();
                return JsonSerializer.Deserialize<IaSimpleAnswer>(cleanJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return new IaSimpleAnswer { Text = text.Trim(), Message = text.Trim() };
            }
        }

        private string BuildPromptForGemini(string question, ModelContext context)
        {
            return $@"Eres un Tutor de Revit. Responde con PASOS PASO A PASO.
Usa este formato JSON:
{{
  ""message"": ""Explicación detallada de pasos (Menú > Herramienta)"",
  ""suggestedDestination"": {{
    ""categoryName"": ""OST_Walls (si aplica)"",
    ""highlight"": true,
    ""action"": ""SHOW_CATEGORY""
  }}
}}

Pregunta: {question}
Contexto: {context.VistaActual}";
        }
    }

    public class GeminiResponse { public List<Candidate> Candidates { get; set; } }
    public class Candidate { public Content Content { get; set; } }
    public class Content { public List<Part> Parts { get; set; } }
    public class Part { public string Text { get; set; } }
}
