using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Pigeon_Server
{
    public partial class ServerWindow : Window
    {
        public const string Version = "2.0";
        public readonly static string n = Environment.NewLine;

        Chat chat;
        BitmapImage Button_Background_1;
        BitmapImage Button_Background_2;

        delegate void AddLogDelegate(string sender, string text);
        delegate void ShowUsersDelegate();

        public ServerWindow()
        {
            InitializeComponent();
        }

        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UserInfo.Content = "";
            ServersInfo.Content = "";

            ServerTitle.Content = "Pigeon Сервер v." + Version;
            Button_Background_1 = LoadBitmapFromResource("Button_Background_1");
            Button_Background_2 = LoadBitmapFromResource("Button_Background_2");
            Tabs.Background = new SolidColorBrush(Colors.Transparent);
            Tabs.BorderBrush = new SolidColorBrush(Colors.Transparent);
            foreach (TabItem tab in Tabs.Items)
            {
                tab.Visibility = Visibility.Collapsed;
            }
            AddLog("SERVER", "Окно загружено.");
            chat = new Chat(this);
        }

        public void AddLog_delegate(string sender, string text)
        {
            Dispatcher.Invoke(new AddLogDelegate(AddLog), sender, text);
        }

        public void AddLog(string sender, string text)
        {
            if (Log.Text != "") Log.Text += $"{Environment.NewLine}[{sender}]: {text}";
            else Log.Text += $"[{sender}]: {text}";
            Log.ScrollToEnd();
        }

        void ServerWindow_Closing(object sender, CancelEventArgs e)
        {
            chat.Dispose();
            Environment.Exit(0);
        }

        void Window_Closed(object sender, EventArgs e)
        {
            
        }

        #region Style
        public static BitmapImage LoadBitmapFromResource(string Path)
        {
            Path = @"Resources/" + Path + ".png";
            Assembly assembly = Assembly.GetCallingAssembly();
            if (Path[0] == '/')
            {
                Path = Path.Substring(1);
            }
            return new BitmapImage(new Uri(@"pack://application:,,,/" + assembly.GetName().Name + ";component/" + Path, UriKind.Absolute));
        }

        void Home_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Tabs.SelectedIndex = 0;
        }

        void Home_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Home.Source = Button_Background_2;
        }

        void Home_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Home.Source = Button_Background_1;
        }

        void Statistics_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Tabs.SelectedIndex = 1;
        }

        void Statistics_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Statistics.Source = Button_Background_2;
        }

        void Statistics_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Statistics.Source = Button_Background_1;
        }

        void UsersTab_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Tabs.SelectedIndex = 2;
        }

        void UsersTab_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            UsersTab.Source = Button_Background_2;
        }

        void UsersTab_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            UsersTab.Source = Button_Background_1;
        }

        void ServersTab_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Tabs.SelectedIndex = 3;
        }

        void ServersTab_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ServersTab.Source = Button_Background_2;
        }

        void ServersTab_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ServersTab.Source = Button_Background_1;
        }

        void Exit_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Exit.Source = Button_Background_1;
        }

        void Exit_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Exit.Source = Button_Background_2;
        }

        void Exit_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Environment.Exit(0);       
        }

        void Reload_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Reload.Source = Button_Background_2;
        }

        void Reload_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Reload.Source = Button_Background_1;
        }

        void Reload_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Process currentProcess = Process.GetCurrentProcess();
            try
            {
                currentProcess.WaitForExit(1000);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show("RESTART ERROR! " + ex.Message);
            }
            Process.Start(currentProcess.ProcessName, "");
            Environment.Exit(0);         
        }
        #endregion

        void ShowUsers()
        {
            UsersList.Items.Clear();
            foreach (User user in Database.Users.Values) UsersList.Items.Add(user.Info.Nickname);
        }

        public void ShowUsers_async()
        {
            Dispatcher.Invoke(new ShowUsersDelegate(ShowUsers));
        }

        void ShowServers()
        {
            ServersList.Items.Clear();
            foreach (Server server in Database.Servers.Values) ServersList.Items.Add(server.Info.Title);
        }

        public void ShowServers_async()
        {
            Dispatcher.Invoke(new ShowUsersDelegate(ShowServers));
        }

        void UsersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            User user = Database.GetUser(UsersList.SelectedIndex);
            if (user != null)
            {
                UserInfo.Content = $"Ник: {user.Info.Nickname}{n}Логин: {user.Info.Login}{n}Пароль: {user.Info.Password}{n}Почта: {user.Info.Email}{n}Код восстановления: {user.Info.RecoveryCode}{n}Текущий сервер: {(user.CurrentServer == null ? "Нет" : user.CurrentServer.Info.Title)}{n}IP: {user.CommandsIpAddress}";
            }
        }

        void Send_Click(object sender, RoutedEventArgs e)
        {
            string text = Command.Text;
            string message = "";

            if (text != "")
            {
                if (text.Contains("уведомление: "))
                {
                    string msg = text.Substring(text.IndexOf(":") + 2);
                    foreach (Server server in Database.Servers.Values)
                    {
                        foreach (User user in server.Users)
                        {
                            Chat.SendCommand(ServerCommands.TextMessage, user.CommandsIpAddress, $"[SERVER]: {msg}{Environment.NewLine}");
                        }
                    }
                 
                    message = $"Уведомление отправлено ({msg})";
                }
                else if (text.Contains("create_server: "))
                {
                    string msg = text.Substring(text.IndexOf(":") + 2);
                    string[] msg_mas = msg.Split(' ');
                    try
                    {
                        Database.CreateServer(msg_mas[0], msg_mas[1], int.Parse(msg_mas[2]));
                        message = $"Сервер создан ({msg_mas[0]})";
                    }
                    catch (Exception ex)
                    {
                        message = ex.Message;
                    }

                   
                }
                else message = "Неизвестная команда.";
                AddLog("SERVER", message);
                Command.Text = "";
            }
        }

        void ServersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Server server = Database.GetServer(ServersList.SelectedIndex);
            if (server != null)
            {
                string serverUsers = n;
                for (int i = 0; i < server.Users.Count; i++)
                {
                    if (i == server.Users.Count - 1) serverUsers += server.GetUser(i).Info.Nickname;
                    else serverUsers += $"{server.GetUser(i).Info.Nickname},{n}";
                }
                ServersInfo.Content = $"Название: {server.Info.Title}{n}SID: {server.Info.SID}{n}Пароль: {server.Info.Password}{n}Максимальное кол-во пользователей: {server.Info.MaxUsersCount}{n}Пользователи на сервере: {serverUsers}";
            }
        }

        void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}
