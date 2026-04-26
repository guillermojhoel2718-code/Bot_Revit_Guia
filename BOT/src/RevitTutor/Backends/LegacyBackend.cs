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
            // Corrección automática de Alza -> AIza
            if (!string.IsNullOrEmpty(apiKey) && apiKey.StartsWith("Alza")) apiKey = "AI" + apiKey.Substring(2);
            
            bool hasKey = !string.IsNullOrEmpty(apiKey);

            if (hasKey && DetectHowToQuestion(question))
            {
                try
                {
                    var geminiResponse = await CallGeminiAsync(question, context, apiKey);
                    if (geminiResponse != null) return new TutorAnswer { Message = geminiResponse.Message ?? geminiResponse.Text, Text = geminiResponse.Text, SuggestedDestination = geminiResponse.SuggestedDestination };
                }
                catch (Exception ex)
                {
                    var fallback = BuildLocalAnswer(question, context, hasKey);
                    fallback.Text = $"{fallback.Message}\n\n[IA Error]: {ex.Message}";
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
            // Muy permisivo para que cualquier duda active la IA
            return q.Contains("como") || q.Contains("cómo") || q.Contains("dibuja") || q.Contains("crea") || q.Contains("hace") || q.Contains("explica") || q.Contains("enseña") || q.Contains("paso");
        }

        private TutorAnswer BuildLocalAnswer(string question, ModelContext context, bool hasKey)
        {
            var dest = new Destination();
            string q = question.ToLower().Replace("á","a").Replace("é","e").Replace("í","i").Replace("ó","o").Replace("ú","u");

            // FUZZY MATCHING para faltas de ortografía
            bool isWall = q.Contains("muro") || q.Contains("wall") || q.Contains("mro");
            bool isFloor = q.Contains("suelo") || q.Contains("losa") || q.Contains("floor") || q.Contains("selo") || q.Contains("lsa");
            bool isColumn = q.Contains("pilar") || q.Contains("columna") || q.Contains("column") || q.Contains("plar");
            bool isDoor = q.Contains("puerta") || q.Contains("door") || q.Contains("puer");
            
            bool isShow = q.Contains("mostr") || q.Contains("muestr") || q.Contains("ver") || q.Contains("selec") || q.Contains("ensen") || q.Contains("prend");
            bool isHide = q.Contains("ocult") || q.Contains("apagar") || q.Contains("escond") || q.Contains("quita");

            if (isWall) dest.CategoryName = "OST_Walls";
            else if (isFloor) dest.CategoryName = "OST_Floors";
            else if (isColumn) dest.CategoryName = "OST_StructuralColumns";
            else if (isDoor) dest.CategoryName = "OST_Doors";

            string msg = "Soy tu tutor de Revit. Prueba preguntando '¿cómo dibujo un muro?' o 'selecciona los suelos'.";

            if (isHide)
            {
                dest.Action = "HIDE_CATEGORY";
                msg = "Ocultando elementos...";
            }
            else if (isShow)
            {
                if (q.Contains("todo")) dest.Action = "SHOW_ALL";
                else dest.Action = "SHOW_CATEGORY";
                dest.Highlight = true;
                msg = "Buscando y seleccionando elementos...";
            }
            else if (q.Contains("3d"))
            {
                dest.ViewId = "3D";
                msg = "Cambiando a vista 3D.";
            }

            return new TutorAnswer { Message = msg, Text = msg, SuggestedDestination = (dest.CategoryName != null || dest.Action != null || dest.ViewId != null) ? dest : null };
        }

        private async Task<IaSimpleAnswer?> CallGeminiAsync(string question, ModelContext context, string apiKey)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var prompt = $@"Eres un Tutor de Revit. Responde PASO A PASO. 
JSON: {{ ""message"": ""Explicación"", ""suggestedDestination"": {{ ""categoryName"": ""OST_Walls"", ""highlight"": true, ""action"": ""SHOW_CATEGORY"" }} }}
Pregunta: {question}";
            var payload = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(15);
            using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);
            if (!response.IsSuccessStatusCode) throw new Exception($"Gemini {response.StatusCode}");

            var responseJson = await response.Content.ReadAsStringAsync();
            var gemini = JsonSerializer.Deserialize<GeminiResponse>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var text = gemini?.Candidates?[0].Content?.Parts?[0].Text;
            if (string.IsNullOrEmpty(text)) return null;

            try {
                var clean = text.Replace("```json", "").Replace("```", "").Trim();
                return JsonSerializer.Deserialize<IaSimpleAnswer>(clean, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            } catch { return new IaSimpleAnswer { Text = text.Trim(), Message = text.Trim() }; }
        }
    }

    public class GeminiResponse { public List<Candidate> Candidates { get; set; } }
    public class Candidate { public Content Content { get; set; } }
    public class Content { public List<Part> Parts { get; set; } }
    public class Part { public string Text { get; set; } }
}
