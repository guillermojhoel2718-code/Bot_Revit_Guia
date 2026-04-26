using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitTutor
{

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
        /// Obtiene las vistas del modelo que NO son plantillas, incluyendo su ID.
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
                    .Select(v => $"[{v.Id.Value}] {v.ViewType}: {v.Name}")
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
        /// Obtiene las categorías de modelo activas con su conteo de elementos.
        /// </summary>
        private static List<string> ObtenerCategorias(Document doc)
        {
            try
            {
                var allElements = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .ToElements();

                var categoryCounts = new Dictionary<string, int>();

                foreach (var el in allElements)
                {
                    if (el.Category != null && el.Category.CategoryType == CategoryType.Model)
                    {
                        string catName = el.Category.Name;
                        if (!categoryCounts.ContainsKey(catName))
                        {
                            categoryCounts[catName] = 0;
                        }
                        categoryCounts[catName]++;
                    }
                }

                return categoryCounts
                    .Select(kvp => $"{kvp.Key}: {kvp.Value} elementos")
                    .OrderBy(c => c)
                    .ToList();
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
