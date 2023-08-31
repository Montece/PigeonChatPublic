using Microsoft.Win32;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Media;
using Newtonsoft.Json;
using System.Xml.Serialization;
using NAudio.Wave.SampleProviders;

namespace Pigeon_Client
{
    public partial class Main : Window  
    {
        /*ДЕЛЕГАТЫ*/
        delegate void UserDelegate(string Nickname);
        delegate void ShowNewMessageDelegate(string text);
        delegate void AddServerDelegate(ServerInfo server);
        delegate void ClearServersDelegate();
        delegate void AddUserDelegate(ShortUserInfo user);
        delegate void SetServersListActive(bool status);

        /*ОСНОВНЫЕ*/
        List<User> ServerUsers = new List<User>();
        
        private Thread CommandsRecieverThread;
        private Thread VoiceRecieverThread;

        /*ЗВУК*/
        WaveInEvent WI = new WaveInEvent();
        int DeviceID = 0;
        bool InputEnabled = false;
        //bool OutputEnabled = true;
        bool ListenToMyVoice = false;
        const int MaxMessageLength = 64;
        float Volume = 1;

        public DirectSoundOut MyWO { get; set; }
        public BufferedWaveProvider MyBuffer { get; set; }
        public VolumeSampleProvider MyVolume { get; set; }

        ///<summary> Конструктор </summary>
        public Main()
        {
            InitializeComponent();
            Start_Page.Visibility = Visibility.Visible;
        }
        
        ///<summary> Когда внешне все загрузилось </summary>
        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MyWO = new DirectSoundOut(Library.Latency);
            MyBuffer = new BufferedWaveProvider(Library.SoundFormat);
            MyVolume = new VolumeSampleProvider(MyBuffer.ToSampleProvider());
            MyBuffer.BufferLength = 65536;
            MyWO.Init(MyBuffer);
            MyWO.Init(MyVolume);
            MyWO.Play();

            new Thread(GetServers).Start();
            Chat.InitVoice();
        }
        
        ///<summary> Обновить список микрофонов </summary>
        void LoadMicros()
        {
            Microphones.Items.Clear();
            int count = WaveIn.DeviceCount;
            for (int i = 0; i < count; i++)
            {
                WaveInCapabilities Info = WaveIn.GetCapabilities(i);
                string Device = Info.ProductName;
                Microphones.Items.Add(Device);
            }
            Microphones.SelectedIndex = 0;
            DeviceID = Microphones.SelectedIndex;
        }
        
        ///<summary> Нажатие на отправить </summary>    
        void Send_Click(object sender, RoutedEventArgs e)
        {
            string msg = Message.Text;

            if (msg.Length > 0 && msg != "")
            {
                if (msg[0] == '/')
                {
                    msg = msg.Substring(1);
                    if (msg.Contains("download "))
                    {
                        string filename = msg.Substring(9);
                        Chat.SendCommand(ClientCommands.HasFile, filename);
                    }
                }
                else
                {
                    if (msg.Length < MaxMessageLength) Chat.SendTextMessage(msg);
                }

                Message.Text = "";
            }
        }
       
        ///<summary> Добавить человека в список людей на сервере </summary>
        void AddUser(ShortUserInfo user)
        {
            User u = new User(user.Nickname, user.Login);
            bool canAdd = true;
            foreach (User item in ServerUsers)
            {
                if (item.Login == user.Login)
                {
                    canAdd = false;
                    break;
                }
            }
            if (!canAdd) return;
            u.Buffer.BufferLength = 65536;
            u.WO.Init(u.Buffer);
            u.WO.Init(u.Volume);
            u.Volume.Volume = Volume;
            //u.WO.Volume = Volume;
            u.WO.Play();
            ServerUsers.Add(u);
            Users.Items.Add(u.Nickname);
        }

        void RemoveUser(ShortUserInfo user)
        {
            User u = new User(user.Nickname, user.Login);
            bool canRemove = false;
            int index = 0;
            for (int i = 0; i < ServerUsers.Count; i++)
            {
                if (ServerUsers[i].Login == user.Login)
                {
                    canRemove = true;
                    index = i;
                    break;
                }
            }
            if (!canRemove) return;
            ServerUsers[index].WO.Stop();
            ServerUsers.RemoveAt(index);
            Users.Items.RemoveAt(index);
        }

        void AddUser_async(ShortUserInfo user)
        {
            Dispatcher.Invoke(new AddUserDelegate(AddUser), user);
        }

        void RemoveUser_async(ShortUserInfo user)
        {
            Dispatcher.Invoke(new AddUserDelegate(RemoveUser), user);
        }

