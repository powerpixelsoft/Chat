using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Chat.Client.Wpf
{
    /// <summary>
    /// Interaction logic for PasswordConfirmWindow.xaml
    /// </summary>
    public partial class PasswordConfirmWindow : Window
    {
        private readonly String password;
        private readonly Action openWindowCallback;
        public PasswordConfirmWindow(String password, Action openWindowCallback)
        {
            InitializeComponent();

            this.password = password;
            this.openWindowCallback = openWindowCallback;
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(AuthPasswordBox.Password) || !AuthPasswordBox.Password.Equals(password))
            {
                return;
            }
            else
            {
                openWindowCallback?.Invoke();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width;
            this.Top = desktopWorkingArea.Bottom - this.Height;

            Activate();
            AuthPasswordBox.Focus();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
            else if (e.Key == Key.Enter)
            {
                if (String.IsNullOrEmpty(AuthPasswordBox.Password) || !AuthPasswordBox.Password.Equals(password))
                {
                    return;
                }
                else
                {
                    openWindowCallback?.Invoke();
                }
            }
        }
    }
}
