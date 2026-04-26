using System.Threading.Tasks;

namespace RevitTutor
{
    /// <summary>
    /// Respuesta del tutor devuelta por el backend de IA.
    /// </summary>
    public class TutorAnswer
    {
        public string Text { get; set; } = string.Empty;
        public string? Message { get; set; }
        public string? Summary { get; set; } // Resumen corto para el sprite
        public Destination? SuggestedDestination { get; set; }
    }

    /// <summary>
    /// Interfaz principal para interactuar con la IA desde el panel de Revit.
    /// Permite soportar diferentes implementaciones (BYOK Legacy vs MCP 2027+).
    /// </summary>
    public interface IRevitTutorBackend
    {
        /// <summary>
        /// Obtiene el contexto actual del modelo.
        /// </summary>
        Task<ModelContext> GetModelContextAsync();

        /// <summary>
        /// Navega a un destino (vista o elemento) en el modelo.
        /// </summary>
        Task NavigateToDestinationAsync(Destination destination);

        /// <summary>
        /// Envía una pregunta a la IA junto con el contexto del modelo.
        /// </summary>
        Task<TutorAnswer> AskQuestionAsync(string question, ModelContext context);
    }
}
