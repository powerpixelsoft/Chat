using System;
using System.Windows;

namespace Chat.Client.Wpf
{
    using System.Configuration;
    using System.IO;
    using System.Net.Sockets;
    using System.Threading;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Forms;
    using System.Windows.Input;
    using System.Windows.Media.Imaging;
    using Chat.Utils;
    using MessageBox = System.Windows.MessageBox;

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

        private Thread receiveMessagesThread;
        private volatile Boolean isSending = true, isReceiving = true;

        public Boolean IsConnected => tcpClient != null && tcpClient.Connected;
        private ILogger logger;
        private Packet packet;
        private NotifyIcon notifyIcon = null;

        private String notificationText, notificationTitle;

        #region Initialization
        public MainWindow()
        {
            InitializeComponent();
            Initialize();
        }
        private void Initialize()
        {
            logger = new FileLogger(@"C:\Temp\PopeChat\ClientWpf.txt");

            notifyIcon = new NotifyIcon();
            notifyIcon.Click += (s, a) => RestoreWindow();
            notifyIcon.Visible = true;
            notifyIcon.Icon = Properties.Resources.ChatWindowsIcon;

            IpTextBox.Text = ConfigurationManager.AppSettings["default_ip"];
            PortTextBox.Text = ConfigurationManager.AppSettings["default_port"];

            notificationText = ConfigurationManager.AppSettings["default_notification_text"];
            notificationTitle = ConfigurationManager.AppSettings["default_notification_title"];

            ShowInTaskbar = false;

            OutputRichTextBox?.Document.Blocks.Clear();
        }
        private TcpClient Connect(String ip, Int32 port)
        {
            try
            {
                TcpClient client = new TcpClient();
                client.Connect(ip, port);
                clientStream = client.GetStream();

                //OutputInfo("ClientProgram is online");
                OutputRichInfo("ClientProgram is online");

                return client;
            }
            catch (Exception e)
            {
                throw;
            }
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
                //OutputInfo("Configuration is not complete. Check the fields and format of the data.");
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

                //OutputInfo("[Receive] thread is ON");
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
            if (!IsConnected) return;

            isReceiving = false;
            SendMessage(clientStream,
                packet = new Packet { ClientName = ClientName, Ip = Ip, Message = $"{ClientName} is disconnected.", Flag = "end" });

            notifyIcon.Visible = false;
            notifyIcon = null;

            Environment.Exit(0);
        }
        private void HideButton_Click(object sender, RoutedEventArgs e)
        {
            if (Visibility == Visibility.Visible)
                Visibility = Visibility.Hidden;
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
                        Dispatcher.Invoke(() => OutputMessage(receivedPacket.ClientName, receivedPacket.Message));
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
            else
            {
                
            }
        }
        #endregion

        #region Visual
        private void RestoreWindow()
        {
            Visibility = Visibility.Visible;
            Activate();
        }
        #endregion
    }
}
