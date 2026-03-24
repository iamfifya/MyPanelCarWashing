using System;
using System.Windows;

namespace MyPanelCarWashing
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var login = LoginBox.Text;
            var password = PasswordBox.Password;

            var user = Core.DB.AuthenticateUser(login, password);

            if (user != null)
            {
                var mainWindow = new MainWindow(user);
                mainWindow.Show();
                Close();
            }
            else
            {
                MessageBox.Show("Неверный логин или пароль!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}