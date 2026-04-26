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
                var config = ConfigService.LoadConfig();
                string apiKey = config.GeminiApiKey;
                if (!string.IsNullOrEmpty(apiKey) && apiKey.StartsWith("Alza", StringComparison.OrdinalIgnoreCase))
                {
                    apiKey = "AIza" + apiKey.Substring(4);
                    ConfigService.SaveApiKey(apiKey);
                    txtApiKey.Password = apiKey;
                }

                txtRespuesta.Text = "⏳ Pensando...";
                UpdateBotComment("Pensando...");
                SetBotSpriteState("thinking");

                // Obtener el backend adecuado según la versión
                IRevitTutorBackend backend = RevitVersionHelper.CreateBackend(_uiApp);

                // Obtener contexto
                var context = await backend.GetModelContextAsync();

                // Preguntar a la IA
                var answer = await backend.AskQuestionAsync(pregunta, context);

                // Solo lanzamos el bot flotante si es una acción
                string shortMessage = answer.Message?.Split('\n')[0] ?? "Listo.";
                if (IsActionQuestion(pregunta) || shortMessage.Contains("He "))
                {
                    MoveBotToDrawingArea();
                    SpawnFloatingBot(shortMessage);
                }

                // Mostrar respuesta en lenguaje natural
                txtRespuesta.Text = answer.Message ?? "Listo, he procesado tu solicitud.";
                UpdateBotComment("¡Listo!");
                txtBotSummary.Text = answer.Summary ?? "";
                
                SetBotSpriteState("speaking");
                
                // Volver a idle
                _ = System.Threading.Tasks.Task.Delay(3000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() => 
                    {
                        UpdateBotComment("En espera");
                        txtBotSummary.Text = "";
                        ResetBotPosition();
                        SetBotSpriteState("idle");
                    });
                });
                
                _ = System.Threading.Tasks.Task.Delay(3000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() => SetBotSpriteState("idle"));
                });
            }
            catch (Exception ex)
            {
                txtRespuesta.Text = $"❌ Error: {ex.Message}";
                UpdateBotComment("Error");
                SetBotSpriteState("idle");
                ResetBotPosition();
            }
        }

        private void UpdateBotComment(string text)
        {
            Dispatcher.Invoke(() =>
            {
                if (txtBotStatus != null)
                {
                    txtBotStatus.Text = text;
                }
            });
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
            return low.Contains("muestr") || low.Contains("mostr") || low.Contains("ensen") || low.Contains("enseñ") || 
                   low.Contains("selecc") || low.Contains("busc") || low.Contains("donde") || 
                   low.Contains("ubica") || low.Contains("pilar") || low.Contains("muro") || 
                   low.Contains("suelo") || low.Contains("losa") || low.Contains("prend") || low.Contains("ocult");
        }

        private void SpawnFloatingBot(string message)
        {
            try
            {
                // Calcular la posición actual del bot en la pantalla para una transición suave
                Point botScreenPos = imgBot.PointToScreen(new Point(0, 0));
                
                var floatingWindow = new Window
                {
                    Width = 300,
                    Height = 150,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = System.Windows.Media.Brushes.Transparent,
                    Topmost = true,
                    ShowInTaskbar = false,
                    Left = botScreenPos.X,
                    Top = botScreenPos.Y
                };

                var container = new Grid();
                container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var botImg = new Image 
                { 
                    Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/RevitTutor;component/Resources/sprites/lateral-izquierdo.png")),
                    Width = 80,
                    Height = 80,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(botImg, 0);
                container.Children.Add(botImg);

                var textBorder = new Border
                {
                    Background = System.Windows.Media.Brushes.White,
                    BorderBrush = new System.Windows.Media.SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(8),
                    Margin = new Thickness(8,0,0,0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Visibility = Visibility.Hidden
                };

                var textBlock = new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 12,
                    Foreground = System.Windows.Media.Brushes.Black
                };
                textBorder.Child = textBlock;
                Grid.SetColumn(textBorder, 1);
                container.Children.Add(textBorder);

                floatingWindow.Content = container;
                floatingWindow.Show();

                // Animación de "caminata"
                var walkingTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
                bool frameToggle = false;
                walkingTimer.Tick += (s, e) =>
                {
                    frameToggle = !frameToggle;
                    string sprite = frameToggle ? "lateral-izquierdo-caminata.png" : "lateral-izquierdo.png";
                    botImg.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri($"pack://application:,,,/RevitTutor;component/Resources/sprites/{sprite}"));
                };
                walkingTimer.Start();

                // Destino: un poco a la izquierda del centro de la pantalla para que el globo de texto quede centrado
                double targetX = SystemParameters.PrimaryScreenWidth / 2 - 150;
                double targetY = SystemParameters.PrimaryScreenHeight / 2 - 50;

                // Animación de desplazamiento (X)
                var animX = new DoubleAnimation
                {
                    To = targetX,
                    Duration = TimeSpan.FromSeconds(1.2),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };

                // Bobbing (Y) - simular flotación
                var animY = new DoubleAnimation
                {
                    From = botScreenPos.Y,
                    To = botScreenPos.Y - 20,
                    Duration = TimeSpan.FromMilliseconds(300),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                };

                animX.Completed += (s, e) =>
                {
                    // Al detenerse, cambiar sprite a hablando y mostrar el mensaje
                    walkingTimer.Stop();
                    animY.RepeatBehavior = new RepeatBehavior(1); // Detener el salto
                    botImg.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/RevitTutor;component/Resources/sprites/emocionado.png"));
                    textBorder.Visibility = Visibility.Visible;

                    // Cerrar después de 4 segundos
                    var closeTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
                    closeTimer.Tick += (sender, args) =>
                    {
                        closeTimer.Stop();
                        
                        var animFade = new DoubleAnimation { To = 0.0, Duration = TimeSpan.FromSeconds(0.3) };
                        animFade.Completed += (s2, e2) => floatingWindow.Close();
                        floatingWindow.BeginAnimation(Window.OpacityProperty, animFade);
                    };
                    closeTimer.Start();
                };

                floatingWindow.BeginAnimation(Window.LeftProperty, animX);
                floatingWindow.BeginAnimation(Window.TopProperty, animY);
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
            var config = ConfigService.LoadConfig();
            if (!string.IsNullOrEmpty(config.GeminiApiKey))
            {
                txtApiKey.Password = config.GeminiApiKey;
            }

            if (config.ShowStartupHelp)
            {
                ShowStartupHelp();
            }
        }

        private void ShowStartupHelp()
        {
            txtRespuesta.Text = "👋 ¡Hola! Soy RevitTutor. Puedo ayudarte con:\n\n" +
                               "• Navegación: 've a la planta Nivel 1', 've al alzado Norte', 've a la vista 3D principal'.\n" +
                               "• Visibilidad: 'oculta las anotaciones en esta vista', 'muestra solo las vigas'.\n" +
                               "• Edición visual: 'limpia los colores'.\n" +
                               "• Educación: 'enséñame a modelar una losa', 'explícame cómo dibujar una trabe'.\n" +
                               "• Revisión de calidad: 'revisa la calidad del modelo estructural'.\n\n" +
                               "Puedes clickear en 'Ver comandos sugeridos' para ver más opciones.";
            UpdateBotComment("¡Listo para ayudarte!");
            SetBotSpriteState("idle");
        }

        private void OnCommandSelected(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is ListBoxItem item)
            {
                txtQuestion.Text = item.Content?.ToString();
                // Deseleccionamos para que se pueda volver a clickear el mismo
                listBox.SelectedIndex = -1;
            }
        }

        private void txtQuestion_GotFocus(object sender, RoutedEventArgs e)
        {
            // Limpia el texto de placeholder cuando el usuario hace clic en el TextBox
            if (txtQuestion.Text == "selecciona los muros" || txtQuestion.Text == "muéstrame los muros")
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
            string apiKey = txtApiKey.Password?.Trim() ?? string.Empty;
            
            // Auto-corrección de "Alza" por "AIza" (i mayúscula de IA)
            if (apiKey.StartsWith("Alza", StringComparison.OrdinalIgnoreCase))
            {
                apiKey = "AIza" + apiKey.Substring(4);
                txtApiKey.Password = apiKey;
                MessageBox.Show("He corregido automáticamente el inicio de tu clave de 'Alza' a 'AIza' (con I mayúscula) para que funcione con Google.", "Corrección Automática", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            ConfigService.SaveApiKey(apiKey);
            MessageBox.Show("Configuración guardada correctamente. El tutor ya puede usar IA avanzada.", "RevitTutor", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}