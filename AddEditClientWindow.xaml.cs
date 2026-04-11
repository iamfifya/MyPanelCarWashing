using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace MyPanelCarWashing
{
    public partial class AddEditClientWindow : Window
    {
        private SqliteDataService _SqliteDataService;
        public Client CurrentClient { get; set; }
        public string WindowTitle { get; set; }

        public AddEditClientWindow(SqliteDataService SqliteDataService, Client client)
        {
            InitializeComponent();
            _SqliteDataService = SqliteDataService;

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

            // Подписка на изменение скидки для валидации
            if (CurrentClient != null)
            {
                // Можно добавить PropertyChanged, но для простоты валидируем при сохранении
            }
        }
        private void DiscountPercentTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(CurrentClient.FullName))
                {
                    MessageBox.Show("Введите ФИО клиента", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(CurrentClient.Phone))
                {
                    MessageBox.Show("Введите телефон клиента", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (CurrentClient.DefaultDiscountPercent < 0 || CurrentClient.DefaultDiscountPercent > 100)
                {
                    MessageBox.Show("Скидка должна быть в диапазоне 0-100%", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (CurrentClient.Id == 0)
                {
                    _SqliteDataService.AddClient(CurrentClient);
                    Logger.Info($"Клиент ДОБАВЛЕН | ФИО: {CurrentClient.FullName} | Тел: {CurrentClient.Phone} | Авто: {CurrentClient.CarNumber}", "CLIENT_AUDIT");
                    MessageBox.Show("Клиент добавлен", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Получаем старые данные для сравнения
                    var oldClient = _SqliteDataService.GetClientById(CurrentClient.Id);
                    string changes = "";

                    if (oldClient != null)
                    {
                        if (oldClient.FullName != CurrentClient.FullName) changes += $"ФИО: '{oldClient.FullName}'→'{CurrentClient.FullName}'; ";
                        if (oldClient.Phone != CurrentClient.Phone) changes += $"Тел: '{oldClient.Phone}'→'{CurrentClient.Phone}'; ";
                        if (oldClient.CarNumber != CurrentClient.CarNumber) changes += $"Авто: '{oldClient.CarNumber}'→'{CurrentClient.CarNumber}'; ";
                        if (oldClient.DefaultDiscountPercent != CurrentClient.DefaultDiscountPercent) changes += $"Скидка: {oldClient.DefaultDiscountPercent}%→{CurrentClient.DefaultDiscountPercent}%; ";
                    }

                    _SqliteDataService.UpdateClient(CurrentClient);

                    // Логируем только если что-то реально изменилось
                    if (!string.IsNullOrEmpty(changes))
                    {
                        Logger.Info($"Клиент ИЗМЕНЁН | ID: {CurrentClient.Id} | {changes.TrimEnd(';', ' ')}", "CLIENT_AUDIT");
                    }

                    MessageBox.Show("Данные клиента обновлены", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка при сохранении клиента", ex, "CLIENT");
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
