using MyPanelCarWashing.Controls;
using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
using System;
using System.Linq;
using System.Windows;

namespace MyPanelCarWashing
{
    public partial class AddEditEmployeeWindow : PopupWindowBase
    {
        private DataService _dataService;
        public User CurrentEmployee { get; set; }
        public new string Title { get; set; }

        public AddEditEmployeeWindow(DataService dataService, User employee)
        {
            InitializeComponent();
            _dataService = dataService;

            if (employee == null)
            {
                CurrentEmployee = new User
                {
                    IsActive = true  // Новый сотрудник активен по умолчанию
                };
                Title = "➕ Добавление сотрудника";
            }
            else
            {
                CurrentEmployee = new User
                {
                    Id = employee.Id,
                    FullName = employee.FullName,
                    Login = employee.Login,
                    Password = employee.Password,
                    IsAdmin = employee.IsAdmin,
                    IsActive = employee.IsActive
                };
                Title = "✏ Редактирование сотрудника";
            }

            DataContext = this;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
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

                string password = PasswordBox.Password;

                if (CurrentEmployee.Id == 0 && string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show("Введите пароль", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(password))
                {
                    CurrentEmployee.Password = password;
                }

                if (CurrentEmployee.Id == 0)
                {
                    _dataService.AddUser(CurrentEmployee);
                    MessageBox.Show("Сотрудник добавлен", "Успешно",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    _dataService.UpdateUser(CurrentEmployee);
                    MessageBox.Show("Данные обновлены", "Успешно",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Оповещаем об изменении
                DataService.NotifyDataChanged();

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
