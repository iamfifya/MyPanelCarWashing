using MyPanelCarWashing.Models;
using System;
using System.Linq;
using System.Windows;

namespace MyPanelCarWashing
{
    public partial class AddEditEmployeeWindow : Window
    {
        public User CurrentEmployee { get; set; }
        public string Title { get; set; }

        public AddEditEmployeeWindow(User employee)
        {
            InitializeComponent();

            if (employee == null)
            {
                CurrentEmployee = new User();
                Title = "Добавление сотрудника";
            }
            else
            {
                CurrentEmployee = employee;
                Title = "Редактирование сотрудника";
            }

            DataContext = this;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка обязательных полей
                if (string.IsNullOrWhiteSpace(CurrentEmployee.FullName))
                {
                    MessageBox.Show("Введите ФИО сотрудника", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(CurrentEmployee.Login))
                {
                    MessageBox.Show("Введите логин", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Получаем пароль из PasswordBox
                string password = PasswordBox.Password;

                if (CurrentEmployee.Id == 0 && string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show("Введите пароль", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Устанавливаем пароль (если новый или если пароль изменен)
                if (!string.IsNullOrWhiteSpace(password))
                {
                    CurrentEmployee.Password = password;
                }

                // Если это новый сотрудник, добавляем
                if (CurrentEmployee.Id == 0)
                {
                    Core.DB.AddUser(CurrentEmployee);
                    MessageBox.Show("Сотрудник добавлен", "Успешно",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Обновляем существующего
                    var allUsers = Core.DB.GetAllUsers();
                    var existing = allUsers.FirstOrDefault(u => u.Id == CurrentEmployee.Id);
                    if (existing != null)
                    {
                        existing.FullName = CurrentEmployee.FullName;
                        existing.Login = CurrentEmployee.Login;
                        existing.IsAdmin = CurrentEmployee.IsAdmin;
                        if (!string.IsNullOrWhiteSpace(password))
                        {
                            existing.Password = CurrentEmployee.Password;
                        }
                        Core.DB.SaveData();
                        MessageBox.Show("Данные обновлены", "Успешно",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}