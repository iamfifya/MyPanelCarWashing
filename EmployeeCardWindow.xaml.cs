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

            EmployeesListView.LostFocus += (s, e) =>
            {
                // Не снимаем, если фокус перешел на кнопку редактирования
                var focusedElement = FocusManager.GetFocusedElement(this) as FrameworkElement;

                // Если фокус ушел на кнопки управления - не снимаем выделение
                bool isControlButton = focusedElement is Button &&
                    (focusedElement.Name == "EditClientButton" ||
                     focusedElement.Name == "DeleteClientButton" ||
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
            _allEmployees = _dataService.GetAllUsers();
            ApplyFilter();
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

            var result = MessageBox.Show($"Удалить сотрудника {employee.FullName}?\n\nЭто действие нельзя отменить.",
                "Подтверждение удаления",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var allUsers = _dataService.GetAllUsers();
                    var userToDelete = allUsers.FirstOrDefault(u => u.Id == employee.Id);

                    if (userToDelete != null)
                    {
                        allUsers.Remove(userToDelete);

                        var appData = FileDataService.LoadData();
                        appData.Users = allUsers;
                        FileDataService.SaveData(appData);

                        LoadEmployees();
                        _selectedEmployee = null;

                        MessageBox.Show($"Сотрудник {employee.FullName} успешно удален", "Успешно",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
