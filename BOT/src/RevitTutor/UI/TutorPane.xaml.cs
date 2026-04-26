using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Autodesk.Revit.UI;
using System.Text.Json;
using System.Windows.Media;

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

                // Auto-corrección de API Key al enviar (por si el usuario olvidó Guardar)
                string apiKey = ConfigService.LoadApiKey();
                if (!string.IsNullOrEmpty(apiKey) && apiKey.StartsWith("Alza", StringComparison.OrdinalIgnoreCase))
                {
                    apiKey = "AIza" + apiKey.Substring(4);
                    ConfigService.SaveApiKey(apiKey);
                    txtApiKey.Text = apiKey;
                }

                txtRespuesta.Text = "⏳ Pensando...";
                SetBotSpriteState("thinking");

                // Solo lanzamos el bot flotante si es una acción de "mostrar/seleccionar"
                if (IsActionQuestion(pregunta))
                {
                    MoveBotToDrawingArea();
                    SpawnFloatingBot();
                }

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
                txtRespuesta.Text = answer.Message ?? "Te llevo al destino sugerido en el modelo.";
                
                SetBotSpriteState("speaking");
                
                // Esperar un poco más para mostrar que está sentado al final
                _ = System.Threading.Tasks.Task.Delay(2000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() => 
                    {
                        SetBotSpriteState("sitting");
                        ResetBotPosition();
                    });
                });
                
                // Volver a idle después de un rato sentado
                _ = System.Threading.Tasks.Task.Delay(5000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() => SetBotSpriteState("idle"));
                });
            }
            catch (Exception ex)
            {
                txtRespuesta.Text = $"❌ Error: {ex.Message}";
                SetBotSpriteState("idle");
                ResetBotPosition();
            }
        }

        private void SetBotSpriteState(string state)
        {
            string uri;
            switch (state)
            {
                case "thinking":
                    uri = "pack://application:,,,/RevitTutor;component/Resources/sprites/brazos-cruzados.png";
                    break;
                case "walking":
                    uri = "pack://application:,,,/RevitTutor;component/Resources/sprites/lateral-izquierdo-caminata.png";
                    break;
                case "sitting":
                    uri = "pack://application:,,,/RevitTutor;component/Resources/sprites/sentado.png";
                    break;
                case "speaking":
                    uri = "pack://application:,,,/RevitTutor;component/Resources/sprites/emocionado.png";
                    break;
                case "idle":
                default:
                    uri = "pack://application:,,,/RevitTutor;component/Resources/sprites/estatico.png";
                    break;
            }

            try
            {
                var image = new System.Windows.Media.Imaging.BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(uri, UriKind.Absolute);
                image.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                image.EndInit();
                image.Freeze(); // Importante si se usa en Dispatcher o hilos cruzados

                Dispatcher.Invoke(() =>
                {
                    imgBot.Source = image;
                });
            }
            catch
            {
                // Fallback silencioso si no se encuentra la imagen en recursos
            }
        }

        private bool IsActionQuestion(string q)
        {
            if (string.IsNullOrWhiteSpace(q)) return false;
            var low = q.ToLowerInvariant();
            return low.Contains("muestr") || low.Contains("ensen") || low.Contains("enseñ") || 
                   low.Contains("selecc") || low.Contains("busc") || low.Contains("donde") || 
                   low.Contains("ubica") || low.Contains("pilar") || low.Contains("muro") || 
                   low.Contains("suelo") || low.Contains("losa");
        }

        private void SpawnFloatingBot()
        {
            try
            {
                // Intentamos obtener la posición de este panel en pantalla
                Point panelPos = this.PointToScreen(new Point(0, 0));
                
                var floatingWindow = new Window
                {
                    Width = 100,
                    Height = 100,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = System.Windows.Media.Brushes.Transparent,
                    Topmost = true,
                    ShowInTaskbar = false,
                    Left = panelPos.X,
                    Top = panelPos.Y
                };

                var botImg = new Image 
                { 
                    Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/RevitTutor;component/Resources/sprites/lateral-izquierdo.png")),
                    Width = 80,
                    Height = 80
                };
                floatingWindow.Content = botImg;

                floatingWindow.Show();

                // Animación de "caminata" (intercambio de frames)
                var walkingTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
                bool frameToggle = false;
                walkingTimer.Tick += (s, e) =>
                {
                    frameToggle = !frameToggle;
                    string sprite = frameToggle ? "lateral-izquierdo-caminata.png" : "lateral-izquierdo.png";
                    botImg.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri($"pack://application:,,,/RevitTutor;component/Resources/sprites/{sprite}"));
                };
                walkingTimer.Start();

                // Animación de desplazamiento
                var animX = new DoubleAnimation
                {
                    To = panelPos.X - 600,
                    Duration = TimeSpan.FromSeconds(3.5),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };

                // Efecto de "bobbing" (subir y bajar al caminar)
                var animY = new DoubleAnimation
                {
                    From = panelPos.Y,
                    To = panelPos.Y - 15,
                    Duration = TimeSpan.FromMilliseconds(400),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever // Bobbing infinito hasta que cierre
                };

                var animFade = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.0,
                    BeginTime = TimeSpan.FromSeconds(3.2),
                    Duration = TimeSpan.FromSeconds(0.3)
                };

                animFade.Completed += (s, e) => 
                {
                    walkingTimer.Stop();
                    floatingWindow.Close();
                };

                floatingWindow.BeginAnimation(Window.LeftProperty, animX);
                floatingWindow.BeginAnimation(Window.TopProperty, animY);
                floatingWindow.BeginAnimation(Window.OpacityProperty, animFade);
            }
            catch { /* Evitar crashes si falla la transformación de puntos */ }
        }

        private void MoveBotToDrawingArea()
        {
            // Anima BotTransform.X e Y para "acercarse" al borde que mira al área de dibujo.
            // Y bajamos la opacidad para simular que "sale" de la ventana hacia el dibujo.
            var animX = new DoubleAnimation
            {
                To = 160, // Más desplazamiento hacia el área de dibujo
                Duration = TimeSpan.FromMilliseconds(700),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            var animY = new DoubleAnimation
            {
                To = -20, // Pequeño salto hacia arriba
                Duration = TimeSpan.FromMilliseconds(700),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            var animOpacity = new DoubleAnimation
            {
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(600)
            };

            BotTransform.BeginAnimation(TranslateTransform.XProperty, animX);
            BotTransform.BeginAnimation(TranslateTransform.YProperty, animY);
            imgBot.BeginAnimation(UIElement.OpacityProperty, animOpacity);
        }

        private void ResetBotPosition()
        {
            // Regresa apareciendo en su sitio
            var animX = new DoubleAnimation { To = 0, Duration = TimeSpan.FromMilliseconds(0) };
            var animY = new DoubleAnimation { To = 0, Duration = TimeSpan.FromMilliseconds(0) };
            var animOpacity = new DoubleAnimation { To = 1.0, Duration = TimeSpan.FromMilliseconds(400) };

            BotTransform.BeginAnimation(TranslateTransform.XProperty, animX);
            BotTransform.BeginAnimation(TranslateTransform.YProperty, animY);
            imgBot.BeginAnimation(UIElement.OpacityProperty, animOpacity);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            string apiKey = ConfigService.LoadApiKey();
            if (!string.IsNullOrEmpty(apiKey))
            {
                txtApiKey.Text = apiKey;
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

        private void OnApiKeyLinkClick(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void OnSaveApiKeyClick(object sender, RoutedEventArgs e)
        {
            string apiKey = txtApiKey.Text?.Trim() ?? string.Empty;
            
            // Auto-corrección de "Alza" por "AIza" (i mayúscula de IA)
            if (apiKey.StartsWith("Alza", StringComparison.OrdinalIgnoreCase))
            {
                apiKey = "AIza" + apiKey.Substring(4);
                txtApiKey.Text = apiKey;
                MessageBox.Show("He corregido automáticamente el inicio de tu clave de 'Alza' a 'AIza' (con I mayúscula) para que funcione con Google.", "Corrección Automática", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            ConfigService.SaveApiKey(apiKey); // usa tu servicio existente
            MessageBox.Show("API key guardada. Ahora el tutor usará IA de Gemini cuando sea posible.", "RevitTutor", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}