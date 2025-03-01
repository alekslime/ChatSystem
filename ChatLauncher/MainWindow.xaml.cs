using System.Diagnostics;
using System.Windows;

namespace ChatLauncher
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void LaunchClients_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtNumber.Text, out int number))
            {
                for (int i = 0; i < number; i++)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "ChatClient.exe",
                        WorkingDirectory = @"..\..\..\ChatClient\bin\Debug\net6.0-windows"
                    });
                }
            }
        }
    }
}

