using System;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitTutor
{
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

                // 1. Cambio de vista
                if (!string.IsNullOrEmpty(destination.ViewId))
                {
                    View view = null;
                    if (long.TryParse(destination.ViewId, out long vid)) view = doc.GetElement(new ElementId(vid)) as View;
                    else if (destination.ViewId.ToLower().Contains("3d"))
                    {
                        view = new FilteredElementCollector(doc).OfClass(typeof(View3D)).Cast<View3D>().FirstOrDefault(v => !v.IsTemplate);
                    }
                    if (view != null && uiDoc.ActiveView.Id != view.Id) uiDoc.ActiveView = view;
                }

                // 2. Acciones de visibilidad
                var elements = new System.Collections.Generic.List<ElementId>();
                BuiltInCategory bic = BuiltInCategory.INVALID;

                if (!string.IsNullOrEmpty(destination.CategoryName))
                {
                    string catSearch = destination.CategoryName.StartsWith("OST_") ? destination.CategoryName : "OST_" + destination.CategoryName;
                    Enum.TryParse(catSearch, true, out bic);
                }

                using (Transaction t = new Transaction(doc, "Tutor Action"))
                {
                    t.Start();
                    var activeView = uiDoc.ActiveView;
                    
                    if (bic != BuiltInCategory.INVALID)
                    {
                        elements = new FilteredElementCollector(doc, activeView.Id)
                            .OfCategory(bic).WhereElementIsNotElementType().ToElementIds().ToList();
                        
                        if (destination.Action == "HIDE_CATEGORY") activeView.SetCategoryHidden(new ElementId(bic), true);
                        else if (destination.Action == "SHOW_CATEGORY") activeView.SetCategoryHidden(new ElementId(bic), false);
                    }
                    else if (destination.Action == "SHOW_ALL")
                    {
                        var cats = new[] { BuiltInCategory.OST_Walls, BuiltInCategory.OST_Floors, BuiltInCategory.OST_Doors, BuiltInCategory.OST_StructuralColumns };
                        foreach (var c in cats) activeView.SetCategoryHidden(new ElementId(c), false);
                    }

                    t.Commit();
                }

                // 3. SELECCIÓN (Azul) - Usamos un pequeño delay para que Revit refresque la vista primero
                if (elements.Any() && destination.Highlight)
                {
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => 
                    {
                        await Task.Delay(200); // Delay suficiente para el refresco de UI
                        uiDoc.Selection.SetElementIds(elements);
                        uiDoc.ShowElements(elements); // Esto los centra en pantalla
                    });
                }
            }
            catch { }

            return Task.CompletedTask;
        }
    }
}
