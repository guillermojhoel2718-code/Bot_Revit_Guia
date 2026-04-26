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

            // Intentamos Gemini para casi todo lo que no sea una acción directa local
            if (hasKey && DetectIaIntent(question))
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
                    // Fallback silencioso a lógica local si falla la red
                    System.Diagnostics.Debug.WriteLine("Gemini Error: " + ex.Message);
                }
            }

            return BuildLocalAnswer(question, context);
        }

        public Task<ModelContext> GetModelContextAsync()
        {
            return Task.FromResult(ModelContextService.BuildContext(_uiApp));
        }

        public Task NavigateToDestinationAsync(Destination destination)
        {
            return NavigationService.NavigateToAsync(_uiApp, destination);
        }

        private bool DetectIaIntent(string q)
        {
            var low = q.ToLowerInvariant();
            // Si pregunta "como", "porque", "que es" o pide explicaciones, va a la IA
            return low.Contains("como") || low.Contains("cómo") || low.Contains("que") || 
                   low.Contains("explic") || low.Contains("ensena") || low.Contains("enseña") ||
                   low.Contains("paso") || low.Contains("duda");
        }

        private TutorAnswer BuildLocalAnswer(string question, ModelContext context)
        {
            var dest = new Destination();
            string q = question.ToLower().Replace("á","a").Replace("é","e").Replace("í","i").Replace("ó","o").Replace("ú","u");
            string msg = "Soy tu tutor de Revit. Puedo resaltar elementos o explicarte procesos.";

            // Categorías (con algo de tolerancia a errores)
            bool foundCat = false;
            if (q.Contains("muro") || q.Contains("wall") || q.Contains("mro")) { dest.CategoryName = "OST_Walls"; foundCat = true; }
            else if (q.Contains("suelo") || q.Contains("losa") || q.Contains("floor") || q.Contains("selo")) { dest.CategoryName = "OST_Floors"; foundCat = true; }
            else if (q.Contains("pilar") || q.Contains("columna") || q.Contains("column")) { dest.CategoryName = "OST_StructuralColumns"; foundCat = true; }
            else if (q.Contains("puerta") || q.Contains("door")) { dest.CategoryName = "OST_Doors"; foundCat = true; }

            // Acciones
            if (q.Contains("ocult") || q.Contains("apagar") || q.Contains("escond"))
            {
                dest.Action = "HIDE_CATEGORY";
                msg = "Ocultando elementos seleccionados.";
            }
            else if (q.Contains("mostr") || q.Contains("muestr") || q.Contains("ver") || q.Contains("prend") || q.Contains("selec") || q.Contains("donde"))
            {
                if (q.Contains("todo")) dest.Action = "SHOW_ALL";
                else dest.Action = "SHOW_CATEGORY";
                dest.Highlight = true;
                msg = "Buscando y seleccionando los elementos en el modelo.";
            }
            else if (q.Contains("3d"))
            {
                dest.ViewId = "3D";
                msg = "Cambiando a vista 3D.";
            }

            return new TutorAnswer 
            { 
                Message = msg, 
                Text = msg, 
                SuggestedDestination = (foundCat || dest.Action != null || dest.ViewId != null) ? dest : null 
            };
        }

        private async Task<IaSimpleAnswer?> CallGeminiAsync(string question, ModelContext context, string apiKey)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var prompt = $@"Eres un Tutor de Revit. Responde PASO A PASO. 
Usa JSON: {{ ""message"": ""Explicación"", ""suggestedDestination"": {{ ""categoryName"": ""OST_Walls"", ""highlight"": true, ""action"": ""SHOW_CATEGORY"" }} }}
Pregunta: {question}
Contexto: {context.VistaActual}";
            
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