        ///<summary> Прослушиватель UDP </summary>
        void CommandsReceiver()
        {
            //string history = Chat.LoadHistory();
            //ShowNewMessage_async(history);
            List<ShortUserInfo> Users = Chat.GetUsers();

            foreach (ShortUserInfo user in Users) AddUser_async(user);

            List<byte> request = new List<byte>();
            ServerCommand JSONcommand;
            XmlSerializer UsersXML = new XmlSerializer(typeof(ShortUserInfo));
            StringReader reader;
            ShortUserInfo data;

            while (true)
            {
                request.Clear();
                JSONcommand = null;
                GC.Collect();
                Thread.Sleep(0);

                try
                {
                    request = Chat.CommandsUdp.Receive(ref Chat.CommandsEnd).ToList();
                }
                catch
                {
                    continue;
                }

                try
                {
                    JSONcommand = JsonConvert.DeserializeObject<ServerCommand>(Encoding.UTF8.GetString(request.ToArray()));
                }
                catch
                {
                    continue;
                }

                if (JSONcommand == null) continue;

                switch (JSONcommand.CommandID)
                {
                    case ServerCommands.Kick:
                        MessageBox.Show("Сервер выгнал вас!");
                        break;
                    case ServerCommands.TextMessage:
                        ShowNewMessage_async(JSONcommand.Parameters[0]);
                        break;
                    case ServerCommands.OnUserConnect:
                        reader = new StringReader(JSONcommand.Parameters[0]);
                        data = (ShortUserInfo)UsersXML.Deserialize(reader);
                        AddUser_async(data);
                        break;
                    case ServerCommands.OnUserDisconnect:
                        reader = new StringReader(JSONcommand.Parameters[0]);
                        data = (ShortUserInfo)UsersXML.Deserialize(reader);
                        RemoveUser_async(data);
                        break;
                    case ServerCommands.SendFileStatus:
                        if (bool.Parse(JSONcommand.Parameters[0]))
                        {
                            string filename = JSONcommand.Parameters[1];
                            ShowNewMessage_async($"\n[SERVER]: Скачивание файла {filename}...");
                            Chat.SendCommand(ClientCommands.DownloadFile, filename);
                            new Thread(() => DownloadFile(filename)).Start();
                        }
                        else ShowNewMessage_async("\n[SERVER]: Файл не найден!");
                        break;
                    default:
                        break;
                }
            }
        }

        void VoiceReceiver()
        {
            List<byte> VoiceRequest = new List<byte>();
            VoiceCommand JSONcommand;
            XmlSerializer UsersXML = new XmlSerializer(typeof(ShortUserInfo));

            while (true)
            {
                VoiceRequest.Clear();
                JSONcommand = null;

                try
                {
                    VoiceRequest = Chat.VoiceUdp.Receive(ref Chat.VoiceEnd).ToList();
                }
                catch
                {
                    continue;
                }

                int length = VoiceRequest[0];
                VoiceRequest.RemoveAt(0);
                XmlSerializer VoiceXML = new XmlSerializer(typeof(VoiceCommand));
                StringReader reader = new StringReader(Encoding.UTF8.GetString(VoiceRequest.GetRange(0, length).ToArray()));
                VoiceCommand command = (VoiceCommand)VoiceXML.Deserialize(reader);
                VoiceRequest.RemoveRange(0, length);
                byte[] array = VoiceRequest.ToArray();

                foreach (User user in ServerUsers)
                {
                    if (user.Login == command.Login)
                        user.Buffer.AddSamples(array, 0, array.Length);
                };
            }
        }

