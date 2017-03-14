using System;
using System.Windows;

namespace Chat.Client.Wpf
{
    using System.Configuration;
    using System.IO;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Forms;
    using System.Windows.Input;
    using System.Windows.Media.Imaging;
    using Chat.Utils;
    using ContextMenu = System.Windows.Forms.ContextMenu;
    using DataFormats = System.Windows.DataFormats;
    using MenuItem = System.Windows.Forms.MenuItem;
    using MessageBox = System.Windows.MessageBox;

    //todo: add dynamic packet buffer expansion
    //todo: encrypt data
    //todo: vanish messages
    //todo: settings
    //todo: icon
    //todo: installer

    public partial class MainWindow : Window
    {
        public String Ip;
        public String ClientName;
        public Int32 Port;

        private TcpClient tcpClient;
        private Stream clientStream;

        private Thread receiveMessagesThread, trayIconThread;
        private volatile Boolean isSending = true, isReceiving = true;

        public Boolean IsConnected => tcpClient != null && tcpClient.Connected;
        private ILogger logger;
        private Packet packet;
        private NotifyIcon notifyIcon = null;

        private ContextMenu trayContextMenu;
        private MenuItem trayOpenMenuItem, trayCloseMenuItem, trayExitMenuItem;
        private String notificationText, notificationTitle;

        private PasswordConfirmWindow passwordWindow;


        #region Initialization
        public MainWindow()
        {
            InitializeComponent();
            Initialize();
        }
        private void Initialize()
        {
            logger = new FileLogger(@"C:\Temp\PopeChat\ClientWpf.txt");

            //tray icon
            trayIconThread = new Thread(StartTrayIconThread);
            trayIconThread.Start();

            IpTextBox.Text = ConfigurationManager.AppSettings["default_ip"];
            PortTextBox.Text = ConfigurationManager.AppSettings["default_port"];

            notificationText = ConfigurationManager.AppSettings["default_notification_text"];
            notificationTitle = ConfigurationManager.AppSettings["default_notification_title"];

            ShowInTaskbar = false;

            OutputRichTextBox?.Document.Blocks.Clear();

            //Microsoft.VisualBasic.Interaction.InputBox("Question?", "Title", "Default Text");
        }
        private void StartTrayIconThread()
        {
            trayContextMenu = new ContextMenu();

            trayOpenMenuItem = new MenuItem("Open");
            trayCloseMenuItem = new MenuItem("Close");
            trayExitMenuItem = new MenuItem("Exit");

            trayContextMenu.MenuItems.Add(0, trayOpenMenuItem);
            trayContextMenu.MenuItems.Add(1, trayCloseMenuItem);
            trayContextMenu.MenuItems.Add("-");
            trayContextMenu.MenuItems.Add(2, trayExitMenuItem);

            notifyIcon = new NotifyIcon
            {
                Visible = true,
                Icon = Properties.Resources.ChatWindowsIcon,
                Text = ConfigurationManager.AppSettings["default_notification_title"],
                ContextMenu = trayContextMenu
            };

            notifyIcon.BalloonTipClicked += (sender, args) =>
            {
                Dispatcher.Invoke
                (
                    () =>
                    {
                        if (IsVisible)
                        {
                            Activate();
                            InputTextBox.Focus();
                        }
                        else
                        {
                            passwordWindow = passwordWindow ?? new PasswordConfirmWindow(ConfigurationManager.AppSettings["default_password"],
                            () =>
                            {
                                Activate();
                                InputTextBox.Focus();
                                SetWindowVisible(true);
                            });
                            passwordWindow.Show();
                        }
                    }
                );
            };

            trayOpenMenuItem.Click += (sender, args) =>
            {
                Dispatcher.Invoke
                (
                    () =>
                    {
                        if (IsVisible)
                        {
                            Activate();
                            InputTextBox.Focus();
                        }
                        else
                        {
                            passwordWindow = passwordWindow ?? new PasswordConfirmWindow(ConfigurationManager.AppSettings["default_password"],
                            () =>
                            {
                                Activate();
                                InputTextBox.Focus();
                                SetWindowVisible(true);
                            });
                            passwordWindow.Show();
                        }
                    }
                );
            };

            trayCloseMenuItem.Click += (sender, args) =>
            {
                SetWindowVisible(false);
            };

            trayExitMenuItem.Click += (sender, args) =>
            {
                notifyIcon.Dispose();
                Environment.Exit(0);
            };

           Application.Run();
        }
        private TcpClient Connect(String ip, Int32 port)
        {
            TcpClient client = new TcpClient();
            client.Connect(ip, port);
            clientStream = client.GetStream();

            OutputRichInfo("ClientProgram is online");

            return client;
        }
        #endregion

