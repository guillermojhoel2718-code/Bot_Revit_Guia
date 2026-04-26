using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitTutor
{
    public class CommandExecutionResult
    {
        public string UserMessage { get; set; } = string.Empty; // Compatibilidad anterior
        public string UserMessageShort { get; set; } = string.Empty; // Compatibilidad anterior
        
        public string ActionDescription { get; set; } = string.Empty; // Qué se hizo
        public string EducationalContext { get; set; } = string.Empty; // Por qué es útil
        public string[] Steps { get; set; } = Array.Empty<string>(); // Pasos realizados
        
        public Destination Destination { get; set; } = new Destination();
        public Destination ToDestination() => Destination;
    }

    public class CommandExecutor
    {
        public static async Task<CommandExecutionResult> ExecuteAsync(LlmCommand cmd, ModelContext context, UIApplication uiApp)
        {
            var result = new CommandExecutionResult();
            if (cmd == null || string.IsNullOrEmpty(cmd.Action) || cmd.Action == "none")
            {
                result.UserMessage = "";
                return result;
            }

            // Ejecutamos la lógica de la API de Revit dentro de IExternalEventHandler
            return await App.TaskHandler.RunAsync(App.RevitEvent, (app) =>
            {
                try
                {
                    var uiDoc = app.ActiveUIDocument;
                    var doc = uiDoc?.Document;
                    if (uiDoc == null || doc == null) return result;

                    // 0. CAMBIO DE VISTA PREVIO (si se especifica una vista en cualquier comando)
                    if (!string.IsNullOrEmpty(cmd.ViewName) && cmd.Action != "go_to_view_plan" && cmd.Action != "go_to_view_elevation")
                    {
                        var allViews = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>().Where(v => !v.IsTemplate).ToList();
                        var target = allViews.FirstOrDefault(v => v.Name.ToLower().Contains(cmd.ViewName.ToLower()));
                        if (target != null && uiDoc.ActiveView.Id != target.Id) uiDoc.ActiveView = target;
                    }

                    // 1. NAVEGACION DE VISTAS (no requieren transacción manual)
                    if (cmd.Action == "go_to_view_3d")
                    {
                        var view3D = new FilteredElementCollector(doc).OfClass(typeof(View3D)).Cast<View3D>().FirstOrDefault(v => !v.IsTemplate);
                        if (view3D != null && uiDoc.ActiveView.Id != view3D.Id) uiDoc.ActiveView = view3D;
                        result.UserMessage = "He cambiado a la vista 3D principal.";
                        result.UserMessageShort = "Vista 3D activada";
                        result.ActionDescription = "Cambio a vista 3D principal.";
                        result.EducationalContext = "La vista 3D permite visualizar la integridad estructural del edificio y detectar colisiones entre diferentes sistemas.";
                    }
                    if (cmd.Action == "go_to_view_plan" || cmd.Action == "go_to_view_elevation")
                    {
                        // Buscamos según el tipo de acción
                        Type viewType = cmd.Action == "go_to_view_plan" ? typeof(ViewPlan) : typeof(ViewSection);
                        var allViews = new FilteredElementCollector(doc).OfClass(viewType).Cast<View>().Where(v => !v.IsTemplate).ToList();
                        
                        View targetView = null;
                        if (!string.IsNullOrEmpty(cmd.ViewName))
                        {
                            // Intento 1: Coincidencia exacta (ignorando caso)
                            targetView = allViews.FirstOrDefault(v => v.Name.Equals(cmd.ViewName, StringComparison.OrdinalIgnoreCase));
                            
                            // Intento 2: Coincidencia parcial (Contiene)
                            if (targetView == null)
                                targetView = allViews.FirstOrDefault(v => v.Name.IndexOf(cmd.ViewName, StringComparison.OrdinalIgnoreCase) >= 0);
                        }

                        if (targetView != null)
                        {
                            if (uiDoc.ActiveView.Id != targetView.Id) 
                                uiDoc.ActiveView = targetView;

                            string tipo = cmd.Action == "go_to_view_plan" ? "planta" : "alzado/sección";
                            result.UserMessage = $"He cambiado a la vista de {tipo}: '{targetView.Name}'.";
                            result.UserMessageShort = $"Vista {tipo} activa";
                            result.ActionDescription = $"Cambio a vista: {targetView.Name}";
                            result.EducationalContext = "Navegar entre vistas es esencial para verificar la coordinación entre niveles y alzados estructurales.";
                        }
                        else
                        {
                            string tipo = cmd.Action == "go_to_view_plan" ? "planta" : "alzado";
                            result.UserMessage = $"No encontré ninguna vista de {tipo} que contenga '{cmd.ViewName}'.\n" +
                                               "Por favor, revisa el nombre en el Navegador de Proyectos.";
                            result.UserMessageShort = "Vista no encontrada";
                        }
                        return result;
                    }
                    if (cmd.Action == "show_project_parameters")
                    {
                        var commandId = RevitCommandId.LookupPostableCommandId(PostableCommand.ProjectParameters);
                        if (commandId != null && uiApp.CanPostCommand(commandId)) uiApp.PostCommand(commandId);
                        result.UserMessage = "He abierto la ventana de Parámetros de Proyecto.";
                        result.UserMessageShort = "Parámetros abiertos";
                        result.ActionDescription = "Apertura de Parámetros de Proyecto.";
                        result.EducationalContext = "Los parámetros de proyecto permiten añadir información personalizada a los elementos, esencial para la gestión BIM y tablas de planificación.";
                        return result;
                    }

                    // ACCIONES ESPECIALES: QA y Wizards
                    if (cmd.Action == "model_quality_check")
                    {
                        return RunModelQualityCheck(doc);
                    }
                    if (cmd.Action == "prepare_structural_view")
                    {
                        return RunPrepareStructuralView(uiDoc);
                    }

                    // 2. PROCESAMIENTO DE CATEGORÍAS
                            var catIds = new List<ElementId>();
                            var catNames = new List<string>();
                            if (cmd.Categories != null && !cmd.UseSelection)
                            {
                                foreach (var cName in cmd.Categories)
                                {
                                    var bCat = MapCategory(cName);
                                    if (bCat.HasValue)
                                    {
                                        catIds.Add(new ElementId(bCat.Value));
                                        catNames.Add(cName);
                                    }
                                }
                            }

                            // 3. ACCIONES QUE REQUIEREN TRANSACCIÓN
                            using (Transaction t = new Transaction(doc, "Tutor Action"))
                            {
                                t.Start();
                                var activeView = uiDoc.ActiveView;

                                // A) Visibilidad
                                if (cmd.Action == "hide_category" || cmd.Action == "show_category" || cmd.Action == "hide_annotations" || cmd.Action == "show_annotations" || cmd.Action == "hide_cad_links" || cmd.Action == "show_cad_links")
                                {
                                    bool hide = cmd.Action.StartsWith("hide");
                                    
                                    // Si es anotación o CAD, añadimos esas categorías si no están
                                    if (cmd.Action.Contains("annotations") && !catIds.Any(id => id.IntegerValue == (int)BuiltInCategory.OST_Dimensions))
                                    {
                                        catIds.Add(new ElementId(BuiltInCategory.OST_Dimensions));
                                        catIds.Add(new ElementId(BuiltInCategory.OST_TextNotes));
                                        catIds.Add(new ElementId(BuiltInCategory.OST_Grids));
                                        catIds.Add(new ElementId(BuiltInCategory.OST_Levels));
                                        catNames.Add("Anotaciones");
                                    }
                                    if (cmd.Action.Contains("cad_links") && !catIds.Any(id => id.IntegerValue == (int)BuiltInCategory.OST_RvtLinks))
                                    {
                                        catIds.Add(new ElementId(BuiltInCategory.OST_RvtLinks));
                                        catNames.Add("Vínculos/CAD");
                                    }

                                    foreach (var cId in catIds)
                                    {
                                        if (activeView.CanCategoryBeHidden(cId)) activeView.SetCategoryHidden(cId, hide);
                                    }

                                    result.UserMessage = $"He {(hide ? "ocultado" : "mostrado")} las categorías solicitadas.";
                                    result.UserMessageShort = hide ? "Ocultado" : "Mostrado";
                                    result.ActionDescription = $"Cambio de visibilidad ({(hide ? "ocultar" : "mostrar")}).";
                                    result.EducationalContext = hide ? "Ocultar elementos temporales permite concentrarse en la precisión geométrica de la estructura." : "Mostrar categorías ayuda a validar la posición relativa de los elementos.";
                                }
                                
                                // B) Isolar
                                else if (cmd.Action == "isolate_category" && catIds.Any())
                                {
                                    activeView.IsolateCategoriesTemporary(catIds);
                                    result.UserMessage = $"He aislado las categorías: {string.Join(", ", catNames)}.";
                                    result.UserMessageShort = "Categorías aisladas";
                                    result.ActionDescription = "Aislamiento temporal de categorías.";
                                    result.EducationalContext = "Aislar categorías permite realizar auditorías rápidas de continuidad estructural.";
                                }

                                // C) Unhide All / Reset Overrides
                                else if (cmd.Action == "unhide_all" || cmd.Action == "reset_all_overrides")
                                {
                                    if (activeView.IsTemporaryHideIsolateActive())
                                        activeView.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
                                    
                                    if (cmd.Action == "reset_all_overrides")
                                    {
                                        var allElements = new FilteredElementCollector(doc, activeView.Id).WhereElementIsNotElementType().ToElementIds();
                                        var emptyOverride = new OverrideGraphicSettings();
                                        foreach (var eId in allElements) activeView.SetElementOverrides(eId, emptyOverride);
                                        result.UserMessage = "He reseteado todos los gráficos de la vista.";
                                        result.UserMessageShort = "Gráficos reseteados";
                                    }
                                    else
                                    {
                                        result.UserMessage = "Visibilidad temporal restablecida.";
                                        result.UserMessageShort = "Visibilidad reset";
                                    }
                                }

                                // E) Pintar / Reset Paint
                                else if (cmd.Action == "paint" || cmd.Action == "reset_paint")
                                {
                                    bool reset = cmd.Action == "reset_paint";
                                    var targetIds = new List<ElementId>();

                                    if (catIds.Any() && !cmd.UseSelection)
                                    {
                                        foreach (var cId in catIds)
                                        {
                                            targetIds.AddRange(new FilteredElementCollector(doc, activeView.Id).OfCategoryId(cId).WhereElementIsNotElementType().ToElementIds());
                                        }
                                    }
                                    else if (cmd.UseSelection)
                                    {
                                        targetIds = uiDoc.Selection.GetElementIds().ToList();
                                    }

                                    if (targetIds.Any())
                                    {
                                        if (reset)
                                        {
                                            var emptyOgs = new OverrideGraphicSettings();
                                            foreach (var id in targetIds) activeView.SetElementOverrides(id, emptyOgs);
                                            result.UserMessage = "He restaurado el color original de los elementos seleccionados/categoría.";
                                            result.UserMessageShort = "Color restaurado";
                                        }
                                        else
                                        {
                                            var c = ParseColor(cmd.Color);
                                            var solidPattern = new FilteredElementCollector(doc).OfClass(typeof(FillPatternElement)).Cast<FillPatternElement>().FirstOrDefault(f => f.GetFillPattern().IsSolidFill);
                                            var ogs = new OverrideGraphicSettings();
                                            ogs.SetSurfaceForegroundPatternColor(c);
                                            ogs.SetCutForegroundPatternColor(c);
                                            if (solidPattern != null) { ogs.SetSurfaceForegroundPatternId(solidPattern.Id); ogs.SetCutForegroundPatternId(solidPattern.Id); }
                                            foreach (var id in targetIds) activeView.SetElementOverrides(id, ogs);
                                            result.UserMessage = $"He pintado {targetIds.Count} elementos de color {cmd.Color ?? "rojo"}.";
                                            result.UserMessageShort = $"{(targetIds.Count == 1 ? "Elemento pintado" : "Elementos pintados")}";
                                        }
                                    }
                                    else if (reset)
                                    {
                                        // Reset total de la vista si no hay selección ni categoría específica
                                        var allIds = new FilteredElementCollector(doc, activeView.Id).WhereElementIsNotElementType().ToElementIds();
                                        var emptyOgs = new OverrideGraphicSettings();
                                        foreach (var id in allIds) activeView.SetElementOverrides(id, emptyOgs);
                                        result.UserMessage = "He restaurado los colores originales de TODOS los elementos en esta vista.";
                                        result.UserMessageShort = "Vista restaurada";
                                    }
                                    else result.UserMessage = "No hay elementos seleccionados para esta acción.";
                                }

                        else if (cmd.Action == "select_category" && catIds.Any())
                        {
                            var finalIds = new List<ElementId>();
                            foreach (var bCatId in catIds)
                            {
                                // Primero buscamos en la vista activa
                                var collector = new FilteredElementCollector(doc, activeView.Id)
                                    .OfCategoryId(bCatId)
                                    .WhereElementIsNotElementType();

                                var elements = collector.ToElements();

                                // Si no hay nada en la vista, buscamos en todo el documento (opcional, pero útil)
                                if (!elements.Any())
                                {
                                    elements = new FilteredElementCollector(doc)
                                        .OfCategoryId(bCatId)
                                        .WhereElementIsNotElementType()
                                        .ToElements();
                                }

                                if (cmd.MaxVolume.HasValue || cmd.MinVolume.HasValue)
                                {
                                    double m3ToFt3 = 35.3147;
                                    elements = elements.Where(e => {
                                        Parameter p = e.get_Parameter(BuiltInParameter.HOST_VOLUME_COMPUTED);
                                        if (p == null || !p.HasValue) return false;
                                        double volM3 = p.AsDouble() / m3ToFt3;
                                        if (cmd.MaxVolume.HasValue && cmd.MaxVolume.Value > 0 && volM3 > cmd.MaxVolume.Value) return false;
                                        if (cmd.MinVolume.HasValue && cmd.MinVolume.Value > 0 && volM3 < cmd.MinVolume.Value) return false;
                                        return true;
                                    }).ToList();
                                }
                                finalIds.AddRange(elements.Select(e => e.Id));
                            }

                            if (finalIds.Any())
                            {
                                uiDoc.Selection.SetElementIds(finalIds);
                                uiDoc.ShowElements(finalIds);
                                result.UserMessage = $"He seleccionado {finalIds.Count} elementos de: {string.Join(", ", catNames)}.";
                                result.UserMessageShort = $"{finalIds.Count} elementos seleccionados";
                                result.ActionDescription = "Selección de elementos por categoría.";
                                result.EducationalContext = "Seleccionar elementos masivamente te ayuda a revisar sus parámetros compartidos y asegurar la integridad de la información en el panel de Propiedades.";
                            }
                            else 
                            {
                                result.UserMessage = "No encontré elementos de esas categorías en el modelo.";
                                result.UserMessageShort = "No se encontraron elementos";
                            }
                        }

                        t.Commit();
                    }

                    if (!string.IsNullOrEmpty(cmd.Explanation))
                        result.UserMessage += "\n\n💡 Nota: " + cmd.Explanation;

                }
                catch (Exception ex)
                {
                    result.UserMessage = $"Error: {ex.Message}";
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }
                return result;
            });
        }

        private static CommandExecutionResult RunModelQualityCheck(Document doc)
        {
            var result = new CommandExecutionResult();
            var steps = new List<string>();

            // 1. Vigas sin nivel
            var beams = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralFraming).WhereElementIsNotElementType().ToElements();
            int beamsNoLevel = beams.Count(e => e.LevelId == ElementId.InvalidElementId);
            steps.Add($"{beamsNoLevel} vigas sin nivel de referencia asignado.");

            // 2. Muros sin material estructural
            var walls = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Walls).WhereElementIsNotElementType().Cast<Wall>().ToList();
            int wallsNoStructural = walls.Count(w => {
                Parameter p = w.get_Parameter(BuiltInParameter.STRUCTURAL_ANALYTICAL_MODEL);
                return p == null || p.AsInteger() == 0;
            });
            steps.Add($"{wallsNoStructural} muros marcados como no estructurales.");

            // 3. Columnas
            var columns = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralColumns).WhereElementIsNotElementType().ToList();
            steps.Add($"{columns.Count} columnas estructurales verificadas.");

            result.ActionDescription = "Revisión rápida de calidad del modelo estructural.";
            result.EducationalContext = "Mantener los niveles correctos y los materiales estructurales asignados es crítico para la coordinación y el análisis de cargas.";
            result.Steps = steps.ToArray();
            result.UserMessage = "Revisión de calidad completada. Mira los detalles en el panel.";
            result.UserMessageShort = "QA completado";
            return result;
        }

        private static CommandExecutionResult RunPrepareStructuralView(UIDocument uiDoc)
        {
            var doc = uiDoc.Document;
            var view = uiDoc.ActiveView;
            var result = new CommandExecutionResult();

            using (Transaction t = new Transaction(doc, "Preparar Vista Estructural"))
            {
                t.Start();
                // Ocultar categorías no estructurales comunes
                var toHide = new[] { BuiltInCategory.OST_Furniture, BuiltInCategory.OST_Planting, BuiltInCategory.OST_Entourage, BuiltInCategory.OST_Dimensions, BuiltInCategory.OST_TextNotes };
                foreach (var cat in toHide)
                {
                    ElementId id = new ElementId(cat);
                    if (view.CanCategoryBeHidden(id)) view.SetCategoryHidden(id, true);
                }
                t.Commit();
            }

            result.ActionDescription = "Preparación de vista para planos estructurales.";
            result.EducationalContext = "Una vista estructural limpia debe resaltar la geometría de carga y ocultar elementos arquitectónicos no esenciales como mobiliario y anotaciones genéricas.";
            result.UserMessage = "He limpiado la vista de elementos no estructurales.";
            result.UserMessageShort = "Vista preparada";
            return result;
        }

        private static BuiltInCategory? MapCategory(string categoryString)
        {
            if (string.IsNullOrEmpty(categoryString)) return null;
            return categoryString.ToLower() switch
            {
                "walls" => BuiltInCategory.OST_Walls,
                "floors" => BuiltInCategory.OST_Floors,
                "columns" => BuiltInCategory.OST_StructuralColumns,
                "beams" => BuiltInCategory.OST_StructuralFraming,
                "doors" => BuiltInCategory.OST_Doors,
                "windows" => BuiltInCategory.OST_Windows,
                _ => null
            };
        }

        private static Autodesk.Revit.DB.Color ParseColor(string colorStr)
        {
            if (string.IsNullOrEmpty(colorStr)) return new Autodesk.Revit.DB.Color(255, 0, 0); // default rojo
            switch (colorStr.ToLower())
            {
                case "red": return new Autodesk.Revit.DB.Color(255, 0, 0);
                case "blue": return new Autodesk.Revit.DB.Color(0, 0, 255);
                case "green": return new Autodesk.Revit.DB.Color(0, 255, 0);
                case "yellow": return new Autodesk.Revit.DB.Color(255, 255, 0);
                case "orange": return new Autodesk.Revit.DB.Color(255, 165, 0);
                case "cyan": return new Autodesk.Revit.DB.Color(0, 255, 255);
                case "magenta": return new Autodesk.Revit.DB.Color(255, 0, 255);
                default: return new Autodesk.Revit.DB.Color(255, 0, 0);
            }
        }
    }
}
