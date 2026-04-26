using System;
using Autodesk.Revit.UI;

namespace RevitTutor
{
    /// <summary>
    /// Punto de entrada del plugin. Registra el panel Tutor IA como DockablePane.
    /// Solo lectura — no modifica el modelo.
    /// </summary>
    public class App : IExternalApplication
    {
        // GUID único para el DockablePane del tutor
        public static readonly DockablePaneId PaneId =
            new DockablePaneId(new Guid("B1C2D3E4-F5A6-7890-1234-567890ABCDEF"));

        private static TutorPane? _tutorPane;

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Crear la instancia del panel WPF
                _tutorPane = new TutorPane();

                // Registrar como DockablePane en Revit
                application.RegisterDockablePane(
                    PaneId,
                    "Tutor IA",
                    _tutorPane
                );

                // Suscribirse al evento de documento abierto para pasar UIApplication
                application.ControlledApplication.DocumentOpened += (sender, args) =>
                {
                    // Se obtiene UIApplication a través del evento ViewActivated
                };

                // Suscribirse para pasar UIApplication cuando se activa una vista
                application.ViewActivated += (sender, args) =>
                {
                    var uiApp = sender as UIApplication;
                    if (uiApp != null)
                    {
                        _tutorPane?.SetUpInitialData(uiApp);
                    }
                };

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Error al iniciar Tutor IA: {ex.Message}",
                    "RevitTutorIA",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            _tutorPane = null;
            return Result.Succeeded;
        }
    }
}
