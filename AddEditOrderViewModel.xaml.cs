// AddEditOrderWindow.xaml.cs
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
    public partial class AddEditOrderWindow : Window
    {
        private readonly DataService _dataService;
        private readonly Shift _currentShift;
        private readonly AddEditOrderViewModel _viewModel;

        public AddEditOrderWindow(DataService dataService, Shift currentShift, CarWashOrder order = null)
        {
            InitializeComponent();
            _dataService = dataService;
            _currentShift = currentShift;
            _viewModel = new AddEditOrderViewModel(dataService, currentShift, order);
            DataContext = _viewModel;

            // Заполняем дату/время
            if (order != null)
            {
                OrderDatePicker.SelectedDate = order.Time;
                OrderTimeTextBox.Text = order.Time.ToString("HH:mm");
            }
            else
            {
                OrderDatePicker.SelectedDate = DateTime.Now;
                OrderTimeTextBox.Text = DateTime.Now.ToString("HH:mm");
            }

            // Выбираем статус, если редактируем
            if (_viewModel.IsEditMode && order != null && !string.IsNullOrEmpty(order.Status))
            {
                foreach (ComboBoxItem item in StatusComboBox.Items)
                {
                    string itemText = (item.Content as string)?.Replace("🟡 ", "").Replace("🟢 ", "").Replace("✅ ", "").Replace("❌ ", "") ?? "";
                    if (itemText == order.Status)
                    {
                        StatusComboBox.SelectedItem = item;
                        break;
                    }
                }
            }

            // Выбираем способ оплаты
            if (order != null && !string.IsNullOrEmpty(order.PaymentMethod))
            {
                foreach (ComboBoxItem item in PaymentMethodComboBox.Items)
                {
                    string itemText = (item.Content as string)?.Replace("💵 ", "").Replace("💳 ", "").Replace("📱 ", "") ?? "";
                    if (itemText == order.PaymentMethod)
                    {
                        PaymentMethodComboBox.SelectedItem = item;
                        break;
                    }
                }
            }

            // Подписываемся на выбор услуг
            ServicesListBox.SelectionChanged += (s, e) =>
            {
                foreach (ServiceViewModel service in ServicesListBox.Items)
                {
                    service.IsSelected = ServicesListBox.SelectedItems.Contains(service);
                }
                _viewModel.CalculateTotal();
            };

            // Изначально выбираем услуги
            if (_viewModel.Services != null)
            {
                foreach (var service in _viewModel.Services)
                {
                    if (service.IsSelected)
                    {
                        ServicesListBox.SelectedItems.Add(service);
                    }
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Обновляем время из UI
                if (OrderDatePicker.SelectedDate.HasValue)
                {
                    string timeStr = OrderTimeTextBox.Text.Trim();
                    if (System.Text.RegularExpressions.Regex.IsMatch(timeStr, @"^([0-1][0-9]|2[0-3]):[0-5][0-9]$"))
                    {
                        var date = OrderDatePicker.SelectedDate.Value;
                        var timeParts = timeStr.Split(':');
                        var orderDateTime = new DateTime(date.Year, date.Month, date.Day,
                            int.Parse(timeParts[0]), int.Parse(timeParts[1]), 0);
                        _viewModel.CurrentOrder.Time = orderDateTime;
                    }
                    else
                    {
                        MessageBox.Show("Введите корректное время в формате HH:MM", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Обновляем статус для редактирования
                if (_viewModel.IsEditMode && StatusComboBox.SelectedItem is ComboBoxItem statusItem)
                {
                    string status = statusItem.Content.ToString();
                    status = status.Replace("🟡 ", "").Replace("🟢 ", "").Replace("✅ ", "").Replace("❌ ", "");
                    _viewModel.CurrentOrder.Status = status;
                }

                // Обновляем способ оплаты
                if (PaymentMethodComboBox.SelectedItem is ComboBoxItem paymentItem)
                {
                    string payment = paymentItem.Content.ToString();
                    payment = payment.Replace("💵 ", "").Replace("💳 ", "").Replace("📱 ", "");
                    _viewModel.CurrentOrder.PaymentMethod = payment;
                }

                // Обновляем мойщика (только если это не запись)
                if (!_viewModel.IsAppointment && WasherComboBox.SelectedItem is User selectedWasher)
                {
                    _viewModel.CurrentOrder.WasherId = selectedWasher.Id;
                }

                // Сохраняем
                _viewModel.SaveOrder(out bool success, out string message);

                if (success)
                {
                    MessageBox.Show(message, "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ConvertToOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_viewModel.IsAppointment) return;

            var result = MessageBox.Show($"Преобразовать запись в заказ?\n\n" +
                $"🚗 {_viewModel.CurrentOrder.CarModel} ({_viewModel.CurrentOrder.CarNumber})\n" +
                $"🚘 Категория кузова: {GetCategoryName(_viewModel.SelectedBodyTypeCategory)}\n" +
                $"📅 {_viewModel.CurrentOrder.Time:dd.MM.yyyy HH:mm}\n" +
                $"💰 Сумма: {_viewModel.FinalTotal:N0} ₽\n\n" +
                $"После преобразования запись будет считаться выполненной, а заказ можно будет редактировать.",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            if (_currentShift == null || _currentShift.IsClosed)
            {
                MessageBox.Show("Нет активной смены! Сначала начните смену.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var selectedServices = _viewModel.Services.Where(s => s.IsSelected).ToList();
                var serviceIds = selectedServices.Select(s => s.Id).ToList();

                if (!serviceIds.Any())
                {
                    MessageBox.Show("Выберите хотя бы одну услугу", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var appData = FileDataService.LoadData();

                // Находим оригинальную запись
                var appointment = appData.Appointments.FirstOrDefault(a => a.Id == _viewModel.CurrentOrder.AppointmentId.Value);
                if (appointment == null)
                {
                    MessageBox.Show("Запись не найдена!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"=== ПРЕОБРАЗОВАНИЕ ЗАПИСИ ===");
                System.Diagnostics.Debug.WriteLine($"Appointment ID: {appointment.Id}");
                System.Diagnostics.Debug.WriteLine($"Appointment IsCompleted: {appointment.IsCompleted}");
                System.Diagnostics.Debug.WriteLine($"Appointment OrderId: {appointment.OrderId}");

                // Создаем новый заказ
                var newOrderId = GetNextOrderId(appData);
                var order = new CarWashOrder
                {
                    Id = newOrderId,
                    CarModel = _viewModel.CurrentOrder.CarModel,
                    CarNumber = _viewModel.CurrentOrder.CarNumber,
                    CarBodyType = GetCategoryName(_viewModel.SelectedBodyTypeCategory),
                    BodyTypeCategory = _viewModel.SelectedBodyTypeCategory,
                    Time = _viewModel.CurrentOrder.Time,
                    BoxNumber = _viewModel.CurrentOrder.BoxNumber,
                    WasherId = _currentShift.EmployeeIds.FirstOrDefault(),
                    ServiceIds = serviceIds,
                    ExtraCost = _viewModel.ExtraCost,
                    ExtraCostReason = _viewModel.CurrentOrder.ExtraCostReason,
                    Status = "В ожидании",
                    PaymentMethod = _viewModel.CurrentOrder.PaymentMethod,
                    IsAppointment = false,
                    ShiftId = _currentShift.Id,
                    TotalPrice = _viewModel.ServicesTotal
                };

                System.Diagnostics.Debug.WriteLine($"Создан заказ ID: {order.Id}");

                // Обновляем запись
                appointment.IsCompleted = true;
                appointment.OrderId = order.Id;

                System.Diagnostics.Debug.WriteLine($"Обновлена запись: IsCompleted={appointment.IsCompleted}, OrderId={appointment.OrderId}");

                // Добавляем заказ в смену
                var shift = appData.Shifts.FirstOrDefault(s => s.Id == _currentShift.Id);
                if (shift != null)
                {
                    if (shift.Orders == null) shift.Orders = new List<CarWashOrder>();
                    shift.Orders.Add(order);
                    System.Diagnostics.Debug.WriteLine($"Заказ добавлен в смену {shift.Id}, всего заказов: {shift.Orders.Count}");
                }

                // Сохраняем изменения
                FileDataService.SaveData(appData);

                // Проверяем сохранение
                var checkAppData = FileDataService.LoadData();
                var checkAppointment = checkAppData.Appointments.FirstOrDefault(a => a.Id == appointment.Id);
                var checkOrder = checkAppData.Shifts.SelectMany(s => s.Orders ?? new List<CarWashOrder>()).FirstOrDefault(o => o.Id == order.Id);

                System.Diagnostics.Debug.WriteLine($"=== ПОСЛЕ СОХРАНЕНИЯ ===");
                System.Diagnostics.Debug.WriteLine($"Запись найдена: {checkAppointment != null}");
                System.Diagnostics.Debug.WriteLine($"Запись IsCompleted: {checkAppointment?.IsCompleted}");
                System.Diagnostics.Debug.WriteLine($"Запись OrderId: {checkAppointment?.OrderId}");
                System.Diagnostics.Debug.WriteLine($"Заказ найден: {checkOrder != null}");
                System.Diagnostics.Debug.WriteLine($"Заказ ID: {checkOrder?.Id}");

                // Обновляем ViewModel
                _viewModel.CurrentOrder.Id = order.Id;
                _viewModel.CurrentOrder.IsAppointment = false;

                MessageBox.Show($"✅ Заказ успешно создан из записи!\n\n" +
                    $"🚗 {order.CarModel} ({order.CarNumber})\n" +
                    $"🚘 Категория кузова: {order.CarBodyType}\n" +
                    $"📅 {order.Time:dd.MM.yyyy HH:mm}\n" +
                    $"👤 Мойщик: {GetWasherName(order.WasherId)}\n" +
                    $"💰 Услуги: {order.TotalPrice:N0} ₽\n" +
                    $"➕ Дополнительно: {order.ExtraCost:N0} ₽\n" +
                    $"💵 Итоговая сумма: {order.FinalPrice:N0} ₽\n" +
                    $"👤 Мойщику (35%): {order.WasherEarnings:N0} ₽\n" +
                    $"🏢 Компании (65%): {order.CompanyEarnings:N0} ₽",
                    "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при преобразовании: {ex}");
                MessageBox.Show($"Ошибка при преобразовании: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetCategoryName(int categoryId)
        {
            switch (categoryId)
            {
                case 1:
                    return "Категория 1 (Легковая)";
                case 2:
                    return "Категория 2 (Универсал)";
                case 3:
                    return "Категория 3 (Кроссовер)";
                case 4:
                    return "Категория 4 (Внедорожник)";
                default:
                    return "Категория 1 (Легковая)";
            }
        }

        private string GetWasherName(int washerId)
        {
            var washer = _dataService.GetAllUsers().FirstOrDefault(u => u.Id == washerId);
            return washer?.FullName ?? "Не назначен";
        }

        private int GetNextOrderId(AppData appData)
        {
            int maxId = 0;
            foreach (var shift in appData.Shifts)
            {
                if (shift.Orders != null && shift.Orders.Any())
                {
                    var maxInShift = shift.Orders.Max(o => o.Id);
                    if (maxInShift > maxId) maxId = maxInShift;
                }
            }
            return maxId + 1;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
