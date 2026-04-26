using System.Collections.Generic;

namespace RevitTutor
{
    /// <summary>
    /// Modelo de datos que representa el contexto actual del modelo Revit.
    /// Se serializa a JSON para enviar junto con la pregunta del usuario.
    /// </summary>
    public class ModelContext
    {
        public string NombreProyecto { get; set; } = string.Empty;
        public string VistaActual { get; set; } = string.Empty;
        public string TipoVistaActual { get; set; } = string.Empty;
        public List<string> VistasDisponibles { get; set; } = new();
        public List<string> CategoriasActivas { get; set; } = new();
        public int ElementosSeleccionados { get; set; }
        public List<string> IdsSeleccionados { get; set; } = new();
    }
}
