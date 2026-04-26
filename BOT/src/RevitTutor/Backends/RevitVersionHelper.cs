using System;
using Autodesk.Revit.UI;

namespace RevitTutor
{
    /// <summary>
    /// Utilidad para detectar la versión de Revit y decidir qué backend instanciar.
    /// </summary>
    public static class RevitVersionHelper
    {
        public static IRevitTutorBackend CreateBackend(UIApplication uiApp)
        {
            // Obtener versión de Revit
            string versionStr = uiApp.Application.VersionNumber;
            
            if (int.TryParse(versionStr, out int version) && version >= 2027)
            {
                // Revit 2027 o superior: usar MCP
                return new Revit2027McpBackend();
            }
            else
            {
                // Revit 2026 o inferior: usar Legacy (BYOK)
                return new LegacyBackend(uiApp);
            }
        }
    }
}
