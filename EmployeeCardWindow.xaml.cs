using MyPanelCarWashing.Models;
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

        private List<User> _allEmployees;
        private List<User> _employeesList;

        public List<User> EmployeesList
        {
            get => _employeesList;
            set
            {
                _employeesList = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("EmployeesList"));
            }
        }

        public EmployeeCardWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        // Переопределяем метод OnActivated, который вызывается каждый раз при активации окна
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            LoadEmployees(); // Загружаем свежие данные при каждом показе окна
        }

        private void LoadEmployees()
        {
            // Получаем свежие данные из файла
            _allEmployees = Core.DB.GetAllUsers();
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

            // Принудительно обновляем ListView
            if (EmployeesListView != null)
            {
                EmployeesListView.Items.Refresh();
            }
        }

        private string _searchFilter = "";
        private void SearchFilterTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            _searchFilter = SearchFilterTextBox.Text;
            ApplyFilter();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var addWin = new AddEditEmployeeWindow(null);
            if (addWin.ShowDialog() == true)
            {
                LoadEmployees(); // Обновляем список после добавления
            }
        }

        private void EditItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedEmployee = EmployeesListView.SelectedItem as User;
            if (selectedEmployee != null)
            {
                var editWin = new AddEditEmployeeWindow(selectedEmployee);
                if (editWin.ShowDialog() == true)
                {
                    LoadEmployees(); // Обновляем список после редактирования
                }
            }
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedEmployee = EmployeesListView.SelectedItem as User;
            if (selectedEmployee != null)
            {
                // Защита от удаления главного админа
                if (selectedEmployee.IsAdmin && selectedEmployee.Login == "admin")
                {
                    MessageBox.Show("Нельзя удалить главного администратора!", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show($"Удалить сотрудника {selectedEmployee.FullName}?\n\nЭто действие нельзя отменить.",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Получаем свежие данные
                        var allUsers = Core.DB.GetAllUsers();

                        // Удаляем сотрудника
                        allUsers.RemoveAll(u => u.Id == selectedEmployee.Id);

                        // Сохраняем изменения
                        var appData = FileDataService.LoadData();
                        appData.Users = allUsers;
                        FileDataService.SaveData(appData);

                        // Обновляем Core.DB
                        Core.RefreshData();

                        // Обновляем список
                        LoadEmployees();

                        MessageBox.Show($"Сотрудник {selectedEmployee.FullName} успешно удален", "Успешно",
                            MessageBoxButton.OK, MessageBoxImage.Information);
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
            MessageBox.Show("Список обновлен", "Обновление",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}