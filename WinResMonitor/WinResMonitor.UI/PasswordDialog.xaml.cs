using System.Windows;
using System.Windows.Input;
using WinResMonitor.Core;

namespace WinResMonitor.UI
{
    public partial class PasswordDialog : Window
    {
        private readonly PasswordManager _pm;
        private readonly bool _isSetup;

        public PasswordDialog(PasswordManager pm, bool isSetup = false)
        {
            InitializeComponent();
            _pm = pm;
            _isSetup = isSetup;

            if (isSetup)
            {
                SubtitleText.Text = "No password set. Create one to protect the UI.";
                OkButton.Content = "Set Password";
                ConfirmLabel.Visibility = Visibility.Visible;
                ConfirmBox.Visibility = Visibility.Visible;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e) => TrySubmit();

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (_isSetup) ConfirmBox.Focus();
                else TrySubmit();
            }
        }

        private void ConfirmBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) TrySubmit();
        }

        private void TrySubmit()
        {
            var password = PasswordBox.Password;

            if (_isSetup)
            {
                if (password.Length < 4)
                {
                    ShowError("Password must be at least 4 characters.");
                    return;
                }
                if (password != ConfirmBox.Password)
                {
                    ShowError("Passwords do not match.");
                    ConfirmBox.Clear();
                    ConfirmBox.Focus();
                    return;
                }
                _pm.SetPassword(password);
                DialogResult = true;
            }
            else
            {
                if (_pm.Verify(password))
                {
                    DialogResult = true;
                }
                else
                {
                    ShowError("Incorrect password. Please try again.");
                    PasswordBox.Clear();
                    PasswordBox.Focus();
                }
            }
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
