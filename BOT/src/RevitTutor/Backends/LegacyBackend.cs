using System;
using System.Threading.Tasks;
using Autodesk.Revit.UI;

namespace RevitTutor
{
    /// <summary>
    /// Backend Legacy para versiones de Revit 2026 y anteriores.
    /// Utiliza lógica propia (BYOK) para interactuar con la IA y usa ModelContextService y NavigationService.
    /// </summary>
    public class LegacyBackend : IRevitTutorBackend
    {
        private readonly UIApplication _uiApp;

        public LegacyBackend(UIApplication uiApp)
        {
            _uiApp = uiApp;
        }

        public Task<ModelContext> GetModelContextAsync()
        {
            // Se ejecuta de manera síncrona internamente, pero envuelto en Task para la interfaz.
            var context = ModelContextService.BuildContext(_uiApp);
            return Task.FromResult(context);
        }

        public async Task NavigateToDestinationAsync(Destination destination)
        {
            await NavigationService.NavigateToAsync(_uiApp, destination);
        }

        public async Task<TutorAnswer> AskQuestionAsync(string question, ModelContext context)
        {
            // TODO: Implementar la llamada a la API de IA (BYOK)
            await Task.Delay(500); // Simulando red
            
            // Ejemplo de respuesta que simula el uso de NavigationService y ModelContextService
            // Si el usuario pregunta "Muéstrame los muros", la IA podría devolver un Destination:
            var suggestedDestination = new Destination();
            
            if (question.ToLower().Contains("muro") || question.ToLower().Contains("wall"))
            {
                suggestedDestination.CategoryName = "Walls";
                suggestedDestination.Highlight = true;
                
                // Si la IA sabe en qué vista estamos por el ModelContext, podría no cambiar la vista,
                // o podría buscar una vista 3D en las VistasDisponibles del contexto y sugerirla.
            }

            string apiKey = ConfigService.LoadApiKey();
            string responsePrefix = string.IsNullOrEmpty(apiKey)
                ? "No has configurado tu API key; uso solo reglas simples. "
                : "He detectado tu API key; en la siguiente versión la usaré para llamar a la IA externa. ";

            return new TutorAnswer 
            { 
                Text = responsePrefix + "He analizado el ModelContext y he usado NavigationService si pediste ver elementos.",
                SuggestedDestination = string.IsNullOrEmpty(suggestedDestination.CategoryName) && string.IsNullOrEmpty(suggestedDestination.ViewId) ? null : suggestedDestination
            };
        }
    }
}
