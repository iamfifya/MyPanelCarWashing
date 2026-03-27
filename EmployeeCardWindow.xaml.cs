using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace MyPanelCarWashing
{
    public partial class EmployeeCardWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly DataService _dataService;
        private List<User> _allEmployees;
        private List<User> _employeesList;
        private string _searchFilter = "";

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
                filtered = filtered.Where(e => e.FullName.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                               e.Login.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            EmployeesList = filtered.ToList();
        }

        private void SearchFilterTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            _searchFilter = SearchFilterTextBox.Text;
            ApplyFilter();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var addWin = new AddEditEmployeeWindow(_dataService, null);
            if (addWin.ShowDialog() == true)
            {
                LoadEmployees();
            }
        }

        private void EditItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedEmployee = EmployeesListView.SelectedItem as User;
            if (selectedEmployee != null)
            {
                var editWin = new AddEditEmployeeWindow(_dataService, selectedEmployee);
                if (editWin.ShowDialog() == true)
                {
                    LoadEmployees();
                }
            }
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedEmployee = EmployeesListView.SelectedItem as User;
            if (selectedEmployee != null)
            {
                if (selectedEmployee.IsAdmin && selectedEmployee.Login == "admin")
                {
                    MessageBox.Show("Нельзя удалить главного администратора!", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show($"Удалить сотрудника {selectedEmployee.FullName}?\n\nЭто действие нельзя отменить.",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var allUsers = _dataService.GetAllUsers();
                        var userToDelete = allUsers.FirstOrDefault(u => u.Id == selectedEmployee.Id);

                        if (userToDelete != null)
                        {
                            allUsers.Remove(userToDelete);

                            var appData = FileDataService.LoadData();
                            appData.Users = allUsers;
                            FileDataService.SaveData(appData);

                            LoadEmployees();

                            MessageBox.Show($"Сотрудник {selectedEmployee.FullName} успешно удален", "Успешно",
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
            else
            {
                MessageBox.Show("Выберите сотрудника для удаления", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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
