using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

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

    /// <summary>
    /// Servicio de solo lectura que extrae contexto del modelo Revit activo.
    /// NO modifica el modelo en ningún caso.
    /// </summary>
    public static class ModelContextService
    {
        /// <summary>
        /// Construye un snapshot del contexto actual del modelo.
        /// </summary>
        public static ModelContext BuildContext(UIApplication uiApp)
        {
            var uiDoc = uiApp.ActiveUIDocument;
            var doc = uiDoc.Document;

            var context = new ModelContext
            {
                NombreProyecto = doc.Title ?? "Sin título",
                VistaActual = uiDoc.ActiveView?.Name ?? "Ninguna",
                TipoVistaActual = uiDoc.ActiveView?.ViewType.ToString() ?? "Desconocido",
                VistasDisponibles = ObtenerVistas(doc),
                CategoriasActivas = ObtenerCategorias(doc),
                ElementosSeleccionados = uiDoc.Selection.GetElementIds().Count,
                IdsSeleccionados = ObtenerIdsSeleccionados(uiDoc)
            };

            return context;
        }

        /// <summary>
        /// Obtiene las vistas del modelo que NO son plantillas.
        /// </summary>
        private static List<string> ObtenerVistas(Document doc)
        {
            try
            {
                using var collector = new FilteredElementCollector(doc);
                return collector
                    .OfClass(typeof(View))
                    .Cast<View>()
                    .Where(v => !v.IsTemplate)
                    .Select(v => $"{v.ViewType}: {v.Name}")
                    .OrderBy(name => name)
                    .Take(50) // Limitar para no saturar el JSON
                    .ToList();
            }
            catch
            {
                return new List<string> { "Error al leer vistas" };
            }
        }

        /// <summary>
        /// Obtiene las categorías de modelo activas (visibles) en la vista actual.
        /// </summary>
        private static List<string> ObtenerCategorias(Document doc)
        {
            try
            {
                var categorias = doc.Settings.Categories;
                var resultado = new List<string>();

                foreach (Category cat in categorias)
                {
                    // Solo categorías de modelo (no de anotación ni internas)
                    if (cat.CategoryType == CategoryType.Model && cat.Name != null)
                    {
                        resultado.Add(cat.Name);
                    }
                }

                return resultado.OrderBy(c => c).ToList();
            }
            catch
            {
                return new List<string> { "Error al leer categorías" };
            }
        }

        /// <summary>
        /// Obtiene los IDs de los elementos seleccionados (máximo 20 para no saturar).
        /// </summary>
        private static List<string> ObtenerIdsSeleccionados(UIDocument uiDoc)
        {
            try
            {
                return uiDoc.Selection
                    .GetElementIds()
                    .Take(20)
                    .Select(id => id.ToString())
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}
