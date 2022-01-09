using System;
using System.Diagnostics;
using System.Windows;

namespace ShareScreen
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        private static Process _process;
        private static StreamingServer _streamingServer;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void StartServer()
        {
            StopServer();

            _process = new Process
            {
                StartInfo =
                {
                    FileName = "cmd.exe",
                    Arguments = "/K ngrok http " + TbPort.Text,
                }
            };

            _process.Start();

            _streamingServer = new StreamingServer();
            _streamingServer.Start(Convert.ToInt32(TbPort.Text));

            BStart.Visibility = Visibility.Collapsed;
            BStop.Visibility = Visibility.Visible;
        }

        private void StopServer()
        {
            foreach (var p in Process.GetProcessesByName("ngrok"))
            {
                p.Kill();
            }

            BStop.Visibility = Visibility.Collapsed;
            BStart.Visibility = Visibility.Visible;
        }

        private void BStart_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TbPort.Text))
            {
                MessageBox.Show("Informe a porta");
                return;
            }

            StartServer();
        }

        private void BStop_OnClick(object sender, RoutedEventArgs e)
        {
            StopServer();
        }

        private void MainWindow_OnClosed(object? sender, EventArgs e)
        {
            _streamingServer?.Stop();

            _process?.Refresh();
            _process?.Kill();

            StopServer();
        }
    }
}
