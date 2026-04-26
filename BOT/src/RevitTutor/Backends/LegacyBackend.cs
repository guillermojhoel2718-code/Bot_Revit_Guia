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
    /// <summary>
    /// Clase interna para deserializar la respuesta JSON de Gemini.
    /// </summary>
    public class IaSimpleAnswer
    {
        public string Text { get; set; } = string.Empty;
        public string? Message { get; set; }
        public Destination? SuggestedDestination { get; set; }
    }

    /// <summary>
    /// Backend para versiones de Revit < 2027 o sin soporte MCP.
    /// </summary>
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
                    fallback.Text = $"{fallback.Message}\n\nNota: No pude contactar a la IA de Gemini: {ex.Message}. Uso reglas locales.";
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
            return q.Contains("como") || q.Contains("cómo") || q.Contains("crear") || q.Contains("crer") ||
                   q.Contains("dibujar") || q.Contains("dibujr") || q.Contains("hacer") || q.Contains("hacr") ||
                   q.Contains("modelar") || q.Contains("modelr") || q.Contains("trabe") || q.Contains("viga") ||
                   q.Contains("tuberia") || q.Contains("tuber") || q.Contains("ducto") || q.Contains("duct") ||
                   q.Contains("pilar") || q.Contains("columna") || q.Contains("muro") || q.Contains("suelo") ||
                   q.Contains("mostr") || q.Contains("ensen") || q.Contains("enseñ");
        }

        private TutorAnswer BuildLocalAnswer(string question, ModelContext context, bool hasKey)
        {
            var suggestedDestination = new Destination();
            string message = "";
            string q = question.ToLower()
                .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u");

            if (q.Contains("muro"))
            {
                suggestedDestination.CategoryName = "OST_Walls";
                suggestedDestination.Highlight = true;
                message = "Buscando muros...";
            }
            else if (q.Contains("suelo") || q.Contains("losa"))
            {
                suggestedDestination.CategoryName = "OST_Floors";
                suggestedDestination.Highlight = true;
                message = "Buscando suelos/losas...";
            }
            else if (q.Contains("ocult") || q.Contains("apagar"))
            {
                suggestedDestination.Action = "HIDE_CATEGORY";
                if (q.Contains("muro")) suggestedDestination.CategoryName = "OST_Walls";
                else if (q.Contains("suelo") || q.Contains("losa")) suggestedDestination.CategoryName = "OST_Floors";
                message = "Ocultando categoría...";
            }
            else if (q.Contains("mostr") || q.Contains("prend"))
            {
                if (q.Contains("todo")) suggestedDestination.Action = "SHOW_ALL";
                else suggestedDestination.Action = "SHOW_CATEGORY";
                message = "Mostrando elementos...";
            }
            else
            {
                message = "Soy tu tutor guía. Puedo seleccionar elementos, ocultarlos o explicarte procesos.";
            }

            return new TutorAnswer { Message = message, Text = message, SuggestedDestination = suggestedDestination };
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
                throw new HttpRequestException($"Gemini API Error ({(int)response.StatusCode}): {errorBody}");
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
            return $@"Eres un Tutor experto en Autodesk Revit. 
Tu objetivo es dar EXPLICACIONES PASO A PASO detalladas de cómo realizar acciones en la interfaz de Revit.
NO des consejos generales, da pasos procedimentales (ej: 'Pestaña X > Panel Y > Herramienta Z').

Contexto del modelo actual:
- Vista activa: {context.VistaActual}
- Elementos seleccionados: {context.ElementosSeleccionados}

Pregunta del usuario: {question}

Responde SIEMPRE en formato JSON puro con esta estructura:
{{
  ""message"": ""Tu explicación procedural paso a paso"",
  ""suggestedDestination"": {{
    ""categoryName"": ""OST_Walls (opcional)"",
    ""highlight"": true,
    ""action"": ""HIDE_CATEGORY, SHOW_CATEGORY o nada""
  }}
}}";
        }
    }

    public class GeminiResponse
    {
        public List<Candidate> Candidates { get; set; }
    }

    public class Candidate
    {
        public Content Content { get; set; }
    }

    public class Content
    {
        public List<Part> Parts { get; set; }
    }

    public class Part
    {
        public string Text { get; set; }
    }
}
