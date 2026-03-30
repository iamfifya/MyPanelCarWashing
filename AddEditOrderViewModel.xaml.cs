// AddEditOrderWindow.xaml.cs
using DocumentFormat.OpenXml.Drawing.Charts;
using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
using MyPanelCarWashing.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;

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

            // Загружаем ВСЕХ сотрудников для выбора мойщика (не только из смены)
            var allUsers = _dataService.GetAllUsers();
            WasherComboBox.ItemsSource = allUsers;

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
            if (_viewModel.IsAppointment && WasherComboBox.Items.Count > 0)
            {
                // По умолчанию выбираем первого сотрудника (пользователь все равно должен выбрать сам)
                WasherComboBox.SelectedIndex = 0;
            }

            // Если это обычный заказ, устанавливаем сохраненного мойщика
            if (!_viewModel.IsAppointment && order != null && order.WasherId > 0)
            {
                var savedWasher = allUsers.FirstOrDefault(u => u.Id == order.WasherId);
                if (savedWasher != null)
                    WasherComboBox.SelectedItem = savedWasher;
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
                if (PaymentMethodComboBox.SelectedItem is ComboBoxItem selectedPayment)
                {
                    _viewModel.CurrentOrder.PaymentMethod = selectedPayment.Tag?.ToString() ?? "Наличные";
                }

                // Обновляем мойщика (для всех типов заказов, включая записи)
                if (WasherComboBox.SelectedItem is User selectedWasher)
                {
                    _viewModel.CurrentOrder.WasherId = selectedWasher.Id;
                }
                else
                {
                    MessageBox.Show("Выберите мойщика!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
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

            // Проверяем, выбран ли мойщик
            if (WasherComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите мойщика для выполнения заказа!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedWasher = WasherComboBox.SelectedItem as User;
            if (selectedWasher == null)
            {
                MessageBox.Show("Выберите корректного мойщика!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверяем, работает ли выбранный мойщик в текущей смене
            if (_currentShift == null || !_currentShift.EmployeeIds.Contains(selectedWasher.Id))
            {
                MessageBox.Show($"Мойщик \"{selectedWasher.FullName}\" не работает в текущей смене!\n\n" +
                    "Пожалуйста, выберите сотрудника из списка или добавьте его в смену.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Преобразовать запись в заказ?\n\n" +
                $"🚗 {_viewModel.CurrentOrder.CarModel} ({_viewModel.CurrentOrder.CarNumber})\n" +
                $"🚘 Категория кузова: {GetCategoryName(_viewModel.SelectedBodyTypeCategory)}\n" +
                $"👤 Мойщик: {selectedWasher.FullName}\n" +
                $"👤 Клиент: {GetClientName(_viewModel.CurrentOrder.ClientId)}\n" +
                $"📅 {_viewModel.CurrentOrder.Time:dd.MM.yyyy HH:mm}\n" +
                $"💰 Сумма: {_viewModel.FinalTotal:N0} ₽\n" +
                $"💳 Оплата: {_viewModel.CurrentOrder.PaymentMethod}\n\n" +
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

                // Получаем способ оплаты
                string paymentMethod = "Наличные";
                if (PaymentMethodComboBox.SelectedItem is ComboBoxItem selectedPaymentItem)
                {
                    paymentMethod = selectedPaymentItem.Tag?.ToString() ?? "Наличные";
                    if (string.IsNullOrEmpty(paymentMethod) || paymentMethod == "Наличные")
                    {
                        string content = selectedPaymentItem.Content.ToString();
                        paymentMethod = content.Replace("💵 ", "").Replace("💳 ", "").Replace("📱 ", "");
                    }
                }
                else if (!string.IsNullOrWhiteSpace(_viewModel.CurrentOrder.PaymentMethod))
                {
                    paymentMethod = _viewModel.CurrentOrder.PaymentMethod
                        .Replace("💵 ", "").Replace("💳 ", "").Replace("📱 ", "");
                }

                string bodyTypeName = GetCategoryName(_viewModel.SelectedBodyTypeCategory);

                // ИСПОЛЬЗУЕМ ТРАНЗАКЦИОННОЕ СОХРАНЕНИЕ
                var order = await Task.Run(() => TransactionService.ExecuteInTransaction(appData =>
                {
                    // Находим оригинальную запись
                    var appointment = appData.Appointments.FirstOrDefault(a => a.Id == _viewModel.CurrentOrder.AppointmentId.Value);
                    if (appointment == null)
                        throw new Exception("Запись не найдена!");

                    System.Diagnostics.Debug.WriteLine($"=== ПРЕОБРАЗОВАНИЕ ЗАПИСИ (ТРАНЗАКЦИЯ) ===");
                    System.Diagnostics.Debug.WriteLine($"Appointment ID: {appointment.Id}");
                    System.Diagnostics.Debug.WriteLine($"Выбранный мойщик: {selectedWasher.FullName} (ID: {selectedWasher.Id})");

                    // Создаем новый заказ
                    var newOrder = new CarWashOrder
                    {
                        Id = GetNextOrderId(appData),
                        CarModel = _viewModel.CurrentOrder.CarModel,
                        CarNumber = _viewModel.CurrentOrder.CarNumber,
                        CarBodyType = bodyTypeName,
                        BodyTypeCategory = _viewModel.SelectedBodyTypeCategory,
                        Time = _viewModel.CurrentOrder.Time,
                        BoxNumber = _viewModel.CurrentOrder.BoxNumber,
                        WasherId = selectedWasher.Id,
                        ServiceIds = serviceIds,
                        ExtraCost = _viewModel.ExtraCost,
                        ExtraCostReason = _viewModel.CurrentOrder.ExtraCostReason ?? appointment.ExtraCostReason,
                        TotalPrice = _viewModel.ServicesTotal,
                        Status = "В ожидании",
                        PaymentMethod = paymentMethod,
                        ClientId = _viewModel.CurrentOrder.ClientId,
                        Notes = appointment.Notes,
                        IsAppointment = false,
                        AppointmentId = appointment.Id,
                        ShiftId = _currentShift.Id
                    };

                    // Обновляем запись
                    appointment.IsCompleted = true;
                    appointment.OrderId = newOrder.Id;

                    // Добавляем заказ в смену
                    var shift = appData.Shifts.FirstOrDefault(s => s.Id == _currentShift.Id);
                    if (shift == null)
                        throw new Exception("Смена не найдена!");

                    if (shift.Orders == null)
                        shift.Orders = new List<CarWashOrder>();
                    shift.Orders.Add(newOrder);

                    // Обновляем статистику клиента
                    if (newOrder.ClientId.HasValue)
                    {
                        var client = appData.Clients.FirstOrDefault(c => c.Id == newOrder.ClientId.Value);
                        if (client != null)
                        {
                            client.VisitsCount++;
                            client.TotalSpent += newOrder.FinalPrice;
                            client.LastVisitDate = DateTime.Now;
                            System.Diagnostics.Debug.WriteLine($"Обновлена статистика клиента {client.FullName}: визитов={client.VisitsCount}, сумма={client.TotalSpent:N0} ₽");
                        }
                    }

                    return newOrder;
                }));

                // Обновляем ViewModel
                _viewModel.CurrentOrder.Id = order.Id;
                _viewModel.CurrentOrder.IsAppointment = false;

                string clientName = GetClientName(order.ClientId);

                MessageBox.Show($"✅ Заказ успешно создан из записи!\n\n" +
                    $"🚗 {order.CarModel} ({order.CarNumber})\n" +
                    $"🚘 Категория кузова: {order.CarBodyType}\n" +
                    $"👤 Мойщик: {selectedWasher.FullName}\n" +
                    $"👤 Клиент: {clientName}\n" +
                    $"📅 {order.Time:dd.MM.yyyy HH:mm}\n" +
                    $"💰 Услуги: {order.TotalPrice:N0} ₽\n" +
                    $"➕ Дополнительно: {order.ExtraCost:N0} ₽\n" +
                    $"💵 Итоговая сумма: {order.FinalPrice:N0} ₽\n" +
                    $"💳 Способ оплаты: {order.PaymentMethod}\n" +
                    $"👤 Мойщику (35%): {order.WasherEarnings:N0} ₽\n" +
                    $"🏢 Компании (65%): {order.CompanyEarnings:N0} ₽\n\n" +
                    $"📝 Примечания: {(string.IsNullOrEmpty(order.Notes) ? "нет" : order.Notes)}",
                    "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (TransactionException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Транзакция не удалась: {ex}");
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}\n\nДанные не были сохранены.",
                    "Ошибка транзакции", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (DataIntegrityException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка целостности: {ex}");
                MessageBox.Show($"Ошибка целостности данных: {ex.Message}\n\nОперация отменена.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при преобразовании: {ex}");
                MessageBox.Show($"Ошибка при преобразовании: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private string GetClientName(int? clientId)
        {
            if (!clientId.HasValue) return "Не указан";

            var client = _dataService.GetClientById(clientId.Value);
            return client?.FullName ?? $"Клиент #{clientId}";
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
        private void LoadClients()
        {
            var clients = _dataService.GetAllClients();
            ClientComboBox.ItemsSource = clients;

            if (_viewModel.CurrentOrder.ClientId.HasValue)
            {
                var client = clients.FirstOrDefault(c => c.Id == _viewModel.CurrentOrder.ClientId.Value);
                if (client != null)
                {
                    ClientComboBox.SelectedItem = client;
                }
            }
        }

        private void ClientComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClientComboBox.SelectedItem is Client selectedClient)
            {
                _viewModel.CurrentOrder.ClientId = selectedClient.Id;

                // Автозаполнение данных из карточки клиента
                if (string.IsNullOrWhiteSpace(_viewModel.CurrentOrder.CarModel))
                {
                    _viewModel.CurrentOrder.CarModel = selectedClient.CarModel;
                }
                if (string.IsNullOrWhiteSpace(_viewModel.CurrentOrder.CarNumber))
                {
                    _viewModel.CurrentOrder.CarNumber = selectedClient.CarNumber;
                }
            }
        }

        private void AddNewClient_Click(object sender, RoutedEventArgs e)
        {
            var addClientWin = new AddEditClientWindow(_dataService, null);
            if (addClientWin.ShowDialog() == true)
            {
                LoadClients();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
