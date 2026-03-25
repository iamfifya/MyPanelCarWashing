using System;
using System.Linq;
using System.Windows;

namespace MyPanelCarWashing
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();

            // Для отладки - добавляем тестового пользователя, если его нет
            try
            {
                var users = Core.DB.GetAllUsers();
                if (!users.Any())
                {
                    var admin = new Models.User
                    {
                        Id = 1,
                        Login = "admin",
                        Password = "admin",
                        FullName = "Администратор",
                        IsAdmin = true
                    };
                    Core.DB.AddUser(admin);
                    Core.DB.SaveData();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации: {ex.Message}");
            }
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

                var user = Core.DB.AuthenticateUser(login, password);

                if (user != null)
                {
                    var mainWindow = new MainWindow(user);
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
                MessageBox.Show($"Ошибка входа: {ex.Message}\n\n{ex.StackTrace}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}