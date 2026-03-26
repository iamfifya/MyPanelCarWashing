using MyPanelCarWashing.Models;
using MyPanelCarWashing.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MyPanelCarWashing
{
    public partial class EditOrderWindow : Window
    {
        private CarWashOrder _order;
        private Shift _currentShift;
        private List<ServiceViewModel> _services;
        private List<User> _washers;
        private decimal _servicesTotal;
        private decimal _extraCost;

        public EditOrderWindow(CarWashOrder order, Shift currentShift)
        {
            InitializeComponent();
            _order = order;
            _currentShift = currentShift;

            LoadOrderData();
            LoadServices();
            LoadWashers();

            ExtraCostTextBox.TextChanged += ExtraCostTextBox_TextChanged;
        }

        private void LoadOrderData()
        {
            // Заполняем поля данными заказа
            CarModelTextBox.Text = _order.CarModel;
            CarNumberTextBox.Text = _order.CarNumber;

            // Устанавливаем тип кузова
            string bodyType = _order.CarBodyType;
            foreach (ComboBoxItem item in BodyTypeComboBox.Items)
            {
                if (item.Content.ToString() == bodyType)
                {
                    BodyTypeComboBox.SelectedItem = item;
                    break;
                }
            }

            // Устанавливаем дату и время
            OrderDatePicker.SelectedDate = _order.Time;
            OrderTimeTextBox.Text = _order.Time.ToString("HH:mm");

            // Устанавливаем бокс
            switch (_order.BoxNumber)
            {
                case 1: Box1Radio.IsChecked = true; break;
                case 2: Box2Radio.IsChecked = true; break;
                case 3: Box3Radio.IsChecked = true; break;
            }

            // Дополнительная стоимость
            ExtraCostTextBox.Text = _order.ExtraCost.ToString();
            ExtraCostReasonTextBox.Text = _order.ExtraCostReason;
        }

        private void LoadServices()
        {
            var allServices = Core.DB.GetAllServices();
            _services = allServices.Select(s => new ServiceViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Price = s.Price,
                IsSelected = _order.ServiceIds.Contains(s.Id) // Устанавливаем IsSelected
            }).ToList();

            ServicesListBox.ItemsSource = _services;

            // Подписываемся на событие выделения
            ServicesListBox.SelectionChanged += ServicesListBox_SelectionChanged;

            // Выделяем элементы в ListBox
            for (int i = 0; i < _services.Count; i++)
            {
                if (_services[i].IsSelected)
                {
                    ServicesListBox.SelectedItems.Add(_services[i]);
                }
            }

            CalculateTotal();
        }

        private void ServicesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Синхронизируем свойство IsSelected с выделением в ListBox
            if (_services != null)
            {
                foreach (ServiceViewModel service in _services)
                {
                    service.IsSelected = ServicesListBox.SelectedItems.Contains(service);
                }
                CalculateTotal();
            }
        }

        private void LoadWashers()
        {
            if (_currentShift != null && _currentShift.EmployeeIds != null && _currentShift.EmployeeIds.Any())
            {
                var allUsers = Core.DB.GetAllUsers();
                _washers = allUsers.Where(u => _currentShift.EmployeeIds.Contains(u.Id)).ToList();
            }
            else
            {
                _washers = new List<User>();
            }

            WasherComboBox.ItemsSource = _washers;

            // Выбираем текущего мойщика
            var currentWasher = _washers.FirstOrDefault(w => w.Id == _order.WasherId);
            if (currentWasher != null)
                WasherComboBox.SelectedItem = currentWasher;
            else if (_washers.Any())
                WasherComboBox.SelectedIndex = 0;
        }

        private void ExtraCostTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateTotal();
        }

        private void CalculateTotal()
        {
            try
            {
                // Проверяем, что _services не null
                if (_services == null) return;

                // Сумма услуг - берем из свойства IsSelected
                _servicesTotal = 0;

                foreach (ServiceViewModel service in _services)
                {
                    if (service.IsSelected)
                    {
                        _servicesTotal += service.Price;
                    }
                }

                // Дополнительная стоимость
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

                string timeStr = OrderTimeTextBox.Text.Trim();
                if (!System.Text.RegularExpressions.Regex.IsMatch(timeStr, @"^([0-1][0-9]|2[0-3]):[0-5][0-9]$"))
                {
                    MessageBox.Show("Введите корректное время в формате HH:MM (например 14:30)", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime orderDateTime = new DateTime(selectedDate.Value.Year, selectedDate.Value.Month, selectedDate.Value.Day,
                    int.Parse(timeStr.Split(':')[0]), int.Parse(timeStr.Split(':')[1]), 0);

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

                // Проверяем, что _services не null
                if (_services == null)
                {
                    MessageBox.Show("Ошибка загрузки услуг", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Получаем выбранные услуги из свойства IsSelected
                var selectedServices = _services.Where(s => s.IsSelected).ToList();
                if (!selectedServices.Any())
                {
                    MessageBox.Show("Выберите хотя бы одну услугу", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Получаем дополнительную стоимость и причину
                decimal extraCost = _extraCost;
                string extraReason = ExtraCostReasonTextBox.Text.Trim();

                if (extraCost > 0 && string.IsNullOrWhiteSpace(extraReason))
                {
                    MessageBox.Show("Укажите причину дополнительной стоимости", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Обновляем заказ
                _order.CarModel = CarModelTextBox.Text;
                _order.CarNumber = CarNumberTextBox.Text;
                _order.CarBodyType = bodyType;
                _order.Time = orderDateTime;
                _order.BoxNumber = boxNumber;
                _order.WasherId = selectedWasher.Id;
                _order.ExtraCost = extraCost;
                _order.ExtraCostReason = extraReason;

                var serviceIds = selectedServices.Select(s => s.Id).ToList();

                // Обновляем услуги заказа
                Core.DB.UpdateOrderServices(_order.Id, serviceIds);

                // Обновляем TotalPrice (сумма услуг)
                _order.TotalPrice = _servicesTotal;

                // Сохраняем изменения в заказе
                var appData = FileDataService.LoadData();
                var shift = appData.Shifts.FirstOrDefault(s => s.Id == _order.ShiftId);
                if (shift != null)
                {
                    var existingOrder = shift.Orders.FirstOrDefault(o => o.Id == _order.Id);
                    if (existingOrder != null)
                    {
                        existingOrder.CarModel = _order.CarModel;
                        existingOrder.CarNumber = _order.CarNumber;
                        existingOrder.CarBodyType = _order.CarBodyType;
                        existingOrder.Time = _order.Time;
                        existingOrder.BoxNumber = _order.BoxNumber;
                        existingOrder.WasherId = _order.WasherId;
                        existingOrder.ExtraCost = _order.ExtraCost;
                        existingOrder.ExtraCostReason = _order.ExtraCostReason;
                        existingOrder.TotalPrice = _order.TotalPrice;
                        existingOrder.ServiceIds = serviceIds;
                    }
                }
                FileDataService.SaveData(appData);
                Core.RefreshData();

                // Формируем сообщение
                string extraMessage = "";
                if (extraCost > 0)
                {
                    extraMessage = $"\n➕ Дополнительно: {extraCost:N0} ₽\n📝 Причина: {extraReason}";
                }

                MessageBox.Show($"Заказ обновлен!\n\n" +
                    $"🚗 {_order.CarModel} ({_order.CarNumber})\n" +
                    $"🚘 {_order.CarBodyType}\n" +
                    $"🚘 Бокс {_order.BoxNumber}\n" +
                    $"👤 Мойщик: {selectedWasher.FullName}\n" +
                    $"⏰ Время: {_order.Time:HH:mm dd.MM.yyyy}\n" +
                    $"💰 Услуги: {_order.TotalPrice:N0} ₽{extraMessage}\n" +
                    $"💵 Итоговая сумма: {_order.FinalPrice:N0} ₽\n" +
                    $"👤 Мойщику (35%): {_order.WasherEarnings:N0} ₽\n" +
                    $"🏢 Компании (65%): {_order.CompanyEarnings:N0} ₽",
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