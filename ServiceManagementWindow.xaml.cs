using MyPanelCarWashing.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MyPanelCarWashing
{
    public partial class ServiceManagementWindow : Window
    {
        private List<Service> _allServices;

        public ServiceManagementWindow()
        {
            InitializeComponent();
            LoadServices();
        }

        private void LoadServices()
        {
            _allServices = Core.DB.GetAllServices().ToList();
            ApplyFilter();
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
            var addWin = new AddEditServiceWindow(null);
            if (addWin.ShowDialog() == true)
            {
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

            var editWin = new AddEditServiceWindow(selectedService);
            if (editWin.ShowDialog() == true)
            {
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
                        Core.RefreshData();
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
            LoadServices();
            MessageBox.Show("Список услуг обновлен", "Обновление",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}