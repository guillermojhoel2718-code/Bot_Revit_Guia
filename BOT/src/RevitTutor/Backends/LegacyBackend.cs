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
                    fallback.Text = $"{fallback.Message}\n\nNota: Gemini offline: {ex.Message}";
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
            // Ampliamos para capturar más dudas
            return q.Contains("como") || q.Contains("cómo") || q.Contains("paso") || q.Contains("explica") || 
                   q.Contains("enseña") || q.Contains("ensena") || q.Contains("dibuja") || q.Contains("crea");
        }

        private TutorAnswer BuildLocalAnswer(string question, ModelContext context, bool hasKey)
        {
            var dest = new Destination();
            string msg = "Soy tu tutor de Revit. Puedo ayudarte a encontrar elementos o explicarte procesos.";
            string q = question.ToLower()
                .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u");

            // Categorías
            bool foundCat = false;
            if (q.Contains("muro") || q.Contains("wall")) { dest.CategoryName = "OST_Walls"; foundCat = true; }
            else if (q.Contains("suelo") || q.Contains("losa") || q.Contains("floor")) { dest.CategoryName = "OST_Floors"; foundCat = true; }
            else if (q.Contains("pilar") || q.Contains("columna") || q.Contains("column")) { dest.CategoryName = "OST_StructuralColumns"; foundCat = true; }
            else if (q.Contains("puerta") || q.Contains("door")) { dest.CategoryName = "OST_Doors"; foundCat = true; }
            else if (q.Contains("ventana") || q.Contains("window")) { dest.CategoryName = "OST_Windows"; foundCat = true; }

            // Acciones
            if (q.Contains("ocult") || q.Contains("apagar") || q.Contains("escond") || q.Contains("quita"))
            {
                dest.Action = "HIDE_CATEGORY";
                msg = "Ocultando la categoría solicitada.";
            }
            else if (q.Contains("mostr") || q.Contains("muestr") || q.Contains("ver") || q.Contains("prend") || q.Contains("selecc") || q.Contains("ensena") || q.Contains("enseña"))
            {
                if (q.Contains("todo")) { dest.Action = "SHOW_ALL"; msg = "Mostrando todas las categorías principales."; }
                else 
                {
                    dest.Action = "SHOW_CATEGORY";
                    dest.Highlight = true;
                    msg = foundCat ? $"Mostrando y seleccionando elementos de la categoría." : "Mostrando elementos solicitados.";
                }
            }
            else if (q.Contains("3d"))
            {
                dest.ViewId = "3D";
                msg = "Cambiando a la vista 3D.";
            }

            return new TutorAnswer 
            { 
                Message = msg, 
                Text = msg, 
                SuggestedDestination = (foundCat || !string.IsNullOrEmpty(dest.Action) || !string.IsNullOrEmpty(dest.ViewId)) ? dest : null 
            };
        }

        private async Task<IaSimpleAnswer?> CallGeminiAsync(string question, ModelContext context, string apiKey)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var prompt = BuildPromptForGemini(question, context);
            var payload = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(15);
            using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);

            if (!response.IsSuccessStatusCode) return null;

            string responseJson = await response.Content.ReadAsStringAsync();
            var gemini = JsonSerializer.Deserialize<GeminiResponse>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            string? text = gemini?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrWhiteSpace(text)) return null;

            try
            {
                var cleanJson = text.Replace("```json", "").Replace("```", "").Trim();
                return JsonSerializer.Deserialize<IaSimpleAnswer>(cleanJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { return new IaSimpleAnswer { Text = text.Trim(), Message = text.Trim() }; }
        }

        private string BuildPromptForGemini(string question, ModelContext context)
        {
            return $@"Eres un Tutor de Revit. Responde con PASOS PASO A PASO procedimentales.
Usa JSON: {{ ""message"": ""Pasos detallados"", ""suggestedDestination"": {{ ""categoryName"": ""OST_Walls"", ""highlight"": true, ""action"": ""SHOW_CATEGORY"" }} }}
Pregunta: {question}";
        }
    }

    public class GeminiResponse { public List<Candidate> Candidates { get; set; } }
    public class Candidate { public Content Content { get; set; } }
    public class Content { public List<Part> Parts { get; set; } }
    public class Part { public string Text { get; set; } }
}
