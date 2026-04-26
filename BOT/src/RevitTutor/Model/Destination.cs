namespace RevitTutor
{
    /// <summary>
    /// Representa un destino de navegación en Revit (ej. un elemento, una vista).
    /// </summary>
    public class Destination
    {
        public string? ViewId { get; set; }
        public string? CategoryName { get; set; }
        public bool Highlight { get; set; }
    }
}
