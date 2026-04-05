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
    public partial class ClientsWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private DataService _dataService;
        private List<Client> _allClients;
        private Client _selectedClient;

        public ClientsWindow(DataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
            DataContext = this;

            ClientsListBox.LostFocus += (s, e) =>
            {
                // Не снимаем, если фокус перешел на кнопку редактирования
                var focusedElement = FocusManager.GetFocusedElement(this) as FrameworkElement;

                // Если фокус ушел на кнопки управления - не снимаем выделение
                bool isControlButton = focusedElement is Button &&
                    (focusedElement.Name == "EditClientButton" ||
                     focusedElement.Name == "ShowStatsButton");

                if (!isControlButton)
                {
                    ClientsListBox.SelectedItem = null;
                    _selectedClient = null;
                }
            };

            LoadClients();
        }

        private void LoadClients()
        {
            _allClients = _dataService.GetAllClients();
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            string searchText = SearchTextBox.Text.Trim();
            if (string.IsNullOrEmpty(searchText))
            {
                ClientsListBox.ItemsSource = _allClients;
            }
            else
            {
                var filtered = _allClients.Where(c =>
                    c.FullName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    c.Phone.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    c.CarNumber.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                ClientsListBox.ItemsSource = filtered;
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void AddClient_Click(object sender, RoutedEventArgs e)
        {
            var addWin = new AddEditClientWindow(_dataService, null);
            if (addWin.ShowDialog() == true)
            {
                LoadClients();
            }
        }

        private void EditClient_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedClient != null)
            {
                OpenEditClient(_selectedClient);
            }
            else
            {
                MessageBox.Show("Выберите клиента для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OpenEditClient(Client client)
        {
            var editWin = new AddEditClientWindow(_dataService, client);
            if (editWin.ShowDialog() == true)
            {
                LoadClients();
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadClients();
        }

        private void ShowStatsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedClient == null)
            {
                MessageBox.Show("Выберите клиента для просмотра статистики", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ShowClientStats(_selectedClient);
        }

        private void ClientsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Двойной клик - открываем редактирование
            if (_selectedClient != null)
            {
                OpenEditClient(_selectedClient);
                e.Handled = true;
            }
        }

        private void ClientsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedClient = ClientsListBox.SelectedItem as Client;
        }
        private void EditClientMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedClient != null)
            {
                OpenEditClient(_selectedClient);
            }
            else
            {
                MessageBox.Show("Выберите клиента для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ShowStatsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedClient != null)
            {
                ShowClientStats(_selectedClient);
            }
            else
            {
                MessageBox.Show("Выберите клиента для просмотра статистики", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ShowClientStats(Client client)
        {
            // Получаем заказы клиента
            var clientOrders = _dataService.GetOrdersByClientId(client.Id);

            string message = $"📊 СТАТИСТИКА КЛИЕНТА\n\n" +
                $"👤 {client.FullName}\n" +
                $"📞 {client.Phone}\n" +
                $"🚗 {client.CarModel} ({client.CarNumber})\n\n" +
                $"📅 Зарегистрирован: {client.RegistrationDate:dd.MM.yyyy}\n" +
                $"🔄 Всего визитов: {client.VisitsCount}\n" +
                $"💰 Общая сумма: {client.TotalSpent:N0} ₽\n" +
                $"📊 Средний чек: {client.AverageCheck:N0} ₽\n" +
                $"📅 Последний визит: {(client.LastVisitDate?.ToString("dd.MM.yyyy") ?? "нет")}\n\n" +
                $"📋 История заказов ({clientOrders.Count}):\n";

            foreach (var order in clientOrders.OrderByDescending(o => o.Time).Take(10))
            {
                message += $"\n  {order.Time:dd.MM.yyyy HH:mm} - {order.FinalPrice:N0} ₽ - {order.Status}";
            }

            if (clientOrders.Count > 10)
                message += $"\n\n... и еще {clientOrders.Count - 10} заказов";

            MessageBox.Show(message, $"Клиент: {client.FullName}",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