        private void DownloadFile(string filename)
        {
            if (!Directory.Exists("Files/")) Directory.CreateDirectory("Files/");
            if (File.Exists("Files/" + filename)) File.Delete("Files/" + filename);

            TcpListener tcp = new TcpListener(new IPEndPoint(IPAddress.Any, Chat.CLIENT_FILE_PORT));
            tcp.Start();

            using (Socket socket = tcp.AcceptSocket())
            {
                while (socket.Available == 0) { }

                byte[] buffer = new byte[socket.Available];
                socket.Receive(buffer);

                int packages_count = BitConverter.ToInt32(buffer, 0);
                buffer = new byte[Chat.FILE_PACKAGE_SIZE];

                using (FileStream fs = new FileStream("Files/" + filename, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    for (int i = 0; i < packages_count; i++)
                    {
                        buffer = new byte[socket.Available];
                        socket.Receive(buffer);
                        fs.Write(buffer, 0, buffer.Length);
                    }

                    fs.Close();
                }

                socket.Close();
            }

            ShowNewMessage_async($"\n[SERVER]: Скачивание {filename} завершено.");
        }

        ///<summary> Добавление сообщения в историю </summary>
        void ShowNewMessage(string text)
        {
            History.AppendText(text);
            History.ScrollToEnd();
        }

        ///<summary> Добавление сообщения в историю </summary>
        void ShowNewMessage_async(string text)
        {
            Dispatcher.Invoke(new ShowNewMessageDelegate(ShowNewMessage), text);
        }
        /*
        ///<summary> Начало скачивания файла </summary>
        void StartDownload()
        {
            Chat.udp.Client.ReceiveBufferSize = 32768;

            WaitingFileInfo = WaitingFileInfo.Substring(10);
            FilePackagesCount = int.Parse(WaitingFileInfo.Substring(0, WaitingFileInfo.IndexOf(Chat.CommandChar)));
            WaitingFileInfo = WaitingFileInfo.Substring(WaitingFileInfo.IndexOf(Chat.CommandChar) + 1);
            FileType = WaitingFileInfo;
            FilePackagesI = 1;
            IsWaitingFile = true;
        }

        ///<summary> Конец скачивания файла </summary>
        void StopDownload()
        {
            string Path = "";

            SaveFileDialog Save = new SaveFileDialog();
            Save.FileName = FileType;
            if (Save.ShowDialog() == true)
            {
                Path = Save.FileName;
                fs = new FileStream(Path, FileMode.Create, FileAccess.Write);
                fs.Write(file.ToArray(), 0, file.ToArray().Length);
                fs.Close();
                Library.Error("Файл успешно скачан.", "Файл " + FileType + " был скачан. Размер: " + file.Count + " байт.", Library.AlertType.Notification);
                IsWaitingFile = false;
                FileType = "";
            }
        }
       */
        ///<summary> Включить звук с микрофона </summary>
        void InputVoice_Checked(object sender, RoutedEventArgs e)
        {
            StartRecording();
        }

        ///<summary> Выключить звук с микрофона </summary>
        void InputVoice_UnChecked(object sender, RoutedEventArgs e)
        {
            StopRecording();
        }
        
        ///<summary> Настройка микрофона </summary>
        void StartRecording()
        {
            InputEnabled = true;

            if (WI != null) WI.Dispose();
            WI = new WaveInEvent();

            try
            {
                WI.WaveFormat = new WaveFormat(Library.Rate, Library.Sample, Library.Channels);
                WI.DataAvailable += Voice_Input;
                WI.DeviceNumber = DeviceID;
                WI.StartRecording();
            }
            catch (Exception e)
            {
                Library.Error("Ошибка включения микрофона!", "Микрофон не найден или поврежден. " + e.Message, Library.AlertType.Error);
            }
        }

        ///<summary> Выключение микрофона </summary>
        void StopRecording()
        {
            InputEnabled = false;
            WI.StopRecording();    
        }

        ///<summary> Отправка входящего голоса </summary>
        void Voice_Input(object sender, WaveInEventArgs e)
        {
            if (ListenToMyVoice) MyBuffer.AddSamples(e.Buffer, 0, e.Buffer.Length);
            Chat.SendVoice(e.Buffer);
        }       

        ///<summary> Закрытие </summary>
        void OnWindowClosing(object sender, CancelEventArgs e)
        {
            Exit();
        }

        void Exit()
        {
            if (IsOnServer)
            {
                Chat.Stop();
                if (WI != null)
                {
                    WI.Dispose();
                    WI = null;
                }
            }
            Environment.Exit(0);
        }

        ///<summary> Прокрутка вниз истории </summary>
        void History_TextChanged(object sender, TextChangedEventArgs e)
        { 
            History.ScrollToEnd();
        }
        
        ///<summary> Отключение от сервера </summary>
        void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show("В разработке", "В разработке", MessageBoxButton.OK, MessageBoxImage.Information);
            IsOnServer = false;
            Chat.Stop();
            CommandsRecieverThread.Abort();
            VoiceRecieverThread.Abort();
            DoubleAnimation Start_PageAnimation_Height = new DoubleAnimation();
            Start_PageAnimation_Height.From = Start_Page.ActualHeight;
            Start_PageAnimation_Height.To = Start_Page.ActualHeight / 1.1;
            Start_PageAnimation_Height.Duration = TimeSpan.FromSeconds(1);

            /*DoubleAnimation Start_PageAnimation_Width = new DoubleAnimation();
            Start_PageAnimation_Width.From = Start_Page.ActualWidth;
            Start_PageAnimation_Width.To = Start_Page.ActualWidth / 1.1;
            Start_PageAnimation_Width.Duration = TimeSpan.FromSeconds(1);*/

            Start_Page.BeginAnimation(HeightProperty, Start_PageAnimation_Height);
           // Start_Page.BeginAnimation(WidthProperty, Start_PageAnimation_Width);
            Start_Page.Opacity = 1;
        }

