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
    public class LegacyBackend : IRevitTutorBackend
    {
        private readonly UIApplication _uiApp;

        public LegacyBackend(UIApplication uiApp)
        {
            _uiApp = uiApp;
        }

        public async Task<TutorAnswer> AskQuestionAsync(string question, ModelContext context)
        {
            var config = ConfigService.LoadConfig();
            string apiKey = config.GeminiApiKey;
            LlmCommand cmd = null;
            string geminiError = null;
            bool isHowTo = DetectHowToQuestion(question);

            // 1) PRIORIDAD: Detectar si es una pregunta educativa/tutorial
            if (isHowTo)
            {
                cmd = LocalCommandParser.Parse(question);
                cmd.Action = "how_to"; // Forzamos acción how_to si detectamos la intención educativa
            }
            else
            {
                // Prioridad Local para comandos operativos
                cmd = LocalCommandParser.Parse(question);
            }

            // Si es QA o Wizard, forzamos local
            bool isSpecialAction = cmd.Action == "model_quality_check" || cmd.Action == "prepare_structural_view" || cmd.Action == "how_to";
            bool needsAiCommand = !isSpecialAction && cmd.Action == "none";

            if (needsAiCommand && !string.IsNullOrEmpty(apiKey))
            {
                try { cmd = await CallGeminiForCommandAsync(question, context, apiKey); }
                catch (Exception ex) { geminiError = $"IA no disponible: {ex.Message}"; }
            }

            if (cmd == null) cmd = LocalCommandParser.Parse(question);

            // 2) EJECUCIÓN LOCAL
            CommandExecutionResult execResult;
            if (cmd.Action == "how_to")
            {
                execResult = GetStaticTutorial(cmd, question);
            }
            else
            {
                execResult = await CommandExecutor.ExecuteAsync(cmd, context, _uiApp);
            }

            // 3) EXPLICACIÓN EDUCATIVA ADICIONAL (Opcional por IA)
            string aiEducationalText = null;
            if (isHowTo && !string.IsNullOrEmpty(apiKey))
            {
                try { aiEducationalText = await CallGeminiForEducationalExplanationAsync(question, context, execResult, apiKey); }
                catch { /* fallback silencioso */ }
            }

            // 4) CONSTRUIR MENSAJE FINAL
            string finalMessage = BuildFinalEducationalMessage(cmd, execResult, context, aiEducationalText);

            return new TutorAnswer
            {
                Message = finalMessage,
                Text = finalMessage,
                Summary = execResult.UserMessageShort,
                SuggestedDestination = execResult.ToDestination()
            };
        }

        private string BuildFinalEducationalMessage(LlmCommand cmd, CommandExecutionResult execResult, ModelContext context, string aiText)
        {
            if (cmd.Action == "none")
            {
                return "No entendí la instrucción, pero puedo ayudarte con:\n" +
                       "• Revisión de calidad del modelo (QA)\n" +
                       "• Preparar vistas estructurales\n" +
                       "• Mostrar/ocultar y seleccionar categorías estructurales\n" +
                       "• Tutoriales sobre cómo modelar muros, vigas y columnas.";
            }

            StringBuilder sb = new StringBuilder();

            // A) Acción realizada
            sb.AppendLine("✅ " + (string.IsNullOrEmpty(execResult.ActionDescription) ? execResult.UserMessage : execResult.ActionDescription));
            sb.AppendLine();

            // B) Capa Educativa Local
            if (!string.IsNullOrEmpty(execResult.EducationalContext))
            {
                sb.AppendLine("🎓 Por qué es importante:");
                sb.AppendLine(execResult.EducationalContext);
                sb.AppendLine();
            }

            // C) Pasos de QA o Acción
            if (execResult.Steps != null && execResult.Steps.Length > 0)
            {
                sb.AppendLine("📋 Detalles:");
                foreach (var step in execResult.Steps) sb.AppendLine($"• {step}");
                sb.AppendLine();
            }

            // D) Explicación de IA (si existe)
            if (!string.IsNullOrEmpty(aiText))
            {
                sb.AppendLine("📖 Tutorial Avanzado:");
                sb.AppendLine(aiText);
                sb.AppendLine();
            }

            // E) Tip final
            sb.AppendLine("💡 Tip del Tutor:");
            if (cmd.Action == "model_quality_check")
                sb.AppendLine("• Recuerda que un modelo limpio es la base para un análisis estructural exitoso.");
            else
                sb.AppendLine("• Puedes pedirme 'explica cómo modelar...' para aprender más sobre Revit.");

            return sb.ToString();
        }

        private async Task<string> CallGeminiForEducationalExplanationAsync(string question, ModelContext context, CommandExecutionResult localResult, string apiKey)
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(20);

            var prompt = $@"El plugin RevitTutor ejecutó: {localResult.ActionDescription}.
Pregunta del usuario: {question}.
Contexto: Vista '{context.VistaActual}', Tipo: {context.TipoVistaActual}.

Eres un tutor para estudiantes de ingeniería. Explica brevemente por qué esta acción es importante y cómo modelar correctamente.
Formato:
- Contexto (1 frase)
- Pasos clave (lista corta)
- Tip BIM (1 frase)
Responde en español.";

            var payload = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={apiKey}";
            
            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);
            if (!response.IsSuccessStatusCode) return null;
            
            var responseJson = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;
            if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0) return null;

            return candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
        }

        public Task<ModelContext> GetModelContextAsync()
        {
            return Task.FromResult(ModelContextService.BuildContext(_uiApp));
        }

        public Task NavigateToDestinationAsync(Destination destination)
        {
            return Task.CompletedTask;
        }

        private bool DetectHowToQuestion(string q)
        {
            if (string.IsNullOrWhiteSpace(q)) return false;
            var low = q.ToLowerInvariant().Replace("á","a").Replace("é","e").Replace("í","i").Replace("ó","o").Replace("ú","u");
            return low.Contains("como") || low.Contains("explica") || low.Contains("paso") || 
                   low.Contains("dibuja") || low.Contains("crea") || low.Contains("hace") || 
                   low.Contains("ensena") || low.Contains("tutorial") || low.Contains("guia");
        }

        private string LastGeminiError = "";

        private async Task<LlmCommand> CallGeminiForCommandAsync(string question, ModelContext context, string apiKey)
        {
            LastGeminiError = "";
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            var prompt = BuildCommandPromptForGemini(question, context);

            var payload = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } }
            };

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={apiKey}";
            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                LastGeminiError = $"Error {response.StatusCode}: {responseJson}";
                return null;
            }

            var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;
            if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0) return null;

            var contentElem = candidates[0].GetProperty("content");
            var parts = contentElem.GetProperty("parts");
            if (parts.GetArrayLength() == 0) return null;

            string text = parts[0].GetProperty("text").GetString();
            if (string.IsNullOrEmpty(text)) return null;

            text = text.Replace("```json", "").Replace("```", "").Trim();

            try
            {
                return JsonSerializer.Deserialize<LlmCommand>(text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return null;
            }
        }

        private string BuildCommandPromptForGemini(string question, ModelContext context)
        {
            return $@"Eres un experto en Revit API y tutor educativo. Traduce la pregunta a JSON.
Contexto: Vista '{context.VistaActual}', Tipo: {context.TipoVistaActual}.

Esquema JSON:
{{
  ""action"": ""select_category""|""hide_category""|""show_category""|""isolate_category""|""unhide_all""|""reset_all_overrides""|""paint""|""reset_paint""|""go_to_view_3d""|""go_to_view_plan""|""show_project_parameters""|""none"",
  ""categories"": [""walls"", ""floors"", ""doors"", ""windows"", ""columns"", ""beams""],
  ""viewName"": ""string"",
  ""color"": ""red""|""blue""|""green""|""yellow""|""orange""|""none"",
  ""maxVolume"": number,
  ""minVolume"": number,
  ""explanation"": ""Lista numerada de 3-5 pasos sobre cómo hacerlo manualmente + un Tip.""
}}

Instrucciones:
- Si es una pregunta de 'cómo hacer' algo manual, pon los pasos en 'explanation'.
- 'categories' es una LISTA (siempre).
- Responde SOLO JSON.";
        }

        private CommandExecutionResult GetStaticTutorial(LlmCommand cmd, string question)
        {
            var result = new CommandExecutionResult();
            var steps = new List<string>();

            if (cmd.Categories.Contains("walls"))
            {
                result.ActionDescription = "Tutorial: Cómo modelar Muros Estructurales";
                steps.Add("Selecciona la pestaña 'Estructura' > 'Muro'.");
                steps.Add("En el selector de tipos, elige un muro de concreto o mampostería.");
                steps.Add("Configura la altura (hasta el nivel superior) y la línea de ubicación.");
                steps.Add("Dibuja el muro haciendo clic en el origen y fin.");
                result.EducationalContext = "Los muros estructurales deben estar correctamente vinculados a los niveles para asegurar la transferencia de cargas en el modelo analítico.";
            }
            else if (cmd.Categories.Contains("floors"))
            {
                result.ActionDescription = "Tutorial: Cómo modelar Suelos/Losas";
                steps.Add("Ve a 'Estructura' > 'Suelo' > 'Suelo estructural'.");
                steps.Add("Usa las herramientas de dibujo para definir el contorno (boceto cerrado).");
                steps.Add("Asegúrate de que no haya líneas encimadas o espacios abiertos.");
                steps.Add("Haz clic en el check verde para finalizar.");
                result.EducationalContext = "El contorno del suelo define el área de carga. Asegúrate de alinear los bordes con los ejes de vigas o muros.";
            }
            else if (cmd.Categories.Contains("beams"))
            {
                result.ActionDescription = "Tutorial: Cómo modelar Vigas";
                steps.Add("Ve a 'Estructura' > 'Viga'.");
                steps.Add("Carga la familia de viga necesaria si no existe.");
                steps.Add("Dibuja de eje a eje o usa la herramienta 'En rejillas'.");
                result.EducationalContext = "Las vigas se dibujan en el nivel actual pero su geometría se proyecta hacia abajo por defecto.";
            }
            else
            {
                result.ActionDescription = "Guía del Tutor";
                steps.Add("Puedo ayudarte con: Muros, Suelos, Vigas y Columnas.");
                steps.Add("Intenta preguntar: '¿Cómo modelo un muro?'");
                result.EducationalContext = "BIM no es solo dibujar, es construir digitalmente con información precisa.";
            }

            result.Steps = steps.ToArray();
            result.UserMessage = "He preparado una guía paso a paso para ti.";
            result.UserMessageShort = "Guía mostrada";
            return result;
        }
    }
}
