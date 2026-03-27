using MyPanelCarWashing.Services;
using System;
using System.Windows;

namespace MyPanelCarWashing
{
    public partial class LoginWindow : Window
    {
        private readonly DataService _dataService;

        public LoginWindow(DataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var login = LoginTextBox.Text.Trim();
                var password = PasswordBox.Password;

                if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Введите логин и пароль", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var user = _dataService.AuthenticateUser(login, password);

                if (user != null)
                {
                    var mainWindow = new MainWindow(_dataService, user);
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Неверный логин или пароль!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка входа: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
