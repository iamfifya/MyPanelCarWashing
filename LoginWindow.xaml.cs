using MyPanelCarWashing.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyPanelCarWashing
{
    public partial class LoginWindow : Window
    {
        private DataService _dataService;

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
                    Logger.Warn("Попытка входа с пустыми полями", "AUTH");
                    MessageBox.Show("Введите логин и пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var user = _dataService.AuthenticateUser(login, password);

                if (user != null)
                {
                    // Устанавливаем контекст для всех последующих логов в этой сессии
                    Logger.SetUserContext(user.FullName, user.Id);
                    Logger.Info("Вход выполнен", "AUTH", $"Логин: {login} | Роль: {(user.IsAdmin ? "Админ" : "Сотрудник")}");

                    var mainWindow = new MainWindow(_dataService, user);
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    Logger.Warn("Неудачная попытка входа", "AUTH", $"Логин: '{login}' | IP: localhost | Время: {DateTime.Now:HH:mm:ss}");
                    MessageBox.Show("Неверный логин или пароль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Критическая ошибка при авторизации", ex, "AUTH");
                MessageBox.Show($"Ошибка входа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
