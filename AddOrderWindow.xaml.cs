using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
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
        private readonly DataService _dataService;
        private Shift _currentShift;
        private List<ServiceViewModel> _services;
        private List<User> _washers;
        private decimal _servicesTotal;
        private decimal _extraCost;

        public AddOrderWindow(DataService dataService, Shift currentShift)
        {
            InitializeComponent();
            _dataService = dataService;
            _currentShift = currentShift;

            OrderDatePicker.SelectedDate = DateTime.Now;
            OrderTimeTextBox.Text = DateTime.Now.ToString("HH:mm");

            LoadServices();
            LoadWashers();

            ExtraCostTextBox.TextChanged += ExtraCostTextBox_TextChanged;
        }

        private void LoadServices()
        {
            var allServices = _dataService.GetAllServices();
            _services = allServices.Select(s => new ServiceViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Price = s.Price,
                IsSelected = false
            }).ToList();

            ServicesListBox.ItemsSource = _services;
            ServicesListBox.SelectionChanged += ServicesListBox_SelectionChanged;
        }

        private void LoadWashers()
        {
            if (_currentShift != null && _currentShift.EmployeeIds != null && _currentShift.EmployeeIds.Any())
            {
                var allUsers = _dataService.GetAllUsers();
                _washers = allUsers.Where(u => _currentShift.EmployeeIds.Contains(u.Id)).ToList();
            }
            else
            {
                _washers = new List<User>();
                MessageBox.Show("В смене нет сотрудников! Добавьте сотрудников в смену.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            WasherComboBox.ItemsSource = _washers;
            if (_washers.Any())
                WasherComboBox.SelectedIndex = 0;
        }

        private void ServicesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CalculateTotal();
        }

        private void ExtraCostTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateTotal();
        }

        private void CalculateTotal()
        {
            try
            {
                _servicesTotal = 0;

                if (ServicesListBox != null && ServicesListBox.SelectedItems != null)
                {
                    foreach (ServiceViewModel service in ServicesListBox.SelectedItems)
                    {
                        if (service != null)
                        {
                            _servicesTotal += service.Price;
                        }
                    }
                }

                if (ExtraCostTextBox != null && !string.IsNullOrWhiteSpace(ExtraCostTextBox.Text))
                {
                    if (!decimal.TryParse(ExtraCostTextBox.Text, out _extraCost))
                        _extraCost = 0;
                }
                else
                {
                    _extraCost = 0;
                }

                decimal finalTotal = _servicesTotal + _extraCost;

                if (ServicesTotalText != null)
                    ServicesTotalText.Text = $"💰 Услуги: {_servicesTotal:N0} ₽";

                if (ExtraCostText != null)
                    ExtraCostText.Text = $"➕ Дополнительно: {_extraCost:N0} ₽";

                if (TotalPriceTextBlock != null)
                    TotalPriceTextBlock.Text = $"💰 Итого: {finalTotal:N0} ₽";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка CalculateTotal: {ex.Message}");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
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

                string bodyType = (BodyTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Седан";

                DateTime? selectedDate = OrderDatePicker.SelectedDate;
                if (!selectedDate.HasValue)
                {
                    MessageBox.Show("Выберите дату", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string timeStr = OrderTimeTextBox.Text.Trim();
                if (!System.Text.RegularExpressions.Regex.IsMatch(timeStr, @"^([0-1][0-9]|2[0-3]):[0-5][0-9]$"))
                {
                    MessageBox.Show("Введите корректное время в формате HH:MM (например 14:30)", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime orderDateTime = new DateTime(selectedDate.Value.Year, selectedDate.Value.Month, selectedDate.Value.Day,
                    int.Parse(timeStr.Split(':')[0]), int.Parse(timeStr.Split(':')[1]), 0);

                int boxNumber = 1;
                if (Box2Radio.IsChecked == true) boxNumber = 2;
                if (Box3Radio.IsChecked == true) boxNumber = 3;

                var selectedWasher = WasherComboBox.SelectedItem as User;
                if (selectedWasher == null)
                {
                    MessageBox.Show("Выберите мойщика", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var selectedServices = ServicesListBox.SelectedItems.Cast<ServiceViewModel>().ToList();
                if (!selectedServices.Any())
                {
                    MessageBox.Show("Выберите хотя бы одну услугу", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                decimal extraCost = _extraCost;
                string extraReason = ExtraCostReasonTextBox.Text.Trim();

                if (extraCost > 0 && string.IsNullOrWhiteSpace(extraReason))
                {
                    MessageBox.Show("Укажите причину дополнительной стоимости", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var newOrder = new CarWashOrder
                {
                    CarModel = CarModelTextBox.Text,
                    CarNumber = CarNumberTextBox.Text,
                    CarBodyType = bodyType,
                    Time = orderDateTime,
                    BoxNumber = boxNumber,
                    WasherId = selectedWasher.Id,
                    ShiftId = _currentShift.Id,
                    ExtraCost = extraCost,
                    ExtraCostReason = extraReason
                };

                var serviceIds = selectedServices.Select(s => s.Id).ToList();
                _dataService.AddOrder(newOrder, serviceIds);

                string extraMessage = "";
                if (extraCost > 0)
                {
                    extraMessage = $"\n➕ Дополнительно: {extraCost:N0} ₽\n📝 Причина: {extraReason}";
                }

                MessageBox.Show($"Заказ добавлен!\n\n" +
                    $"🚗 {newOrder.CarModel} ({newOrder.CarNumber})\n" +
                    $"🚘 {newOrder.CarBodyType}\n" +
                    $"🚘 Бокс {newOrder.BoxNumber}\n" +
                    $"👤 Мойщик: {selectedWasher.FullName}\n" +
                    $"⏰ Время: {newOrder.Time:HH:mm dd.MM.yyyy}\n" +
                    $"💰 Услуги: {newOrder.TotalPrice:N0} ₽{extraMessage}\n" +
                    $"💵 Итоговая сумма: {newOrder.FinalPrice:N0} ₽\n" +
                    $"👤 Мойщику (35%): {newOrder.WasherEarnings:N0} ₽\n" +
                    $"🏢 Компании (65%): {newOrder.CompanyEarnings:N0} ₽",
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
