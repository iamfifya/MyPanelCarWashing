using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MyPanelCarWashing
{
    public partial class ServiceManagementWindow : Window
    {
        private DataService _dataService;
        private List<Service> _allServices;

        public ServiceManagementWindow(DataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
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
            IEnumerable<Service> filtered;

            if (string.IsNullOrEmpty(searchText))
            {
                filtered = _allServices;
            }
            else
            {
                filtered = _allServices
                    .Where(s => s.Name.ToLower().Contains(searchText) ||
                                (s.Description != null && s.Description.ToLower().Contains(searchText)));
            }

            // ListBox сам обновится при смене ItemsSource
            ServicesListBox.ItemsSource = filtered;
            ServicesListBox.SelectedItem = null; // Сброс выделения при фильтрации
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        // Снимает выделение при клике на пустое место
        private void ServicesListBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Ищем, не кликнули ли мы по ListBoxItem (карточке)
            var source = e.OriginalSource as DependencyObject;
            var listBoxItem = FindParent<ListBoxItem>(source);

            // Если клик не по элементу списка — снимаем выделение
            if (listBoxItem == null)
            {
                ServicesListBox.SelectedItem = null;
                e.Handled = true;
            }
        }

        // Вспомогательный метод для поиска родителя
        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null && !(parent is T))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as T;
        }

        // Получаем выбранную услугу из ListBox
        private Service GetSelectedService()
        {
            return ServicesListBox.SelectedItem as Service;
        }

        private void EditService(Service service)
        {
            if (service == null) return;

            var editWin = new AddEditServiceWindow(_dataService, service);
            if (editWin.ShowDialog() == true)
            {
                _dataService = new DataService();
                LoadServices();
                MessageBox.Show("Услуга обновлена", "Успешно",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteService(Service service)
        {
            if (service == null) return;

            var result = MessageBox.Show($"Удалить услугу \"{service.Name}\"?\n\nЭто действие нельзя отменить.\n\n" +
                $"ВНИМАНИЕ: Если услуга используется в заказах, удаление может вызвать ошибки!",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var appData = FileDataService.LoadData();
                    var serviceToDelete = appData.Services.FirstOrDefault(s => s.Id == service.Id);
                    if (serviceToDelete != null)
                    {
                        appData.Services.Remove(serviceToDelete);
                        FileDataService.SaveData(appData);

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

        private void EditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is Service service)
            {
                EditService(service);
            }
        }

        private void CopyNameMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is Service service)
            {
                Clipboard.SetText(service.Name);
                MessageBox.Show($"Название услуги \"{service.Name}\" скопировано в буфер обмена",
                    "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var addWin = new AddEditServiceWindow(_dataService, null);
            if (addWin.ShowDialog() == true)
            {
                _dataService = new DataService();
                LoadServices();
                MessageBox.Show("Услуга добавлена", "Успешно",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var service = GetSelectedService();
            if (service != null)
            {
                EditService(service);
            }
            else
            {
                MessageBox.Show("Выберите услугу для редактирования (нажмите на карточку или используйте контекстное меню)",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var service = GetSelectedService();
            if (service != null)
            {
                DeleteService(service);
            }
            else
            {
                MessageBox.Show("Выберите услугу для удаления (нажмите на карточку или используйте контекстное меню)",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Двойной клик по карточке = редактирование
        private void ServicesListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var service = GetSelectedService();
            if (service != null)
            {
                EditService(service);
                e.Handled = true;
            }
        }
    }
}
