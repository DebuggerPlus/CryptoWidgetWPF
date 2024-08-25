using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WebSocketSharp;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Windows.Threading;

namespace CryptoWidgetWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private WebSocket _webSocket;
        private readonly string logFilePath = "WebSocketErrors.log"; // Путь к файлу логов

        public MainWindow()
        {
            InitializeComponent();

            StartWebSocket();

            // Установить начальное значение времени
            dateTimeTextBlock.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm");

            // Запуск таймера для обновления времени каждую секунду
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (sender, args) =>
            {
                dateTimeTextBlock.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            };
            timer.Start();
        }

        private void StartWebSocket()
        {
            _webSocket = new WebSocket("wss://stream.binance.com:9443/ws/!ticker@arr");
            _webSocket.OnMessage += (sender, e) =>
            {
                try
                {
                    var json = JArray.Parse(e.Data);

                    foreach (var ticker in json)
                    {
                        string symbol = ticker["s"].ToString();

                        // Обрабатываем только определенные символы
                        if (symbol == "BTCUSDT" || symbol == "ETHUSDT" || symbol == "LTCUSDT")
                        {
                            if (Double.TryParse(ticker["c"].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double price) &&
                                Double.TryParse(ticker["P"].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double priceChangePercent))
                            {
                                // Форматирование цены и процентов
                                string formattedPrice = price.ToString("#,##0.00", CultureInfo.InvariantCulture);
                                string formattedPercent = priceChangePercent.ToString("0.00", CultureInfo.InvariantCulture);

                                Dispatcher.Invoke(() =>
                                {
                                    switch (symbol)
                                    {
                                        case "BTCUSDT":
                                            btcPrice.Text = $"{formattedPrice}";
                                            btcChange.Text = $"{formattedPercent}%";
                                            btcChange.Foreground = priceChangePercent >= 0 ? Brushes.LimeGreen : Brushes.Red;
                                            break;

                                        case "ETHUSDT":
                                            ethPrice.Text = $"{formattedPrice}";
                                            ethChange.Text = $"{formattedPercent}%";
                                            ethChange.Foreground = priceChangePercent >= 0 ? Brushes.LimeGreen : Brushes.Red;
                                            break;

                                        case "LTCUSDT":
                                            ltcPrice.Text = $"{formattedPrice}";
                                            ltcChange.Text = $"{formattedPercent}%";
                                            ltcChange.Foreground = priceChangePercent >= 0 ? Brushes.LimeGreen : Brushes.Red;
                                            break;
                                    }
                                });
                            }
                            else
                            {
                                LogError($"Failed to parse price or priceChangePercent for symbol: {symbol}. Data received: {ticker.ToString()}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError("Exception during OnMessage: " + ex.ToString());
                }
            };

            _webSocket.OnError += (sender, e) =>
            {
                LogError("WebSocket Error: " + e.Message);
            };

            _webSocket.OnClose += (sender, e) =>
            {
                LogError("WebSocket Closed: " + e.Reason);
                ReconnectWebSocket();
            };

            _webSocket.Connect();
        }

        private void ReconnectWebSocket()
        {
            if (_webSocket != null)
            {
                _webSocket.Close();
                _webSocket = null;
            }

            StartWebSocket();
        }

        private void LogError(string message)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine($"{DateTime.Now}: {message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to log error: " + ex.Message);
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximizeRestore();
            }
            else
            {
                DragMove();
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeRestore_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximizeRestore();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ToggleMaximizeRestore()
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
        }
    }
}
