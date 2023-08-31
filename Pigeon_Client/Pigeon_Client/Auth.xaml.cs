using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static Pigeon_Client.Library;

namespace Pigeon_Client
{
    public partial class Auth : Window
    {
        bool IsLoggining = true;
        bool IsFullscreen = false;
        bool WillSave = false;

        ///<summary> Конструктор </summary>
        public Auth()
        {                           
            InitializeComponent();         
        }

        ///<summary> Когда загрузка формы завершилась </summary>
        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists("user.data"))
            {
                SaveLoginPassword.IsChecked = true;
                WillSave = true;
                string data = AES.DecodeString(File.ReadAllText("user.data"));
                string login = data.Substring(0, data.IndexOf("|"));
                string password = data.Substring(data.IndexOf("|") + 1);
                Login_Box.Text = login;
                Password_Box.Password = password;
            }
        }

        ///<summary> Загрузка формы с серверами </summary>
        void Success()
        {         
            Hide();
            new Main().Show();
        }

        ///<summary> Попытка авторизации </summary>
        void Login(string login, string password)
        {
            try
            {
                bool result = Chat.LogIn(login, password);
                if (result) Success();
                else Error("Не удалось авторизироваться!", "Проверьте правильность логина/пароля или подключение к интернету!", AlertType.Notification);
            }
            catch (Exception e)
            {
                Error("Ошибка авторизации!", e.Message);
            }
        }

        #region Стиль
        void OnWindowClosing(object sender, CancelEventArgs e)
        {
            Environment.Exit(0);
        }

        void Close_Button_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        void FullScreen_Button_Click(object sender, RoutedEventArgs e)
        {
            if (!IsFullscreen)
            {
                WindowState = WindowState.Maximized;
                IsFullscreen = true;
            }
            else
            {
                WindowState = WindowState.Normal;
                IsFullscreen = false;
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

        void Login_Got_Focus(object sender, RoutedEventArgs e)
        {
            Login_Box.BorderThickness = new Thickness(0, 0, 0, 1.5);
            Login_Box.BorderBrush = Brushes.White;
        }

        void Password_Got_Focus(object sender, RoutedEventArgs e)
        {
            Password_Box.BorderThickness = new Thickness(0, 0, 0, 1.5);
            Password_Box.BorderBrush = Brushes.White;
        }

        void Password_Lost_Focus(object sender, RoutedEventArgs e)
        {
            Password_Box.BorderThickness = new Thickness(0, 0, 0, 1);
            Password_Box.BorderBrush = Brushes.Gray;
        }

        void Nickname_Got_Focus(object sender, RoutedEventArgs e)
        {
            Nickname_Box.BorderThickness = new Thickness(0, 0, 0, 1.5);
            Nickname_Box.BorderBrush = Brushes.White;
        }

        void Nickname_Lost_Focus(object sender, RoutedEventArgs e)
        {
            Nickname_Box.BorderThickness = new Thickness(0, 0, 0, 1);
            Nickname_Box.BorderBrush = Brushes.Gray;
        }

        void Login_Lost_Focus(object sender, RoutedEventArgs e)
        {
            Login_Box.BorderThickness = new Thickness(0, 0, 0, 1);
            Login_Box.BorderBrush = Brushes.Gray;
        }

        void Auth_Background_Click(object sender, RoutedEventArgs e)
        {
            Login_Box.BorderThickness = new Thickness(0, 0, 0, 1);
            Login_Box.BorderBrush = Brushes.Gray;
            Password_Box.BorderThickness = new Thickness(0, 0, 0, 1);
            Password_Box.BorderBrush = Brushes.Gray;
            Nickname_Box.BorderThickness = new Thickness(0, 0, 0, 1);
            Nickname_Box.BorderBrush = Brushes.Gray;
        }

        void Move_Panel_Click(object sender, RoutedEventArgs e)
        {
            Login_Box.BorderThickness = new Thickness(0, 0, 0, 1);
            Login_Box.BorderBrush = Brushes.Gray;
            Password_Box.BorderThickness = new Thickness(0, 0, 0, 1);
            Password_Box.BorderBrush = Brushes.Gray;
            Nickname_Box.BorderThickness = new Thickness(0, 0, 0, 1);
            Nickname_Box.BorderBrush = Brushes.Gray;
        }

        void Login_Box_Key_Down(object sender, KeyEventArgs e)
        {
            if (IsLoggining) if (e.Key == Key.Enter) Password_Box.Focus();
            else if(e.Key == Key.Enter) Nickname_Box.Focus();
        }

        void Password_Box_Key_Down(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) Login_Button_Click(sender, e);
        }

        void Nickname_Box_Key_Down(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) Password_Box.Focus();
        }

        void Register_Text_Box_Click(object sender, RoutedEventArgs e)
        {
            if (IsLoggining == true)
            {
                Reg_Label.Content = "Уже зарегистрированы?";
                Register_Text_Box.Content = "Войти";
                IsLoggining = false;
                Login_Button.Content = "Зарегистрироваться";
                Height = 420;
                Login_Stack_Panel.Height = 420;
                Login_Box_Stack_Panel.Orientation = Orientation.Vertical;
                Nickname_Box.Opacity = 1;
                Nickname_Label.Opacity = 1;
                SaveLoginPassword.Opacity = 0;
            }
            else
            {
                Reg_Label.Content = "Еще не зарегистрированы?";
                Register_Text_Box.Content = "Зарегистрироваться";
                IsLoggining = true;
                Login_Button.Content = "Войти";
                Height = 350;
                Login_Stack_Panel.Height = 350;
                Login_Box_Stack_Panel.Orientation = Orientation.Horizontal;
                Nickname_Box.Opacity = 0;
                Nickname_Label.Opacity = 0;
                SaveLoginPassword.Opacity = 1;
            }
        }

        void _MouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        void Mouse_Enter_Register_Button(object sender, RoutedEventArgs e)
        {
            Register_Text_Box.BorderThickness = new Thickness(0, 0, 0, 1);
        }

        void Mouse_Leave_Register_Button(object sender, RoutedEventArgs e)
        {
            Register_Text_Box.BorderThickness = new Thickness(0, 0, 0, 0);
        }

        ///<summary> Запоминать ли пароль? </summary>
        void SaveLoginPassword_Checked(object sender, RoutedEventArgs e)
        {
            WillSave = SaveLoginPassword.IsChecked.Value;
        }

        ///<summary> Запоминать ли пароль? </summary>
        private void SaveLoginPassword_Unchecked(object sender, RoutedEventArgs e)
        {
            WillSave = SaveLoginPassword.IsChecked.Value;
        }

        ///<summary> Загрузка формы с серверами </summary>
        void Login_Button_Click(object sender, RoutedEventArgs e)
        {
            string login = Login_Box.Text;
            string password = Password_Box.Password;
            string nickname = Nickname_Box.Text;
            string email = "nomail@nomail.nomail";

            if (IsLoggining)
            {
                if (WillSave)
                {
                    using (FileStream fs = new FileStream("user.data", FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        byte[] data = Encoding.UTF8.GetBytes(AES.EncodeString(login + "|" + password));
                        fs.Write(data, 0, data.Length);
                        fs.Close();
                    }
                }
                else
                {
                    if (File.Exists("user.data")) File.Delete("user.data");
                }

                if (login != "" && password != "") Login(login, password);
            }
            else
            {
                if (nickname.Length > 2)
                {
                    if (login.Length > 2)
                    {
                        if (password.Length >= 6)
                        {
                            if (email.Contains("@"))
                            {
                                bool result = Chat.Register(login, password, Nickname_Box.Text, email);
                                if (result) Error("Вы зарегистрированы!", "Ваша регистрация прошла успешно.", AlertType.Notification);
                                else Error("Вы не зарегистрированы!", "Ваша регистрация провалилась.", AlertType.Notification);
                            }
                            else Error("Не удалось зарегистрироваться!", "Неверный e-mail адрес.", AlertType.Notification);
                        }
                        else Error("Не удалось зарегистрироваться!", "Пароль должен быть длиннее или быть равным 6 символам.", AlertType.Notification);
                    }
                    else Error("Не удалось зарегистрироваться!", "Логин должен быть длиннее или быть равным 2 символам.", AlertType.Notification);
                }
                else Error("Не удалось зарегистрироваться!", "Псевдоним должен быть длиннее или быть равным 2 символам.", AlertType.Notification);
            }
        }
        #endregion
    }
}