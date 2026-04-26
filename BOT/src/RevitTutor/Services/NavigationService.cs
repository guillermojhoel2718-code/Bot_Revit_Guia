using System;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitTutor
{
    /// <summary>
    /// Servicio responsable de realizar la navegación visual en Revit.
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
                if (!string.IsNullOrEmpty(destination.ViewId))
                {
                    View view = null;
                    if (long.TryParse(destination.ViewId, out long viewIdLong))
                    {
                        view = doc.GetElement(new ElementId(viewIdLong)) as View;
                    }
                    else if (destination.ViewId.ToLower().Contains("3d"))
                    {
                        view = new FilteredElementCollector(doc)
                            .OfClass(typeof(View3D))
                            .Cast<View3D>()
                            .FirstOrDefault(v => !v.IsTemplate);
                    }

                    if (view != null && uiDoc.ActiveView.Id != view.Id)
                    {
                        uiDoc.ActiveView = view;
                    }
                }

                // 2. Acciones de visibilidad y selección
                var elements = new System.Collections.Generic.List<ElementId>();
                BuiltInCategory bic = BuiltInCategory.INVALID;

                if (!string.IsNullOrEmpty(destination.CategoryName))
                {
                    string catSearch = destination.CategoryName.StartsWith("OST_") ? destination.CategoryName : "OST_" + destination.CategoryName;
                    if (Enum.TryParse(catSearch, true, out BuiltInCategory foundBic)) bic = foundBic;
                }

                using (Transaction t = new Transaction(doc, "Tutor Action"))
                {
                    t.Start();
                    
                    var activeView = uiDoc.ActiveView;
                    
                    if (bic != BuiltInCategory.INVALID)
                    {
                        elements = new FilteredElementCollector(doc, activeView.Id)
                            .OfCategory(bic)
                            .WhereElementIsNotElementType()
                            .ToElementIds()
                            .ToList();
                    }

                    if (destination.Action == "HIDE_CATEGORY" && bic != BuiltInCategory.INVALID)
                    {
                        if (activeView.CanCategoryBeHidden(new ElementId(bic)))
                            activeView.SetCategoryHidden(new ElementId(bic), true);
                    }
                    else if (destination.Action == "SHOW_CATEGORY" && bic != BuiltInCategory.INVALID)
                    {
                        if (activeView.CanCategoryBeHidden(new ElementId(bic)))
                            activeView.SetCategoryHidden(new ElementId(bic), false);
                    }
                    else if (destination.Action == "HIDE" && elements.Any())
                    {
                        activeView.HideElements(elements);
                    }
                    else if (destination.Action == "ISOLATE" && elements.Any())
                    {
                        activeView.IsolateElementsTemporary(elements);
                    }
                    else if (destination.Action == "SHOW_ALL")
                    {
                        var cats = new[] { BuiltInCategory.OST_Walls, BuiltInCategory.OST_Floors, BuiltInCategory.OST_Doors, BuiltInCategory.OST_StructuralColumns };
                        foreach (var cat in cats)
                        {
                            if (activeView.CanCategoryBeHidden(new ElementId(cat)))
                                activeView.SetCategoryHidden(new ElementId(cat), false);
                        }
                    }

                    t.Commit();
                }

                // Selección fuera de la transacción y sin delay raro
                if (elements.Any() && destination.Highlight)
                {
                    uiDoc.Selection.SetElementIds(elements);
                    uiDoc.ShowElements(elements);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            }

            return Task.CompletedTask;
        }
    }
}
