using DocumentFormat.OpenXml.Drawing.Charts;
using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
using MyPanelCarWashing.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MyPanelCarWashing
{
    public partial class EditOrderWindow : Window
    {
        private DataService _dataService;
        private CarWashOrder _order;
        private Shift _currentShift;
        private List<ServiceViewModel> _services;
        private List<User> _washers;
        private decimal _servicesTotal;
        private decimal _extraCost;

        public EditOrderWindow(DataService dataService, CarWashOrder order, Shift currentShift)
        {
            InitializeComponent();
            _dataService = dataService;
            _order = order;
            _currentShift = currentShift;

            LoadOrderData();
            LoadServices();
            LoadWashers();

            ExtraCostTextBox.TextChanged += ExtraCostTextBox_TextChanged;
        }

        private void LoadOrderData()
        {
            CarModelTextBox.Text = _order.CarModel;
            CarNumberTextBox.Text = _order.CarNumber;

            string bodyType = _order.CarBodyType;
            foreach (ComboBoxItem item in BodyTypeComboBox.Items)
            {
                if (item.Content.ToString() == bodyType)
                {
                    BodyTypeComboBox.SelectedItem = item;
                    break;
                }
            }

            OrderDatePicker.SelectedDate = _order.Time;
            OrderTimeTextBox.Text = _order.Time.ToString("HH:mm");

            switch (_order.BoxNumber)
            {
                case 1: Box1Radio.IsChecked = true; break;
                case 2: Box2Radio.IsChecked = true; break;
                case 3: Box3Radio.IsChecked = true; break;
            }

            ExtraCostTextBox.Text = _order.ExtraCost.ToString();
            ExtraCostReasonTextBox.Text = _order.ExtraCostReason;

            // Загружаем статус
            if (!string.IsNullOrEmpty(_order.Status))
            {
                foreach (ComboBoxItem item in StatusComboBox.Items)
                {
                    string itemText = (item.Content as string)?.Replace("🟡 ", "").Replace("🟢 ", "").Replace("✅ ", "").Replace("❌ ", "") ?? "";
                    if (itemText == _order.Status || item.Content.ToString().Contains(_order.Status))
                    {
                        StatusComboBox.SelectedItem = item;
                        break;
                    }
                }
            }

            // Если это запись, настраиваем интерфейс для конвертации
            if (_order.IsAppointment && _order.AppointmentId.HasValue && _order.Id == 0)
            {
                Title = "Редактирование записи (предварительная)";

                // Делаем поля доступными для редактирования
                CarModelTextBox.IsReadOnly = false;
                CarNumberTextBox.IsReadOnly = false;
                BodyTypeComboBox.IsEnabled = true;
                OrderDatePicker.IsEnabled = true;
                OrderTimeTextBox.IsReadOnly = false;
                Box1Radio.IsEnabled = true;
                Box2Radio.IsEnabled = true;
                Box3Radio.IsEnabled = true;
                ServicesListBox.IsEnabled = true;
                ExtraCostTextBox.IsReadOnly = false;
                ExtraCostReasonTextBox.IsReadOnly = false;

                // Скрываем статус для записи
                StatusComboBox.Visibility = Visibility.Collapsed;

                // Показываем кнопку конвертации
                ConvertToOrderButton.Visibility = Visibility.Visible;
            }
            else
            {
                Title = "Редактирование заказа";
                var convertButton = FindName("ConvertToOrderButton") as Button;

                // Если это запись, настраиваем интерфейс для конвертации
                if (_order.IsAppointment && _order.AppointmentId.HasValue && _order.Id == 0)
                {
                    Title = "Редактирование записи (предварительная)";

                    // Делаем поля доступными для редактирования
                    CarModelTextBox.IsReadOnly = false;
                    CarNumberTextBox.IsReadOnly = false;
                    BodyTypeComboBox.IsEnabled = true;
                    OrderDatePicker.IsEnabled = true;
                    OrderTimeTextBox.IsReadOnly = false;
                    Box1Radio.IsEnabled = true;
                    Box2Radio.IsEnabled = true;
                    Box3Radio.IsEnabled = true;
                    ServicesListBox.IsEnabled = true;
                    ExtraCostTextBox.IsReadOnly = false;
                    ExtraCostReasonTextBox.IsReadOnly = false;

                    // Скрываем статус для записи
                    StatusComboBox.Visibility = Visibility.Collapsed;

                    // Показываем кнопку конвертации
                    if (convertButton != null)
                        convertButton.Visibility = Visibility.Visible;
                }
                else
                {
                    Title = "Редактирование заказа";
                    if (convertButton != null)
                        convertButton.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void LoadServices()
        {
            var allServices = _dataService.GetAllServices();
            _services = allServices.Select(s => new ServiceViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Price = s.GetPrice(1),
                IsSelected = _order.ServiceIds.Contains(s.Id)
            }).ToList();

            ServicesListBox.ItemsSource = _services;
            ServicesListBox.SelectionChanged += ServicesListBox_SelectionChanged;

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
                var allUsers = _dataService.GetAllUsers();
                _washers = allUsers.Where(u => _currentShift.EmployeeIds.Contains(u.Id)).ToList();
            }
            else
            {
                _washers = new List<User>();
            }

            WasherComboBox.ItemsSource = _washers;

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
                if (_services == null) return;

                _servicesTotal = 0;

                foreach (ServiceViewModel service in _services)
                {
                    if (service.IsSelected)
                    {
                        _servicesTotal += service.Price;
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

        private void ConvertToOrderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Если это запись
                if (_order.IsAppointment && _order.AppointmentId.HasValue)
                {
                    var appointment = _dataService.GetAppointmentById(_order.AppointmentId.Value);
                    if (appointment != null && !appointment.IsCompleted)
                    {
                        var result = MessageBox.Show($"Преобразовать запись в заказ?\n\n" +
                            $"🚗 {_order.CarModel} ({_order.CarNumber})\n" +
                            $"📅 {_order.Time:dd.MM.yyyy HH:mm}\n\n" +
                            $"После преобразования запись будет считаться выполненной, а заказ можно будет редактировать.",
                            "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            // Получаем выбранные услуги
                            var selectedServices = _services.Where(s => s.IsSelected).ToList();
                            var serviceIds = selectedServices.Select(s => s.Id).ToList();

                            // Создаем новый заказ
                            var order = new CarWashOrder
                            {
                                CarModel = CarModelTextBox.Text,
                                CarNumber = CarNumberTextBox.Text,
                                CarBodyType = (BodyTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Седан",
                                Time = OrderDatePicker.SelectedDate.Value.Date + TimeSpan.Parse(OrderTimeTextBox.Text),
                                BoxNumber = Box1Radio.IsChecked == true ? 1 : (Box2Radio.IsChecked == true ? 2 : 3),
                                ServiceIds = serviceIds,
                                ExtraCost = _extraCost,
                                ExtraCostReason = ExtraCostReasonTextBox.Text.Trim(),
                                Status = "В ожидании",
                                IsAppointment = false
                            };

                            // Рассчитываем сумму
                            var allServices = _dataService.GetAllServices();
                            order.TotalPrice = serviceIds.Sum(id => allServices.FirstOrDefault(s => s.Id == id)?.GetPrice(1) ?? 0);

                            // Если есть активная смена, добавляем заказ
                            if (_currentShift != null && !_currentShift.IsClosed)
                            {
                                order.Id = _dataService.GetNextOrderId();
                                order.ShiftId = _currentShift.Id;

                                // Назначаем мойщика по умолчанию
                                order.WasherId = _currentShift.EmployeeIds.FirstOrDefault();
                                if (order.WasherId == 0)
                                {
                                    order.WasherId = 1;
                                }

                                _currentShift.Orders.Add(order);

                                // Отмечаем запись как выполненную
                                appointment.IsCompleted = true;
                                appointment.OrderId = order.Id;

                                _dataService.SaveData();

                                MessageBox.Show($"✅ Заказ создан!\n\n" +
                                    $"🚗 {order.CarModel} ({order.CarNumber})\n" +
                                    $"📅 {order.Time:dd.MM.yyyy HH:mm}\n" +
                                    $"💰 Сумма: {order.TotalPrice:N0} ₽",
                                    "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);

                                DialogResult = true;
                                Close();
                            }
                            else
                            {
                                MessageBox.Show("Нет активной смены! Сначала начните смену.", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Это не предварительная запись", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
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

                if (_services == null)
                {
                    MessageBox.Show("Ошибка загрузки услуг", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var selectedServices = _services.Where(s => s.IsSelected).ToList();
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

                // Получаем статус
                string status = (StatusComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "🟡 В ожидании";
                status = status.Replace("🟡 ", "").Replace("🟢 ", "").Replace("✅ ", "").Replace("❌ ", "");

                _order.CarModel = CarModelTextBox.Text;
                _order.CarNumber = CarNumberTextBox.Text;
                _order.CarBodyType = bodyType;
                _order.Time = orderDateTime;
                _order.BoxNumber = boxNumber;
                _order.WasherId = selectedWasher.Id;
                _order.ExtraCost = extraCost;
                _order.ExtraCostReason = extraReason;
                _order.Status = status;

                var serviceIds = selectedServices.Select(s => s.Id).ToList();
                _dataService.UpdateOrderServices(_order.Id, serviceIds);

                _order.TotalPrice = _servicesTotal;

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
                        existingOrder.Status = _order.Status;
                    }
                }
                FileDataService.SaveData(appData);

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
                    $"📋 Статус: {_order.Status}\n" +
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
