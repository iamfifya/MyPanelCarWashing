using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MyPanelCarWashing
{
    public partial class AddEditEmployeeWindow : Window
    {
        private SqliteDataService _SqliteDataService;
        public User CurrentEmployee { get; set; }
        public new string Title { get; set; }

        public AddEditEmployeeWindow(SqliteDataService SqliteDataService, User employee)
        {
            InitializeComponent();
            _SqliteDataService = SqliteDataService;

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

        private void PhoneTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;

            // Убираем всё кроме цифр
            string digits = new string(tb.Text.Where(char.IsDigit).ToArray());

            if (digits.Length == 0) return;

            // Форматируем: 8 (999) 609-43-63
            string formatted = "8";
            if (digits.Length > 1) formatted += " (" + digits.Substring(1, Math.Min(3, digits.Length - 1));
            if (digits.Length > 4) formatted += ") " + digits.Substring(4, Math.Min(3, digits.Length - 4));
            if (digits.Length > 7) formatted += "-" + digits.Substring(7, Math.Min(2, digits.Length - 7));
            if (digits.Length > 9) formatted += "-" + digits.Substring(9, Math.Min(2, digits.Length - 9));

            if (tb.Text != formatted)
            {
                tb.Text = formatted;
                tb.CaretIndex = tb.Text.Length;
            }
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
                    _SqliteDataService.AddUser(CurrentEmployee);
                    MessageBox.Show("Сотрудник добавлен", "Успешно",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    _SqliteDataService.UpdateUser(CurrentEmployee);
                    MessageBox.Show("Данные обновлены", "Успешно",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Оповещаем об изменении
                SqliteDataService.NotifyDataChanged();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void PhoneTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, @"^[\d\+\s\-\(\)]+$");
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
