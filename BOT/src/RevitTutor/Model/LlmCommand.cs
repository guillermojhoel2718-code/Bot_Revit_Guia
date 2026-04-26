using System;

namespace RevitTutor
{
    public class LlmCommand
    {
        public string Action { get; set; }          // "show_category", "hide_category", "select_category", "show_annotations", "hide_annotations", "show_cad_links", "hide_cad_links", "go_to_view_plan", "go_to_view_elevation", "go_to_view_3d", "paint", "reset_paint", "show_project_parameters", "none"
        public List<string> Categories { get; set; } = new List<string>(); // "walls", "floors", "columns", "beams", etc.
        public string ViewName { get; set; }        // nombre de vista target, p.ej. "Nivel 1", "Alzado Norte", "3D Vista 1"
        public string Color { get; set; }           // "red", "blue", "green", "yellow", "orange", etc.
        public bool AffectAnnotations { get; set; }
        public bool AffectCadLinks { get; set; }
        public bool UseSelection { get; set; } // Si es true, ignora categorías y usa selección actual
        public double? MaxVolume { get; set; } // En m3
        public double? MinVolume { get; set; } // En m3
        public string Explanation { get; set; }
    }
}