        #region Style
        bool FullScreen_Button_True = false;

        public bool IsOnServer { get; private set; }
        
        void Close_Button_Click(object sender, RoutedEventArgs e)
        {
            Exit();
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

        BitmapImage LoadBitmapFromResource(string Path)
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

        void Move_Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                DragMove();
            }
            catch
            {
                
            }
        }
        #endregion

        ///<summary> Нажатие отправки файла </summary>
        void SendFile_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("В разработке", "В разработке", MessageBoxButton.OK, MessageBoxImage.Information);
            //new Thread(SendFile).Start();
        }
        
        ///<summary> Получение списка серверов </summary>
        void GetServers()
        {
            Chat.GetServers();
            ShowServers();
        }

        void ShowServers()
        {
            Dispatcher.Invoke(new ClearServersDelegate(ClearServersList));
            foreach (ServerInfo server in Chat.Servers) Dispatcher.Invoke(new AddServerDelegate(AddServer), server);
        }

        ///<summary> Добавление сервера в список </summary>
        void AddServer(ServerInfo server)
        {
            string text = server.Title;
            if (server.Password != "") text += " (Приватный)";
            ListboxServers.Items.Add(text);
        }

        void ClearServersList()
        {
            ListboxServers.Items.Clear();
        }

        ///<summary> Нажатие на сервер </summary>
        void ListboxServers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListboxServers.SelectedIndex >= 0)
            {
                int index = ListboxServers.SelectedIndex;
                ListboxServers.SelectedIndex = -1;

                if (Chat.SetCurrentServer(index))
                {                 
                    if (Chat.ConnectToServer())
                    {
                        ListboxServers.IsEnabled = false;
                        Success();
                    }
                    else Library.Error("Ошибка подсоединения", "Сервер отказал в доступе.");
                }
                else Library.Error("Ошибка подсоединения", "Сервер не найден.");
            }
        }
        
        ///<summary> Показ основного окна </summary>
        void Success()
        {
            LoadMicros();
            CommandsRecieverThread = new Thread(CommandsReceiver) { Name = "Commands Reciever" };
            CommandsRecieverThread.Start();
            VoiceRecieverThread = new Thread(VoiceReceiver) { Name = "Voice Reciever" };
            VoiceRecieverThread.Start();
            Start_Page.Height = 0;
            IsOnServer = true;

            DoubleAnimation Start_PageAnimation = new DoubleAnimation
            {
                From = Start_Page.ActualHeight,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.5)
            };
            Start_Page.BeginAnimation(HeightProperty, Start_PageAnimation);
            new Thread(EnableServers).Start();
            
        }

        void EnableServers()
        {
            Dispatcher.Invoke(new SetServersListActive(SetServersListStatus), false);
            Thread.Sleep(1000);
            Dispatcher.Invoke(new SetServersListActive(SetServersListStatus), true);
        }

        void SetServersListStatus(bool status)
        {
            ListboxServers.IsEnabled = status;
        }
        
        ///<summary> Изменение текущего микрофона </summary>
        void Microphones_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DeviceID = Microphones.SelectedIndex;
            if (InputEnabled) StartRecording();
        }

        /*///<summary> Включение голоса от других </summary>
        void OutputVoice_Checked(object sender, RoutedEventArgs e)
        {
            OutputEnabled = true;
        }

        ///<summary> Выключение голоса от других </summary>
        void OutputVoice_Unchecked(object sender, RoutedEventArgs e)
        {
            OutputEnabled = false;
        }*/
        
        ///<summary> Логаут </summary>
        void GoAuth_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            new Auth().Show();
        }

        void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ChangeVolume(VolumeSlider.Value);
        }

        void ChangeVolume(double volume)
        {
            Volume = (float)volume;
            foreach (var item in ServerUsers)
            {
                item.Volume.Volume = Volume;
            }
            if (MyVolume != null) MyVolume.Volume = Volume;
        }

        void InputVoiceMy_Checked(object sender, RoutedEventArgs e)
        {
            ListenToMyVoice = true;
        }

        void InputVoiceMy_UnChecked(object sender, RoutedEventArgs e)
        {
            ListenToMyVoice = false;
        }
    }
}