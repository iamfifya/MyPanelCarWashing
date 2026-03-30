using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
using System;
using System.Windows;

namespace MyPanelCarWashing
{
    public partial class AddEditClientWindow : Window
    {
        private DataService _dataService;
        public Client CurrentClient { get; set; }
        public string WindowTitle { get; set; }

        public AddEditClientWindow(DataService dataService, Client client)
        {
            InitializeComponent();
            _dataService = dataService;

            if (client == null)
            {
                CurrentClient = new Client();
                WindowTitle = "➕ Добавление клиента";
            }
            else
            {
                CurrentClient = new Client
                {
                    Id = client.Id,
                    FullName = client.FullName,
                    Phone = client.Phone,
                    CarModel = client.CarModel,
                    CarNumber = client.CarNumber,
                    Notes = client.Notes,
                    RegistrationDate = client.RegistrationDate,
                    TotalSpent = client.TotalSpent,
                    VisitsCount = client.VisitsCount,
                    LastVisitDate = client.LastVisitDate
                };
                WindowTitle = "✏ Редактирование клиента";
            }

            DataContext = this;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(CurrentClient.FullName))
                {
                    MessageBox.Show("Введите ФИО клиента", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(CurrentClient.Phone))
                {
                    MessageBox.Show("Введите телефон клиента", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (CurrentClient.Id == 0)
                {
                    _dataService.AddClient(CurrentClient);
                    MessageBox.Show("Клиент добавлен", "Успешно",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    _dataService.UpdateClient(CurrentClient);
                    MessageBox.Show("Данные клиента обновлены", "Успешно",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
