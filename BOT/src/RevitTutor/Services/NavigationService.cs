using System;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitTutor
{
    /// <summary>
    /// Servicio responsable de realizar la navegación visual en Revit.
    /// Solo lectura: abre vistas y resalta elementos, pero no los modifica.
    /// </summary>
    public static class NavigationService
    {
        public static Task NavigateToAsync(UIApplication uiApp, Destination destination)
        {
            try
            {
                var uiDoc = uiApp.ActiveUIDocument;
                var doc = uiDoc?.Document;

                if (uiDoc == null || doc == null || destination == null)
                    return Task.CompletedTask;

                // 1. Cambiar la vista activa si se especifica ViewId
                if (!string.IsNullOrEmpty(destination.ViewId) && long.TryParse(destination.ViewId, out long viewIdLong))
                {
                    var viewId = new ElementId(viewIdLong);
                    var view = doc.GetElement(viewId) as View;
                    if (view != null && view.CanBePrinted) // Asegurar que es una vista gráfica válida
                    {
                        uiDoc.ActiveView = view;
                    }
                }

                // 2. Seleccionar elementos de una categoría si se especifica
                if (!string.IsNullOrEmpty(destination.CategoryName))
                {
                    // Buscar elementos en la vista actual que coincidan con la categoría
                    var elementsInCategory = new FilteredElementCollector(doc, uiDoc.ActiveView.Id)
                        .WhereElementIsNotElementType()
                        .ToElements()
                        .Where(e => e.Category != null && e.Category.Name.Equals(destination.CategoryName, StringComparison.OrdinalIgnoreCase))
                        .Select(e => e.Id)
                        .ToList();

                    if (elementsInCategory.Any())
                    {
                        uiDoc.Selection.SetElementIds(elementsInCategory);
                        
                        if (destination.Highlight)
                        {
                            // Si se solicita highlight, podemos hacer zoom a los elementos seleccionados
                            uiDoc.ShowElements(elementsInCategory);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // En un plugin real, se debería hacer log del error.
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            }

            return Task.CompletedTask;
        }
    }
}
