using MyPanelCarWashing.Models;
using MyPanelCarWashing.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MyPanelCarWashing
{
    public partial class AddOrderWindow : Window
    {
        private Shift _currentShift;
        private List<ServiceViewModel> _services;

        public AddOrderWindow(Shift currentShift)
        {
            InitializeComponent();
            _currentShift = currentShift;

            // Устанавливаем сегодняшнюю дату
            OrderDatePicker.SelectedDate = DateTime.Now;
            OrderTimeTextBox.Text = DateTime.Now.ToString("HH:mm");

            LoadServices();
            LoadWashers();
        }

        private void LoadServices()
        {
            var allServices = Core.DB.GetAllServices();
            _services = allServices.Select(s => new ServiceViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Price = s.Price,
                IsSelected = false
            }).ToList();

            ServicesListBox.ItemsSource = _services;
        }

        private void LoadWashers()
        {
            var washers = Core.DB.GetAllUsers().ToList();
            WasherComboBox.ItemsSource = washers;
            if (washers.Any())
                WasherComboBox.SelectedIndex = 0;
        }

        private void ServicesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CalculateTotalPrice();
        }

        private void CalculateTotalPrice()
        {
            decimal total = 0;
            foreach (ServiceViewModel service in ServicesListBox.SelectedItems)
            {
                total += service.Price;
            }
            TotalPriceTextBlock.Text = $"💰 Итого: {total:C}";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка обязательных полей
                if (string.IsNullOrWhiteSpace(CarModelTextBox.Text))
                {
                    MessageBox.Show("Введите марку и модель автомобиля", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(CarNumberTextBox.Text))
                {
                    MessageBox.Show("Введите государственный номер", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Получаем тип кузова
                string bodyType = (BodyTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Седан";

                // Проверка времени
                DateTime? selectedDate = OrderDatePicker.SelectedDate;
                if (!selectedDate.HasValue)
                {
                    MessageBox.Show("Выберите дату", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!DateTime.TryParse($"{selectedDate.Value:yyyy-MM-dd} {OrderTimeTextBox.Text}", out DateTime orderDateTime))
                {
                    MessageBox.Show("Введите корректное время в формате HH:MM (например 14:30)", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Определяем номер бокса
                int boxNumber = 1;
                if (Box2Radio.IsChecked == true) boxNumber = 2;
                if (Box3Radio.IsChecked == true) boxNumber = 3;

                // Получаем выбранного мойщика
                var selectedWasher = WasherComboBox.SelectedItem as User;
                if (selectedWasher == null)
                {
                    MessageBox.Show("Выберите мойщика", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Получаем выбранные услуги
                var selectedServices = ServicesListBox.SelectedItems.Cast<ServiceViewModel>().ToList();
                if (!selectedServices.Any())
                {
                    MessageBox.Show("Выберите хотя бы одну услугу", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Создаем заказ
                var newOrder = new CarWashOrder
                {
                    CarModel = CarModelTextBox.Text,
                    CarNumber = CarNumberTextBox.Text,
                    CarBodyType = bodyType,
                    Time = orderDateTime,
                    BoxNumber = boxNumber,
                    WasherId = selectedWasher.Id,
                    ShiftId = _currentShift.Id
                };

                var serviceIds = selectedServices.Select(s => s.Id).ToList();
                Core.DB.AddOrder(newOrder, serviceIds);

                MessageBox.Show($"Заказ добавлен!\n\n" +
                    $"🚗 {newOrder.CarModel} ({newOrder.CarNumber})\n" +
                    $"🚘 {newOrder.CarBodyType}\n" +
                    $"🚘 Бокс {newOrder.BoxNumber}\n" +
                    $"👤 Мойщик: {selectedWasher.FullName}\n" +
                    $"⏰ Время: {newOrder.Time:HH:mm dd.MM.yyyy}\n" +
                    $"💰 Сумма: {newOrder.TotalPrice:C}",
                    "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);

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