        #region Events
        private void ConnectionButton_Click(Object sender, RoutedEventArgs e)
        {
            if (IsConnected) return;

            Ip = IpTextBox.Text;
            ClientName = ClientNameTextBox.Text;
            Port = Convert.ToInt32(PortTextBox.Text);

            if (String.IsNullOrEmpty(Ip) ||
                String.IsNullOrEmpty(ClientName) ||
                Port <= 0)
            {
                OutputRichInfo("Configuration is not complete. Check the fields and format of the data.");
                return;
            }

            try
            {
                packet = new Packet { ClientName = ClientName, Ip = Ip, Message = $"{ClientName} is connected." };
                tcpClient = Connect(Ip, Port);
                SendMessage(clientStream, packet);

                receiveMessagesThread = new Thread(ReceiveRoutine);
                receiveMessagesThread.Start();

                OutputRichInfo("[Receive] thread is ON");
            }
            catch (Exception exception)
            {
                logger.Write("Connection crashed.", exception);
                MessageBox.Show(exception.Message);
            }
        }
        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsConnected) return;
            if (String.IsNullOrEmpty(InputTextBox.Text)) return;

            try
            {
                packet.Message = InputTextBox.Text;
                InputTextBox.Clear();
                SendMessage(clientStream, packet);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
                logger.Write("Sending crashed.", exception);
            }
        }
        private void InputTextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!IsConnected) return;
            if (String.IsNullOrEmpty(InputTextBox.Text)) return;

            try
            {
                if (e.Key == Key.Enter)
                {
                    packet.Message = InputTextBox.Text;
                    InputTextBox.Clear();
                    SendMessage(clientStream, packet);
                }
            }
            catch (Exception exception) { MessageBox.Show(exception.Message); }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!IsConnected)
            {
                notifyIcon.Dispose();
                Environment.Exit(0);
            }
            else
            {
                isReceiving = false;
                SendMessage(clientStream,
                    packet = new Packet { ClientName = ClientName, Ip = Ip, Message = $"{ClientName} is disconnected.", Flag = "end" });

                notifyIcon.Dispose();
                Environment.Exit(0);
            }           
        }
        private void HideButton_Click(object sender, RoutedEventArgs e)
        {
            SetWindowVisible(false);
        }
        private void InputTextBox_Drop(object sender, System.Windows.DragEventArgs e)
        {
            return;
            if(!IsConnected) return;

            String[] files = e.Data.GetData(DataFormats.FileDrop) as String[];
            if (files == null && files.Length > 1) return;

            String extension = Path.GetExtension(files[0]);
            MediaType mediaType = Media.GetMediaType(extension);

            switch (mediaType)
            {
                case MediaType.Text:
                    return;
                case MediaType.Image:
                    packet.Message = HandleImageDrop(files[0]);
                    SendMessage(clientStream, packet, MediaType.Image);
                    break;
                case MediaType.Document:
                    break;
            }
        }
        private void InputTextBox_PreviewDragOver(object sender, System.Windows.DragEventArgs e)
        {
            e.Handled = true;
        }
        private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.F12)
            {
                if (Visibility == Visibility.Visible)
                    Visibility = Visibility.Hidden;
            }
        }
        #endregion

        #region Transmission
        private void SendMessage(Stream clientStream, Packet packet, MediaType mediaType = MediaType.Text)
        {
            try
            {
                if (mediaType.Equals(MediaType.Image))
                    packet.Flag = "img";

                Byte[] messageBytes = packet.Encode();
                clientStream.Write(messageBytes, 0, messageBytes.Length);

                packet.Flag = "";
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }
        private void ReceivePacket(Stream clientStream)
        {
            try
            {
                while (isReceiving)
                {
                    Byte[] expectedBytes = new Byte[ConnectionData.BUFFER_MAX_SIZE];
                    Int32 receivedLength = clientStream.Read(expectedBytes, 0, expectedBytes.Length);

                    if (receivedLength == 0) continue;

                    Byte[] receivedBytes = new Byte[receivedLength];
                    Buffer.BlockCopy(expectedBytes, 0, receivedBytes, 0, receivedLength);

                    Packet receivedPacket = Packet.Decode(receivedBytes);

                    if (receivedPacket.Flag.Equals("img"))
                        Dispatcher.Invoke(() => OutputMessage(receivedPacket.ClientName, receivedPacket.Message, MediaType.Image));
                    else
                        Dispatcher.Invoke(() => OutputMessage(receivedPacket.ClientName, receivedPacket.Message));

                    Dispatcher.Invoke(Notify);
                }
            }
            catch (Exception ex)
            {
                if (ex.InnerException is SocketException)
                {
                    if (!IsConnected)
                    {
                        logger.Write("Receiving crashed.", ex);
                        Dispatcher.Invoke(() => OutputRichInfo("Connection to server was lost..."));
                    }
                    else
                        Dispatcher.Invoke(() => OutputRichInfo("Connection to server was lost..."));
                }
                else
                {
                    logger.Write("Receiving crashed.", ex);
                    MessageBox.Show(ex.Message);
                }
            }
        }
        private void ReceiveRoutine()
        {
            ReceivePacket(clientStream);
        }
        #endregion

        #region Output
        private void Notify()
        {
            if (WindowState == WindowState.Minimized || !IsActive) notifyIcon.ShowBalloonTip(2000, notificationTitle, notificationText, ToolTipIcon.Info);
        }
        private void OutputRichInfo(String message)
        {
            if (OutputRichTextBox == null) return;

            Paragraph paragraph = new Paragraph();
            Run entry = new Run(message);

            paragraph.Inlines.Add(entry);
            TextRange entryRange = new TextRange(entry.ContentStart, entry.ContentEnd);
            entryRange.ApplyPropertyValue(TextElement.ForegroundProperty, System.Windows.Media.Brushes.Teal);
            entryRange.ApplyPropertyValue(TextElement.FontStyleProperty, FontStyles.Italic);

            OutputRichTextBox.Document.Blocks.Add(paragraph);
            OutputRichTextBox.ScrollToEnd();
        }
        private void OutputRichMessage(String date, String id, String message)
        {
            if (OutputRichTextBox == null) return;

            Paragraph paragraph = new Paragraph();
            Run dateEntry = new Run(date);
            Run idEntry = new Run(id);
            Run messageEntry = new Run(message);

            TextRange dateRange = new TextRange(dateEntry.ContentStart, dateEntry.ContentEnd);
            dateRange.ApplyPropertyValue(TextElement.ForegroundProperty, System.Windows.Media.Brushes.GreenYellow);

            TextRange idRange = new TextRange(idEntry.ContentStart, idEntry.ContentEnd);
            idRange.ApplyPropertyValue(TextElement.ForegroundProperty, System.Windows.Media.Brushes.Cyan);

            TextRange messageRange = new TextRange(messageEntry.ContentStart, messageEntry.ContentEnd);
            messageRange.ApplyPropertyValue(TextElement.ForegroundProperty, System.Windows.Media.Brushes.DarkOrange);

            paragraph.Inlines.AddRange(new Run[] { dateEntry, idEntry, messageEntry });
            OutputRichTextBox.Document.Blocks.Add(paragraph);
            OutputRichTextBox.ScrollToEnd();
        }
        private void OutputRichMessageWithImage(String date, String id, Byte[] imageBytes)
        {
            if (OutputRichTextBox == null) return;

            Paragraph paragraph = new Paragraph();
            Run dateEntry = new Run(date);
            Run idEntry = new Run(id);

            TextRange dateRange = new TextRange(dateEntry.ContentStart, dateEntry.ContentEnd);
            dateRange.ApplyPropertyValue(TextElement.ForegroundProperty, System.Windows.Media.Brushes.GreenYellow);

            TextRange idRange = new TextRange(idEntry.ContentStart, idEntry.ContentEnd);
            idRange.ApplyPropertyValue(TextElement.ForegroundProperty, System.Windows.Media.Brushes.Cyan);

            BitmapImage bitmap = new BitmapImage();
            using (var stream = new MemoryStream(imageBytes))
            {
                stream.Seek(0, SeekOrigin.Begin);
                bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.EndInit();
            }

            Image image = new Image { Source = bitmap,  Width = 20 };
            Figure imageFigure = new Figure() { Width=new FigureLength(100) };
            imageFigure.Blocks.Add(new BlockUIContainer(image));

            paragraph.Inlines.AddRange(new Run[] { dateEntry, idEntry });
            OutputRichTextBox.Document.Blocks.Add(paragraph);
            OutputRichTextBox.ScrollToEnd();
        }
        private void OutputMessage(String identity, String message, MediaType mediaType = MediaType.Text)
        {
            String dateText = $"[{DateTime.Now:HH:mm}]";
            String identityText = $" @{identity}";
            String mssg = $" \"{message}\"";

            if (mediaType == MediaType.Text)
                OutputRichMessage(dateText, identityText, mssg);
            else if(mediaType == MediaType.Image)
            {
                Byte[] imageBytes = Encoding.ASCII.GetBytes(message);
                OutputRichMessageWithImage(dateText, identityText, imageBytes);
            }
        }
        #endregion

        #region Visual
        private void RestoreWindow()
        {
            Visibility = Visibility.Visible;
            Activate();
        }
        private void SetWindowVisible(Boolean isVisible)
        {
            if (isVisible)
            {
                Dispatcher.Invoke(() =>
                {
                    Visibility = Visibility.Visible;
                    Activate();

                    passwordWindow?.Close();
                    passwordWindow = null;
                });
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    if (Visibility == Visibility.Visible)
                        Visibility = Visibility.Hidden;
                });
            }
        }

        #endregion

        #region Media types
        private String HandleImageDrop(String imagePath)
        {
            String imageBytesString = String.Empty;

            try
            {
                byte[] imageBytes;

                using (FileStream fstream = File.OpenRead(imagePath))
                {
                    imageBytes = new Byte[fstream.Length];
                    fstream.Read(imageBytes, 0, imageBytes.Length);
                    imageBytesString = Encoding.ASCII.GetString(imageBytes);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

            return imageBytesString;
        }
        #endregion
    }
}
