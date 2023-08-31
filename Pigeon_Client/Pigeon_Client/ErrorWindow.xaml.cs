using System.Windows;
using static Pigeon_Client.Library;

namespace Pigeon_Client
{
    public partial class ErrorWindow : Window
    {
        public ErrorWindow(string title, string description, AlertType type)
        {
            InitializeComponent();
            ErrorTitle.Content = title;

            switch(type)
            {
                case AlertType.Error: ErrorDescription.Content = "Причина: " + description; break;
                case AlertType.Notification: ErrorDescription.Content = "Описание: " + description; break;
            }
        }

        void OK_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
