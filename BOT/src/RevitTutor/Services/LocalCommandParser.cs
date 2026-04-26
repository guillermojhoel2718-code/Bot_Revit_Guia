using System;
using System.Threading.Tasks;

namespace RevitTutor
{
    public class LocalCommandParser
    {
        public static LlmCommand Parse(string question)
        {
            var cmd = new LlmCommand { Action = "none", AffectAnnotations = false, AffectCadLinks = false };
            if (string.IsNullOrWhiteSpace(question)) return cmd;

            string q = question.ToLowerInvariant()
                .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u")
                .Replace("ñ", "n");

            if (q.Contains("este") || q.Contains("esta") || q.Contains("esto") || q.Contains("seleccionado") || q.Contains("actual"))
            {
                cmd.UseSelection = true;
            }

            // 1) Detectar CATEGORÍA
            if (q.Contains("muro") || q.Contains("mro") || q.Contains("wall")) cmd.Categories.Add("walls");
            if (q.Contains("suelo") || q.Contains("losa") || q.Contains("floor") || q.Contains("selo") || q.Contains("piso")) cmd.Categories.Add("floors");
            if (q.Contains("columna") || q.Contains("pilar") || q.Contains("column") || q.Contains("plar")) cmd.Categories.Add("columns");
            if (q.Contains("viga") || q.Contains("trabe") || q.Contains("vga") || q.Contains("framing")) cmd.Categories.Add("beams");
            if (q.Contains("puerta") || q.Contains("door") || q.Contains("puer")) cmd.Categories.Add("doors");
            if (q.Contains("ventana") || q.Contains("window") || q.Contains("vent")) cmd.Categories.Add("windows");
            
            if (q.Contains("anotacion") || q.Contains("eje") || q.Contains("nivel") || q.Contains("cota") || q.Contains("texto") || q.Contains("etiqueta") || q.Contains("tag"))
            {
                cmd.Categories.Add("annotation");
                cmd.AffectAnnotations = true;
            }
            if (q.Contains("cad") || q.Contains("dwg") || q.Contains("vinculo") || q.Contains("importad") || q.Contains("link"))
            {
                cmd.Categories.Add("cad_links");
                cmd.AffectCadLinks = true;
            }

            // 2) Detectar ACCIÓN y VIEW
            if (q.Contains("revis") || q.Contains("calidad") || q.Contains("checa") || q.Contains("inspecc") || q.Contains("audit"))
            {
                cmd.Action = "model_quality_check";
            }
            else if (q.Contains("preparar") || q.Contains("limpia vista") || q.Contains("vista estructural") || q.Contains("set up"))
            {
                cmd.Action = "prepare_structural_view";
            }
            else if (q.Contains("parametro") || q.Contains("proyecto") || q.Contains("info del proyecto"))
            {
                cmd.Action = "show_project_parameters";
            }
            else if (IsHowToQuestion(q))
            {
                cmd.Action = "how_to";
            }
            else if (IsResetGraphicsCommand(q))
            {
                cmd.Action = "reset_all_overrides";
            }
            else if (q.Contains("restablece") || q.Contains("quita") || q.Contains("borra") || q.Contains("limpia") || q.Contains("resetea"))
            {
                if (q.Contains("color") || q.Contains("pintura") || q.Contains("estilo") || q.Contains("grafico"))
                    cmd.Action = "reset_paint";
                else if (q.Contains("todo") || q.Contains("todas") || q.Contains("modificacion"))
                    cmd.Action = "reset_all_overrides";
                else if (cmd.AffectAnnotations)
                    cmd.Action = "show_annotations";
                else
                    cmd.Action = "show_category";
            }
            else if (q.Contains("pinta") || q.Contains("color") || q.Contains("ilumina") || q.Contains("colorea"))
            {
                cmd.Action = "paint";
                if (q.Contains("rojo") || q.Contains("roj")) cmd.Color = "red";
                else if (q.Contains("azul") || q.Contains("azl")) cmd.Color = "blue";
                else if (q.Contains("verde") || q.Contains("verd")) cmd.Color = "green";
                else if (q.Contains("amarill") || q.Contains("amrl")) cmd.Color = "yellow";
                else if (q.Contains("naranj") || q.Contains("nrnj")) cmd.Color = "orange";
                else cmd.Color = "red"; // default
            }
            else if (q.Contains("ir a") || q.Contains("ve a") || q.Contains("cambia a") || q.Contains("abre") || q.Contains("muestrame") || q.Contains("llevame") || q.Contains("navega") || q.Contains("vete") || q.Contains("vista"))
            {
                if (q.Contains("3d") || q.Contains("tres d") || q.Contains("tridimensional"))
                {
                    cmd.Action = "go_to_view_3d";
                }
                else if (q.Contains("planta") || q.Contains("nivel") || q.Contains("piso") || q.Contains("vista"))
                {
                    cmd.Action = "go_to_view_plan";
                    cmd.ViewName = ExtractViewName(q, new[] { "planta", "nivel", "piso", "vista", "las plantas", "los niveles" });
                }
                else if (q.Contains("alzado") || q.Contains("elevacion") || q.Contains("fachada") || q.Contains("seccion") || q.Contains("corte"))
                {
                    cmd.Action = "go_to_view_elevation";
                    cmd.ViewName = ExtractViewName(q, new[] { "alzado", "elevacion", "fachada", "seccion", "corte", "los alzados", "las elevaciones" });
                }
            }
            else if (q.Contains("selecc") || q.Contains("selec") || q.Contains("resalt") || q.Contains("agarra"))
            {
                cmd.Action = "select_category";
            }
            else if (q.Contains("ocult") || q.Contains("escond") || q.Contains("apaga") || q.Contains("no ver"))
            {
                if (cmd.AffectAnnotations) cmd.Action = "hide_annotations";
                else if (cmd.AffectCadLinks) cmd.Action = "hide_cad_links";
                else cmd.Action = "hide_category";
            }
            else if (q.Contains("mostr") || q.Contains("muestr") || q.Contains("ver") || q.Contains("prend") || q.Contains("visualiza"))
            {
                if (cmd.AffectAnnotations) cmd.Action = "show_annotations";
                else if (cmd.AffectCadLinks) cmd.Action = "show_cad_links";
                else cmd.Action = "show_category";
            }

            return cmd;
        }

        private static bool IsHowToQuestion(string q)
        {
            return q.Contains("como ") || q.Contains("ensename") || q.Contains("ensename") || 
                   q.Contains("explica") || q.Contains("paso a paso") || 
                   q.Contains("tutorial") || q.Contains("guia");
        }

        private static bool IsResetGraphicsCommand(string q)
        {
            return q.Contains("limpia los colores") || q.Contains("limpiar los colores") || 
                   q.Contains("restablecer colores") || q.Contains("quitar colores") || 
                   q.Contains("quitar resaltado") || q.Contains("restablecer vista") || 
                   q.Contains("limpia las modificaciones de la vista");
        }

        private static string ExtractViewName(string q, string[] keywords)
        {
            foreach (var kw in keywords)
            {
                int idx = q.IndexOf(kw);
                if (idx != -1)
                {
                    string rest = q.Substring(idx + kw.Length).Trim();
                    string[] connectors = { "de la", "del", "la", "el", "de", "en", "en la" };
                    foreach (var conn in connectors)
                    {
                        if (rest.StartsWith(conn + " "))
                        {
                            rest = rest.Substring(conn.Length + 1).Trim();
                        }
                    }

                    string[] words = rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length > 0)
                    {
                        return string.Join(" ", words);
                    }
                }
            }
            return null;
        }
    }
}
