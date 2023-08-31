using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace Pigeon_Client
{
    public partial class Init : Window
    {
        public delegate void InvokeDelegate(string text);
        public delegate void Loading();
        Loading LoadForm;
        Thread AnimationThread;

        ///<summary> Инициализация </summary>
        public Init()
        {
            InitializeComponent();
            Config.Load();
            Chat.SetIP(Config.CurrentConfig.ServerIP);
        }

        ///<summary> Начало </summary>
        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadForm += Success;

            try
            {
                new Thread(Check).Start();
                AnimationThread = new Thread(Animation);
                AnimationThread.Start();
            }
            catch (Exception x)
            {
                Library.Error("Ошибка инициализации!", x.Message);
                Environment.Exit(0);
            }
        }

        ///<summary> Анимация точек </summary>
        public void Animation()
        {
            while (true)
            {
                SetText_async("Загрузка.");
                Thread.Sleep(500);
                SetText_async("Загрузка..");
                Thread.Sleep(500);
                SetText_async("Загрузка...");
                Thread.Sleep(500);
            }
        }

        ///<summary> Присвоить текст статусу </summary>
        void SetText(string text)
        {
            this.text.Content = text;
        }

        ///<summary> Присвоить текст статусу  </summary>
        void SetText_async(string text)
        {
            Dispatcher.BeginInvoke(new InvokeDelegate(SetText), text);
        }

        ///<summary> Проверка обновлений </summary>
        void Check()
        {
            string Result = Chat.GetServerVersion();
            if (Result == "ERROR")
            {
                AnimationThread.Abort();             
                SetText_async("Чат недоступен");
            }
            else
            {
                if (Result == Library.Version) Dispatcher.Invoke(LoadForm);
                else Dispatcher.Invoke(new Library.Error_asyncDelegate(Library.Error), "Ошибка обновления", "Функция обновления временно недоступна.", Library.AlertType.Notification);
            }
        }

        ///<summary> Загрузка окна авторизации </summary>
        void Success()
        {
            new Auth().Show();
            Hide();
        }
        
        ///<summary> При закрытии 2 </summary>
        void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        ///<summary> Drag окна </summary>
        void _MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                DragMove();
            }
            catch
            {

            }
        }    
    }
}
