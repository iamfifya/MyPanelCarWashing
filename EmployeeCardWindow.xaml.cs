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

        private SqliteDataService _SqliteDataService;
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

        public EmployeeCardWindow(SqliteDataService SqliteDataService)
        {
            InitializeComponent();
            _SqliteDataService = SqliteDataService;
            DataContext = this;
            SqliteDataService.DataChanged += OnDataChanged;

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
            _allEmployees = _SqliteDataService.GetAllUsersIncludingInactive();
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
                _allEmployees = _SqliteDataService.GetAllUsersIncludingInactive();
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
            var addWin = new AddEditEmployeeWindow(_SqliteDataService, null);
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

        private void OpenEditEmployee(User employee)
        {
            var editWin = new AddEditEmployeeWindow(_SqliteDataService, employee);
            if (editWin.ShowDialog() == true)
            {
                LoadEmployees();
            }
        }

        private void ActivateEmployee(User employee)
        {
            employee.IsActive = true;
            _SqliteDataService.UpdateUser(employee);

            // Оповещаем об изменении
            SqliteDataService.NotifyDataChanged();

            LoadEmployees();
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
            var scheduleWin = new ScheduleWindow(_SqliteDataService);
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
