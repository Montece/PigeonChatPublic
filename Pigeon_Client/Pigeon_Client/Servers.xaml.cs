using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Pigeon_Client
{
    public partial class Servers : Window
    {
        public delegate void InvokeDelegate(string text, int j);

        public Servers()
        {
            InitializeComponent();
            new Thread(GetServers).Start();      
        }

        public void GetServers()
        {
            Chat.GetServers();
            string result = Chat.Receive();
            if (result != "")
            {
                string Server = "";
                int j = 1;
                for (int i = 0; i < result.Length; i++)
                {
                    if (result[i] != Chat.CommandChar) Server += result[i];
                    else
                    {
                        Dispatcher.BeginInvoke(new InvokeDelegate(AddServer), Server, j);
                        j++;
                        Server = "";
                    }
                }
            }
        }

        void AddServer(string text, int j)
        {
            listBox.Items.Add("Сервер №" + j + ": " + text);
        }

        void OnWindowClosing(object sender, CancelEventArgs e)
        {
            Chat.Stop();
        }

        void listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listBox.SelectedIndex != -1)
            {
                string a = listBox.SelectedItem.ToString();
                listBox.SelectedIndex = -1;
                a = a.Substring(a.IndexOf(':') + 2);
                Chat.Server = a;
                Chat.ConnectToServer();
                string Result = Chat.Receive();
                if (Result == Chat.CommandChar + "connect" + Chat.CommandChar + "true")
                {
                    Success();
                }
                else MessageBox.Show("ERROR");
            }           
        }

        void Success()
        {         
            Main MainForm = new Main();
            // Hide();
            MainForm.Show();         
        }

        #region Style

        bool FullScreen_Button_True = false;

        void Close_Button_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        void FullScreen_Button_Click(object sender, RoutedEventArgs e)
        {
            if (FullScreen_Button_True == false)
            {
                WindowState = WindowState.Maximized;
                FullScreen_Button_True = true;
            }
            else
            {
                WindowState = WindowState.Normal;
                FullScreen_Button_True = false;
            }
        }

        void Cut_Down_Button_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        public BitmapImage LoadBitmapFromResource(string Path)
        {
            Path = @"Images/" + Path;
            Assembly assembly = Assembly.GetCallingAssembly();
            if (Path[0] == '/')
            {
                Path = Path.Substring(1);
            }
            return new BitmapImage(new Uri(@"pack://application:,,,/" + assembly.GetName().Name + ";component/" + Path, UriKind.Absolute));
        }

        void Mouse_Enter_Close_Button(object sender, RoutedEventArgs e)
        {
            Close_Button_Image1.Source = LoadBitmapFromResource("Close_Button2.png");
        }

        void Mouse_Leave_Close_Button(object sender, RoutedEventArgs e)
        {
            Close_Button_Image1.Source = LoadBitmapFromResource("Close_Button1.png");
        }

        void Mouse_Enter_FullScreen_Button(object sender, RoutedEventArgs e)
        {
            FullScreen_Button_Image1.Source = LoadBitmapFromResource("Fullscreen_Button2.png");
        }

        void Mouse_Leave_FullScreen_Button(object sender, RoutedEventArgs e)
        {
            FullScreen_Button_Image1.Source = LoadBitmapFromResource("FullScreen_Button1.png");
        }

        void Mouse_Enter_Cut_Down_Button(object sender, RoutedEventArgs e)
        {
            Cut_Down_Button_Image1.Source = LoadBitmapFromResource("Cut_Down_Button2.png");
        }

        void Mouse_Leave_Cut_Down_Button(object sender, RoutedEventArgs e)
        {
            Cut_Down_Button_Image1.Source = LoadBitmapFromResource("Cut_Down_Button1.png");
        }
        #endregion
    }
}
