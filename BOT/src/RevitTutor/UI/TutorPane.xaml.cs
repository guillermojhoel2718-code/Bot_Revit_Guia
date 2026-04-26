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
            txtApiKey.Password = ConfigService.LoadApiKey();
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
        /// Al hacer clic en "Enviar": obtiene contexto vía backend y simula respuesta.
        /// </summary>
        private async void btnSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_uiApp?.ActiveUIDocument == null)
                {
                    txtRespuesta.Text = "⚠️ No hay documento activo. Abre un proyecto primero.";
                    return;
                }

                string pregunta = txtQuestion.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(pregunta) || pregunta == "muéstrame los muros")
                {
                    txtRespuesta.Text = "⚠️ Por favor, escribe una pregunta válida antes de enviar.";
                    return;
                }

                txtRespuesta.Text = "⏳ Pensando...";

                // Obtener el backend adecuado según la versión
                IRevitTutorBackend backend = RevitVersionHelper.CreateBackend(_uiApp);

                // Obtener contexto
                var context = await backend.GetModelContextAsync();

                // Preguntar a la IA
                var answer = await backend.AskQuestionAsync(pregunta, context);

                // Ejecutar navegación si la IA sugiere un destino
                if (answer.SuggestedDestination != null)
                {
                    await backend.NavigateToDestinationAsync(answer.SuggestedDestination);
                }

                // Mostrar respuesta en lenguaje natural
                txtRespuesta.Text = !string.IsNullOrEmpty(answer.Text) 
                    ? answer.Text 
                    : "Te llevo al destino sugerido en el modelo.";
            }
            catch (Exception ex)
            {
                txtRespuesta.Text = $"❌ Error: {ex.Message}";
            }
        }

        private void txtQuestion_GotFocus(object sender, RoutedEventArgs e)
        {
            // Limpia el texto de placeholder cuando el usuario hace clic en el TextBox
            if (txtQuestion.Text == "muéstrame los muros")
            {
                txtQuestion.Text = string.Empty;
            }
        }

        private void btnSaveApiKey_Click(object sender, RoutedEventArgs e)
        {
            string key = txtApiKey.Password;
            ConfigService.SaveApiKey(key);
            txtRespuesta.Text = string.IsNullOrEmpty(key) 
                ? "API key eliminada." 
                : "API key guardada correctamente.";
        }
    }
}