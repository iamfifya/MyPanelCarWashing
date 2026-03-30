// AddEditOrderWindow.xaml.cs
using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
using MyPanelCarWashing.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace MyPanelCarWashing
{
    public partial class AddEditOrderWindow : Window
    {
        private readonly DataService _dataService;
        private readonly Shift _currentShift;
        private readonly AddEditOrderViewModel _viewModel;

        public AddEditOrderWindow(
            DataService dataService,
            AddEditOrderViewModel viewModel,
            Shift currentShift = null,
            CarWashOrder order = null)
        {
            InitializeComponent();
            _dataService = dataService;
            _currentShift = currentShift;
            ClientComboBox.SelectionChanged += ClientComboBox_SelectionChanged;

            viewModel.Initialize(currentShift, order);
            _viewModel = viewModel;
            DataContext = _viewModel;

            // Загружаем ВСЕХ сотрудников для выбора мойщика
            var allUsers = _dataService.GetAllUsers();
            WasherComboBox.ItemsSource = allUsers;

            // ========== ЗАПОЛНЯЕМ КАТЕГОРИИ КУЗОВА ==========
            var bodyTypes = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Категория 1 (Легковая)", "1"),
                new KeyValuePair<string, string>("Категория 2 (Универсал)", "2"),
                new KeyValuePair<string, string>("Категория 3 (Кроссовер)", "3"),
                new KeyValuePair<string, string>("Категория 4 (Внедорожник)", "4")
            };
            BodyTypeComboBox.ItemsSource = bodyTypes;
            BodyTypeComboBox.DisplayMemberPath = "Key";
            BodyTypeComboBox.SelectedValuePath = "Value";

            // ========== ЗАПОЛНЯЕМ СТАТУСЫ ЗАКАЗА ==========
            var statuses = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("🟢 Выполняется", "Выполняется"),
                new KeyValuePair<string, string>("✅ Выполнен", "Выполнен"),
                new KeyValuePair<string, string>("❌ Отменен", "Отменен")
            };
            StatusComboBox.ItemsSource = statuses;
            StatusComboBox.DisplayMemberPath = "Key";
            StatusComboBox.SelectedValuePath = "Value";

            // ========== ЗАПОЛНЯЕМ СПОСОБЫ ОПЛАТЫ ==========
            var payments = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("💵 Наличные", "Наличные"),
                new KeyValuePair<string, string>("💳 Карта", "Карта"),
                new KeyValuePair<string, string>("📱 Перевод", "Перевод")
            };
            PaymentMethodComboBox.ItemsSource = payments;
            PaymentMethodComboBox.DisplayMemberPath = "Key";
            PaymentMethodComboBox.SelectedValuePath = "Value";

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
                StatusComboBox.SelectedValue = order.Status;
            }

            // Выбираем способ оплаты
            if (order != null && !string.IsNullOrEmpty(order.PaymentMethod))
            {
                PaymentMethodComboBox.SelectedValue = order.PaymentMethod;
            }

            // Выбираем категорию кузова
            if (order != null && order.BodyTypeCategory > 0)
            {
                BodyTypeComboBox.SelectedValue = order.BodyTypeCategory.ToString();
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
                WasherComboBox.SelectedItem = WasherComboBox.Items[0];
            }

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
                if (_viewModel.IsEditMode && StatusComboBox.SelectedValue != null)
                {
                    _viewModel.CurrentOrder.Status = StatusComboBox.SelectedValue.ToString();
                }

                // Обновляем способ оплаты
                if (PaymentMethodComboBox.SelectedValue != null)
                {
                    _viewModel.CurrentOrder.PaymentMethod = PaymentMethodComboBox.SelectedValue.ToString();
                }

                // Обновляем категорию кузова
                if (BodyTypeComboBox.SelectedValue != null && int.TryParse(BodyTypeComboBox.SelectedValue.ToString(), out int category))
                {
                    _viewModel.SelectedBodyTypeCategory = category;
                }

                // Обновляем мойщика
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

                _viewModel.CurrentOrder.ExtraCost = _viewModel.ExtraCost;
                _viewModel.CurrentOrder.ExtraCostReason = _viewModel.CurrentOrder.ExtraCostReason;

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

        private void ClientComboBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (ClientComboBox.SelectedItem is Client selectedClient)
            {
                _viewModel.CurrentOrder.ClientId = selectedClient.Id;

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

        private async void ConvertToOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_viewModel.IsAppointment) return;

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

                string paymentMethod = PaymentMethodComboBox.SelectedValue?.ToString() ?? "Наличные";
                string bodyTypeName = GetCategoryName(_viewModel.SelectedBodyTypeCategory);

                var order = await Task.Run(() => TransactionService.ExecuteInTransaction(appData =>
                {
                    var appointment = appData.Appointments.FirstOrDefault(a => a.Id == _viewModel.CurrentOrder.AppointmentId.Value);
                    if (appointment == null)
                        throw new Exception("Запись не найдена!");

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
                        Status = "Выполняется",
                        PaymentMethod = paymentMethod,
                        ClientId = _viewModel.CurrentOrder.ClientId,
                        Notes = appointment.Notes,
                        IsAppointment = false,
                        AppointmentId = appointment.Id,
                        ShiftId = _currentShift.Id
                    };

                    appointment.IsCompleted = true;
                    appointment.OrderId = newOrder.Id;

                    var shift = appData.Shifts.FirstOrDefault(s => s.Id == _currentShift.Id);
                    if (shift == null)
                        throw new Exception("Смена не найдена!");

                    if (shift.Orders == null)
                        shift.Orders = new List<CarWashOrder>();
                    shift.Orders.Add(newOrder);

                    if (newOrder.ClientId.HasValue)
                    {
                        var client = appData.Clients.FirstOrDefault(c => c.Id == newOrder.ClientId.Value);
                        if (client != null)
                        {
                            client.VisitsCount++;
                            client.TotalSpent += newOrder.FinalPrice;
                            client.LastVisitDate = DateTime.Now;
                        }
                    }

                    return newOrder;
                }));

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
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}\n\nДанные не были сохранены.",
                    "Ошибка транзакции", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
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
                case 1: return "Категория 1 (Легковая)";
                case 2: return "Категория 2 (Универсал)";
                case 3: return "Категория 3 (Кроссовер)";
                case 4: return "Категория 4 (Внедорожник)";
                default: return "Категория 1 (Легковая)";
            }
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
