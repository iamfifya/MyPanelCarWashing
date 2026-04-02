using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyPanelCarWashing
{
    public partial class EmployeeCardWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private DataService _dataService;
        private List<User> _allEmployees;
        private List<User> _employeesList;
        private string _searchFilter = "";
        private User _selectedEmployee;

        public List<User> EmployeesList
        {
            get => _employeesList;
            set
            {
                _employeesList = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EmployeesList)));
            }
        }

        public EmployeeCardWindow(DataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
            DataContext = this;
            DataService.DataChanged += OnDataChanged;

            EmployeesListView.LostFocus += (s, e) =>
            {
                // Не снимаем, если фокус перешел на кнопку редактирования
                var focusedElement = FocusManager.GetFocusedElement(this) as FrameworkElement;

                // Если фокус ушел на кнопки управления - не снимаем выделение
                bool isControlButton = focusedElement is Button &&
                    (focusedElement.Name == "EditClientButton" ||
                     focusedElement.Name == "DeleteClientButton" ||
                     focusedElement.Name == "ActivateButton" ||
                     focusedElement.Name == "ShowStatsButton");

                if (!isControlButton)
                {
                    EmployeesListView.SelectedItem = null;
                    _selectedEmployee = null;
                }
            };

            LoadEmployees();
        }

        private void LoadEmployees()
        {
            // Используем GetAllUsersIncludingInactive для отображения всех сотрудников
            _allEmployees = _dataService.GetAllUsersIncludingInactive();
            ApplyFilter();
        }
        private bool _isUpdating = false;
        private DateTime _lastUpdate = DateTime.MinValue;

        private void OnDataChanged()
        {
            // Не обновляем чаще чем раз в 100 мс
            if ((DateTime.Now - _lastUpdate).TotalMilliseconds < 100) return;
            _lastUpdate = DateTime.Now;

            Dispatcher.Invoke(() =>
            {
                _allEmployees = _dataService.GetAllUsersIncludingInactive();
                ApplyFilter();
            });
        }

        private void ApplyFilter()
        {
            var filtered = _allEmployees.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(_searchFilter))
            {
                filtered = filtered.Where(e =>
                    e.FullName.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    e.Login.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            EmployeesList = filtered.ToList();
            EmployeesListView.ItemsSource = EmployeesList;
        }

        private void SearchFilterTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            _searchFilter = SearchFilterTextBox.Text.Trim();
            ApplyFilter();
        }

        private void EmployeesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedEmployee = EmployeesListView.SelectedItem as User;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var addWin = new AddEditEmployeeWindow(_dataService, null);
            if (addWin.ShowDialog() == true)
            {
                LoadEmployees();
            }
        }
        // Добавьте эти методы в EmployeeCardWindow.xaml.cs

        private void EmployeesListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_selectedEmployee != null)
            {
                OpenEditEmployee(_selectedEmployee);
                e.Handled = true;
            }
        }

        private void EditEmployeeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEmployee != null)
            {
                OpenEditEmployee(_selectedEmployee);
            }
            else
            {
                MessageBox.Show("Выберите сотрудника для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteEmployeeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEmployee != null)
            {
                DeleteEmployee(_selectedEmployee);
            }
            else
            {
                MessageBox.Show("Выберите сотрудника для удаления", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OpenEditEmployee(User employee)
        {
            var editWin = new AddEditEmployeeWindow(_dataService, employee);
            if (editWin.ShowDialog() == true)
            {
                LoadEmployees();
            }
        }

        private void DeleteEmployee(User employee)
        {
            if (employee.IsAdmin && employee.Login == "admin")
            {
                MessageBox.Show("Нельзя удалить главного администратора!", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверяем, есть ли у сотрудника заказы в текущей открытой смене
            var currentShift = _dataService.GetCurrentOpenShift();
            var hasOrdersInCurrentShift = false;

            if (currentShift != null && currentShift.Orders != null)
            {
                hasOrdersInCurrentShift = currentShift.Orders.Any(o => o.WasherId == employee.Id);
            }

            string warningMessage = hasOrdersInCurrentShift
                ? $"\n\n⚠️ ВНИМАНИЕ: У сотрудника есть заказы в текущей открытой смене!\n" +
                  "При деактивации эти заказы останутся, но новый сотрудник не сможет их взять.\n" +
                  "Рекомендуется сначала завершить или переназначить эти заказы."
                : "";

            var result = MessageBox.Show($"Деактивировать сотрудника {employee.FullName}?\n\n" +
                $"Сотрудник будет скрыт из списка активных, но все его заказы останутся в системе.\n" +
                $"{warningMessage}\n\nЭто действие можно отменить (снова активировать).",
                "Подтверждение деактивации",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    employee.IsActive = false;
                    _dataService.UpdateUser(employee);

                    // Оповещаем об изменении
                    DataService.NotifyDataChanged();

                    LoadEmployees();
                    _selectedEmployee = null;

                    MessageBox.Show($"Сотрудник {employee.FullName} деактивирован.", "Успешно",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при деактивации: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ActivateEmployee(User employee)
        {
            employee.IsActive = true;
            _dataService.UpdateUser(employee);

            // Оповещаем об изменении
            DataService.NotifyDataChanged();

            LoadEmployees();
        }

        // Обновите метод DeleteItem_Click
        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEmployee != null)
            {
                DeleteEmployee(_selectedEmployee);
            }
            else
            {
                MessageBox.Show("Выберите сотрудника для удаления", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        // Обновите метод EditItem_Click
        private void EditItem_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEmployee != null)
            {
                OpenEditEmployee(_selectedEmployee);
            }
            else
            {
                MessageBox.Show("Выберите сотрудника для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void ActivateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEmployee != null)
            {
                ActivateEmployee(_selectedEmployee);
            }
            else
            {
                MessageBox.Show("Выберите сотрудника для активации", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ScheduleButton_Click(object sender, RoutedEventArgs e)
        {
            var scheduleWin = new ScheduleWindow(_dataService);
            scheduleWin.ShowDialog();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadEmployees();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
