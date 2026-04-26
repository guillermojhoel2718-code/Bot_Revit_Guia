using System;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.UI;
using System.Text.Json;

namespace RevitTutor
{
    /// <summary>
    /// Panel WPF del Tutor IA que se muestra como DockablePane en Revit.
    /// Solo lectura — no modifica el modelo.
    /// </summary>
    public partial class TutorPane : UserControl, IDockablePaneProvider
    {
        private UIApplication? _uiApp;

        public TutorPane()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Recibe la instancia de UIApplication desde App.cs (vía ViewActivated).
        /// </summary>
        public void SetUpInitialData(UIApplication uiApp)
        {
            _uiApp = uiApp;
        }

        /// <summary>
        /// Configuración del DockablePane: posición y comportamiento inicial.
        /// </summary>
        public void SetupDockablePane(DockablePaneProviderData data)
        {
            data.FrameworkElement = this as FrameworkElement;
            data.InitialState = new DockablePaneState
            {
                DockPosition = DockPosition.Tabbed,
                TabBehind = DockablePanes.BuiltInDockablePanes.ProjectBrowser
            };
        }

        /// <summary>
        /// Al hacer clic en "Enviar": construye el contexto del modelo y lo muestra como JSON.
        /// </summary>
        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_uiApp?.ActiveUIDocument == null)
                {
                    txtLog.Text = "⚠️ No hay documento activo. Abre un proyecto primero.";
                    return;
                }

                string pregunta = txtQuestion.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(pregunta))
                {
                    txtLog.Text = "⚠️ Escribe una pregunta antes de enviar.";
                    return;
                }

                // Construir contexto del modelo (solo lectura)
                var context = ModelContextService.BuildContext(_uiApp);

                // Armar payload listo para IA
                var payload = new
                {
                    question = pregunta,
                    context = context,
                    timestamp = DateTime.Now.ToString("O")
                };

                // Serializar a JSON legible
                string json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                txtLog.Text = json;
                txtLog.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xA5, 0xB4, 0xFC) // indigo-300
                );
            }
            catch (Exception ex)
            {
                txtLog.Text = $"❌ Error: {ex.Message}";
                txtLog.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xFB, 0x71, 0x85) // rose-400
                );
            }
        }
    }
}