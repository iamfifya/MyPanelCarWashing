using System;
using System.Windows;
using MyPanelCarWashing.Services;

namespace MyPanelCarWashing
{
    public partial class LoginWindow : Window
    {
        private DataService _dataService;

        public LoginWindow()
        {
            InitializeComponent();
            _dataService = new DataService();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var user = _dataService.AuthenticateUser(LoginBox.Text, PasswordBox.Password);

                if (user != null)
                {
                    var mainWindow = new MainWindow();
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Неверный логин или пароль", "Ошибка входа",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при входе:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}