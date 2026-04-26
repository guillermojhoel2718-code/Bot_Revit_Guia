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

        public static RevitTaskHandler TaskHandler { get; private set; }
        public static ExternalEvent RevitEvent { get; private set; }

        private static TutorPane? _tutorPane;

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                TaskHandler = new RevitTaskHandler();
                RevitEvent = ExternalEvent.Create(TaskHandler);

                // Crear la instancia del panel WPF
                _tutorPane = new TutorPane();

                // Registrar como DockablePane en Revit
                application.RegisterDockablePane(
                    PaneId,
                    "Tutor IA",
                    _tutorPane
                );

                // Crear Ribbon Tab y Panel
                string tabName = "Tutor IA";
                try
                {
                    application.CreateRibbonTab(tabName);
                }
                catch { /* The tab may already exist */ }

                RibbonPanel panel = application.CreateRibbonPanel(tabName, "Panel Tutor");

                // Configurar el botón para mostrar el tutor
                PushButtonData showTutorButtonData = new PushButtonData(
                    "ShowTutorCommand",
                    "Abrir Tutor",
                    System.Reflection.Assembly.GetExecutingAssembly().Location,
                    "RevitTutor.ShowTutorCommand");

                showTutorButtonData.ToolTip = "Abre el panel del Tutor IA para hacer preguntas sobre el modelo.";
                panel.AddItem(showTutorButtonData);

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

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.ReadOnly)]
    public class ShowTutorCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, Autodesk.Revit.DB.ElementSet elements)
        {
            try
            {
                DockablePane pane = commandData.Application.GetDockablePane(App.PaneId);
                if (pane != null)
                {
                    pane.Show();
                }
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
