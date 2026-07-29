using System.Windows;
using WinResMonitor.Core;

namespace WinResMonitor.UI
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var pm = new PasswordManager();

            // First run — prompt to create a password
            if (!pm.IsPasswordSet)
            {
                var setup = new PasswordDialog(pm, isSetup: true);
                if (setup.ShowDialog() != true)
                {
                    Shutdown();
                    return;
                }
            }

            // Verify password before showing main window
            var login = new PasswordDialog(pm);
            if (login.ShowDialog() != true)
            {
                Shutdown();
                return;
            }

            var main = new MainWindow();
            main.Show();
        }
    }
}
