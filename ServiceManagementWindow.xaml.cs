using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MyPanelCarWashing
{
    public partial class ServiceManagementWindow : Window
    {
        private DataService _dataService;
        private List<Service> _allServices;

        public ServiceManagementWindow()
        {
            InitializeComponent();
            _dataService = new DataService();
            LoadServices();
        }

        private void LoadServices()
        {
            _allServices = _dataService.GetAllServices().ToList();
            ApplyFilter();

            System.Diagnostics.Debug.WriteLine($"Загружено услуг: {_allServices.Count}");
        }

        private void ApplyFilter()
        {
            string searchText = SearchTextBox.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(searchText))
            {
                ServicesListView.ItemsSource = _allServices;
            }
            else
            {
                var filtered = _allServices
                    .Where(s => s.Name.ToLower().Contains(searchText) ||
                                (s.Description != null && s.Description.ToLower().Contains(searchText)))
                    .ToList();
                ServicesListView.ItemsSource = filtered;
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var addWin = new AddEditServiceWindow(_dataService, null);
            if (addWin.ShowDialog() == true)
            {
                // Обновляем DataService и список после добавления
                _dataService = new DataService();
                LoadServices();
                MessageBox.Show("Услуга добавлена", "Успешно",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedService = ServicesListView.SelectedItem as Service;
            if (selectedService == null)
            {
                MessageBox.Show("Выберите услугу для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var editWin = new AddEditServiceWindow(_dataService, selectedService);
            if (editWin.ShowDialog() == true)
            {
                // Обновляем DataService и список после редактирования
                _dataService = new DataService();
                LoadServices();
                MessageBox.Show("Услуга обновлена", "Успешно",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedService = ServicesListView.SelectedItem as Service;
            if (selectedService == null)
            {
                MessageBox.Show("Выберите услугу для удаления", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Удалить услугу \"{selectedService.Name}\"?\n\nЭто действие нельзя отменить.",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var appData = FileDataService.LoadData();
                    var serviceToDelete = appData.Services.FirstOrDefault(s => s.Id == selectedService.Id);
                    if (serviceToDelete != null)
                    {
                        appData.Services.Remove(serviceToDelete);
                        FileDataService.SaveData(appData);

                        // Обновляем DataService и список
                        _dataService = new DataService();
                        LoadServices();
                        MessageBox.Show("Услуга удалена", "Успешно",
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

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _dataService = new DataService();
            LoadServices();
            MessageBox.Show("Список услуг обновлен", "Обновление",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void CheckIntegrityButton_Click(object sender, RoutedEventArgs e)
        {
            var allServices = _dataService.GetAllServices();

            var duplicateIds = allServices
                .GroupBy(s => s.Id)
                .Where(g => g.Count() > 1)
                .ToList();

            var duplicateNames = allServices
                .GroupBy(s => s.Name)
                .Where(g => g.Count() > 1)
                .ToList();

            string message = $"Всего услуг: {allServices.Count}\n\n";

            if (duplicateIds.Any())
            {
                message += $"⚠️ Найдены дубликаты по ID:\n";
                foreach (var group in duplicateIds)
                {
                    message += $"  ID {group.Key}: {group.Count()} услуг\n";
                }
                message += "\n";
            }

            if (duplicateNames.Any())
            {
                message += $"⚠️ Найдены дубликаты по названию:\n";
                foreach (var group in duplicateNames)
                {
                    message += $"  \"{group.Key}\": {group.Count()} услуг\n";
                }
            }

            if (!duplicateIds.Any() && !duplicateNames.Any())
            {
                message += "✅ Дубликатов не найдено";
            }

            MessageBox.Show(message, "Проверка целостности", MessageBoxButton.OK,
                duplicateIds.Any() || duplicateNames.Any() ? MessageBoxImage.Warning : MessageBoxImage.Information);
        }
    }
}
