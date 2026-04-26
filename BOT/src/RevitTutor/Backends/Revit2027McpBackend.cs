using System;
using System.Threading.Tasks;

namespace RevitTutor
{
    /// <summary>
    /// Backend preparado para Revit 2027+.
    /// En Revit 2027+, el Revit Public MCP Server (Tech Preview) proporciona un servidor MCP 
    /// que expone herramientas estándar (tools) e información del modelo (resources).
    /// En lugar de reinventar la extracción de contexto, este backend hablará el protocolo MCP.
    /// </summary>
    public class Revit2027McpBackend : IRevitTutorBackend
    {
        public Revit2027McpBackend()
        {
            // TODO: Inicializar la conexión o cliente MCP local si es necesario.
        }

        public Task<ModelContext> GetModelContextAsync()
        {
            // TODO: Usar herramientas del Revit MCP Server para obtener la lista de vistas, categorías, etc.
            throw new NotImplementedException("Conexión con Revit Public MCP Server pendiente.");
        }

        public Task NavigateToDestinationAsync(Destination destination)
        {
            // TODO: Invocar herramienta MCP (ej. navigate_to_destination)
            throw new NotImplementedException("Navegación vía MCP pendiente.");
        }

        public Task<TutorAnswer> AskQuestionAsync(string question, ModelContext context)
        {
            // TODO: En el flujo MCP, es posible que el contexto se pida dinámicamente por el LLM 
            // a través de tool calls al Revit MCP Server. Aquí orquestaremos ese flujo.
            throw new NotImplementedException("Preguntas vía MCP pendiente.");
        }
    }
}
