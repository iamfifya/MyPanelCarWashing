// AddEditOrderWindow.xaml.cs
using Microsoft.VisualBasic;
using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
using MyPanelCarWashing.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace MyPanelCarWashing
{
    public partial class AddEditOrderWindow : Window
    {
        private readonly SqliteDataService _SqliteDataService;
        private readonly Shift _currentShift;
        private readonly AddEditOrderViewModel _viewModel;

        public AddEditOrderWindow(
    SqliteDataService SqliteDataService,
    AddEditOrderViewModel viewModel,
    Shift currentShift = null,
    CarWashOrder order = null)
        {
            InitializeComponent();
            _SqliteDataService = SqliteDataService;
            _currentShift = currentShift;
            ClientComboBox.SelectionChanged += ClientComboBox_SelectionChanged;


            viewModel.Initialize(currentShift, order);
            _viewModel = viewModel;
            _viewModel.LoadServices();
            DataContext = _viewModel;
            _viewModel.Recalculate();



            // Получаем список всех пользователей
            var allUsers = _SqliteDataService.GetAllUsers();

            // Устанавливаем ItemsSource для ComboBox мойщиков
            WasherComboBox.ItemsSource = allUsers;
            _viewModel.Washers = allUsers;

            // ========== ЗАГРУЖАЕМ КЛИЕНТОВ ==========
            LoadClients();

            // ОТЛАДКА
            System.Diagnostics.Debug.WriteLine($"=== КОНСТРУКТОР AddEditOrderWindow ===");
            System.Diagnostics.Debug.WriteLine($"order != null: {order != null}");
            if (order != null)
            {
                System.Diagnostics.Debug.WriteLine($"order.Id: {order.Id}");
                System.Diagnostics.Debug.WriteLine($"order.WasherId: {order.WasherId}");
                System.Diagnostics.Debug.WriteLine($"_viewModel.IsEditMode: {_viewModel.IsEditMode}");
            }
            System.Diagnostics.Debug.WriteLine($"allUsers count: {allUsers.Count}");
            foreach (var u in allUsers)
            {
                System.Diagnostics.Debug.WriteLine($"  User: Id={u.Id}, Name={u.FullName}");
            }

            // ========== ВЫБОР МОЙЩИКА - ПРЯМАЯ УСТАНОВКА ==========
            if (order != null && !_viewModel.IsAppointment && order.WasherId > 0)
            {
                var savedWasher = allUsers.FirstOrDefault(u => u.Id == order.WasherId);
                System.Diagnostics.Debug.WriteLine($"savedWasher found: {savedWasher != null}");
                if (savedWasher != null)
                {
                    // ПРЯМАЯ УСТАНОВКА СРАЗУ
                    WasherComboBox.SelectedValue = savedWasher.Id;
                    _viewModel.CurrentOrder.WasherId = savedWasher.Id;
                    System.Diagnostics.Debug.WriteLine($"Сразу установлен WasherId: {_viewModel.CurrentOrder.WasherId}");

                    // Также через Dispatcher на всякий случай
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        WasherComboBox.SelectedValue = savedWasher.Id;
                        System.Diagnostics.Debug.WriteLine($"Dispatcher: установлен WasherId: {savedWasher.Id}");
                    }), DispatcherPriority.Loaded);
                }
            }
            else if (order == null && allUsers.Any())
            {
                // Новый заказ - выбираем первого
                var firstWasher = allUsers.FirstOrDefault();
                if (firstWasher != null)
                {
                    WasherComboBox.SelectedValue = firstWasher.Id;
                    _viewModel.CurrentOrder.WasherId = firstWasher.Id;
                }
            }

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
        new KeyValuePair<string, string>("📱 Перевод", "Перевод"),
        new KeyValuePair<string, string>("📱 QR-код", "QR-код")
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
            // Для записей - загружаем длительность
            if (_viewModel.IsAppointment && order != null)
            {
                var appointment = _SqliteDataService.GetAppointmentById(order.AppointmentId.Value);
                if (appointment != null)
                {
                    DurationTextBox.Text = appointment.DurationMinutes.ToString();
                }
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
                // Получаем ID выбранных элементов
                var selectedIds = new HashSet<int>(
                    ServicesListBox.SelectedItems.Cast<ServiceViewModel>().Select(sv => sv.Id)
                );

                // Обновляем IsSelected для всех услуг по ID
                foreach (var service in _viewModel.Services)
                {
                    service.IsSelected = selectedIds.Contains(service.Id);
                }

                _viewModel.SyncServiceIds();
                _viewModel.Recalculate();
            };

            // Изначально выбираем услуги
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_viewModel.Services != null)
                {
                    foreach (var service in _viewModel.Services.Where(s => s.IsSelected))
                    {
                        var listBoxItem = ServicesListBox.ItemContainerGenerator
                            .ContainerFromItem(service) as ListBoxItem;
                        if (listBoxItem != null)
                        {
                            listBoxItem.IsSelected = true;
                        }
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // ========== 1. ОБНОВЛЯЕМ ВРЕМЯ ИЗ UI ==========
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

                // ========== 2. ДЛЯ ЗАПИСЕЙ - СОХРАНЯЕМ ДЛИТЕЛЬНОСТЬ ==========
                if (_viewModel.IsAppointment)
                {
                    if (!int.TryParse(DurationTextBox.Text, out int duration) || duration < 15)
                    {
                        MessageBox.Show("Введите корректную длительность (минимум 15 минут)", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var appointment = _SqliteDataService.GetAppointmentById(_viewModel.CurrentOrder.AppointmentId.Value);
                    if (appointment != null)
                    {
                        appointment.DurationMinutes = duration;
                    }
                }

                // ========== 3. ДЛЯ ЗАКАЗОВ - СОХРАНЯЕМ СПЕЦИФИЧЕСКИЕ ПОЛЯ ==========
                if (!_viewModel.IsAppointment)
                {
                    // Обновляем статус
                    if (_viewModel.IsEditMode && StatusComboBox.SelectedValue != null)
                    {
                        _viewModel.CurrentOrder.Status = StatusComboBox.SelectedValue.ToString();
                    }

                    // Обновляем способ оплаты
                    if (PaymentMethodComboBox.SelectedValue != null)
                    {
                        _viewModel.CurrentOrder.PaymentMethod = PaymentMethodComboBox.SelectedValue.ToString();
                    }

                    // Обновляем мойщика
                    if (WasherComboBox.SelectedItem is User selectedWasher)
                    {
                        _viewModel.CurrentOrder.WasherId = selectedWasher.Id;

                        if (_currentShift != null && !_currentShift.EmployeeIds.Contains(selectedWasher.Id))
                        {
                            MessageBox.Show($"Внимание: Мойщик \"{selectedWasher.FullName}\" не работает в текущей смене.\n\n" +
                                "Заказ будет сохранён, но статистика смены может быть некорректной.",
                                "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Выберите мойщика!", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Обновляем клиента
                    if (ClientComboBox.SelectedItem is Client selectedClient)
                    {
                        _viewModel.CurrentOrder.ClientId = selectedClient.Id;
                    }
                }

                // ========== 4. ОБНОВЛЯЕМ ОБЩИЕ ПОЛЯ ==========
                // Обновляем категорию кузова
                if (BodyTypeComboBox.SelectedValue != null && int.TryParse(BodyTypeComboBox.SelectedValue.ToString(), out int category))
                {
                    _viewModel.SelectedBodyTypeCategory = category;
                }
                _viewModel.CurrentOrder.DiscountPercent = _viewModel.DiscountPercent;
                _viewModel.CurrentOrder.DiscountAmount = _viewModel.DiscountAmount;
                _viewModel.CurrentOrder.OriginalTotalPrice = _viewModel.ServicesTotal;
                _viewModel.CurrentOrder.ExtraCost = _viewModel.ExtraCost;
                _viewModel.CurrentOrder.ExtraCostReason = _viewModel.CurrentOrder.ExtraCostReason;

                // ========== 5. СОХРАНЯЕМ ==========
                _viewModel.SaveOrder(out bool success, out string message);

                if (success)
                {
                    SqliteDataService.NotifyDataChanged();

                    string successMessage = _viewModel.IsAppointment
                        ? $"✅ Запись обновлена!\n\n🚗 {_viewModel.CurrentOrder.CarModel} ({_viewModel.CurrentOrder.CarNumber})\n" +
                          $"📅 {_viewModel.CurrentOrder.Time:dd.MM.yyyy HH:mm}\n" +
                          $"⏱️ Длительность: {DurationTextBox.Text} мин\n" +
                          $"💰 Итого: {_viewModel.FinalTotal:N0} ₽"
                        : $"✅ Заказ сохранен!\n\n🚗 {_viewModel.CurrentOrder.CarModel} ({_viewModel.CurrentOrder.CarNumber})\n" +
                          $"💰 Итого: {_viewModel.FinalTotal:N0} ₽";

                    MessageBox.Show(successMessage, "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);

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

                // === АВТО-ПОДСТАНОВКА СКИДКИ КЛИЕНТА ===
                // Применяем ТОЛЬКО если пользователь ещё не менял скидку вручную
                // (т.е. оба поля скидки равны 0)
                if (selectedClient.DefaultDiscountPercent > 0 &&
                    _viewModel.DiscountPercent == 0 &&
                    _viewModel.DiscountAmount == 0)
                {
                    _viewModel.DiscountPercent = selectedClient.DefaultDiscountPercent;
                    System.Diagnostics.Debug.WriteLine($"[AUTO] Применена скидка клиента: {selectedClient.DefaultDiscountPercent}%");
                }
                // === КОНЕЦ АВТО-ПОДСТАНОВКИ ===

                // Авто-заполнение авто, если поля пустые
                if (string.IsNullOrWhiteSpace(_viewModel.CurrentOrder.CarModel))
                {
                    var temp = _viewModel.CurrentOrder;
                    temp.CarModel = selectedClient.CarModel;
                    _viewModel.CurrentOrder = null;
                    _viewModel.CurrentOrder = temp;
                }

                if (string.IsNullOrWhiteSpace(_viewModel.CurrentOrder.CarNumber))
                {
                    var temp = _viewModel.CurrentOrder;
                    temp.CarNumber = selectedClient.CarNumber;
                    _viewModel.CurrentOrder = null;
                    _viewModel.CurrentOrder = temp;
                }
            }
        }

        private async void ConvertToOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_viewModel.IsAppointment) return;

            // Получаем выбранного мойщика
            User selectedWasher = null;

            // Сначала пробуем получить из ComboBox (если он видим)
            if (WasherComboBox.Visibility == Visibility.Visible && WasherComboBox.SelectedItem is User washerFromCombo)
            {
                selectedWasher = washerFromCombo;
            }
            else if (_viewModel.CurrentOrder.WasherId > 0)
            {
                selectedWasher = _SqliteDataService.GetAllUsers().FirstOrDefault(u => u.Id == _viewModel.CurrentOrder.WasherId);
            }

            // Если мойщик не выбран, показываем диалог
            if (selectedWasher == null)
            {
                var washers = _SqliteDataService.GetAllUsers();
                if (!washers.Any())
                {
                    MessageBox.Show("Нет доступных мойщиков!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var dialog = new WasherSelectionDialog(washers);
                dialog.Owner = this;

                if (dialog.ShowDialog() == true && dialog.SelectedWasher != null)
                {
                    selectedWasher = dialog.SelectedWasher;
                }
                else
                {
                    return; // Пользователь отменил выбор
                }
            }

            // Предупреждение, если мойщик не в смене
            if (_currentShift == null || !_currentShift.EmployeeIds.Contains(selectedWasher.Id))
            {
                var shiftWarning = MessageBox.Show($"Мойщик \"{selectedWasher.FullName}\" не работает в текущей смене!\n\n" +
                    "Вы уверены, что хотите назначить его на этот заказ?\n\n" +
                    "Он не будет учтён в статистике смены, но заказ будет создан.",
                    "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (shiftWarning != MessageBoxResult.Yes) return;
            }

            // Основное подтверждение
            var confirmResult = MessageBox.Show($"Преобразовать запись в заказ?\n\n" +
                $"🚗 {_viewModel.CurrentOrder.CarModel} ({_viewModel.CurrentOrder.CarNumber})\n" +
                $"🚘 Категория кузова: {GetCategoryName(_viewModel.SelectedBodyTypeCategory)}\n" +
                $"👤 Мойщик: {selectedWasher.FullName}\n" +
                $"👤 Клиент: {GetClientName(_viewModel.CurrentOrder.ClientId)}\n" +
                $"📅 {_viewModel.CurrentOrder.Time:dd.MM.yyyy HH:mm}\n" +
                $"💰 Сумма: {_viewModel.FinalTotal:N0} ₽\n" +
                $"💳 Оплата: {_viewModel.CurrentOrder.PaymentMethod}\n\n" +
                $"После преобразования запись будет считаться выполненной, а заказ можно будет редактировать.",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirmResult != MessageBoxResult.Yes) return;

            if (_currentShift == null || _currentShift.IsClosed)
            {
                MessageBox.Show("Нет активной смены! Сначала начните смену.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Проверяем, не создан ли уже заказ для этой записи
                var existingOrder = _SqliteDataService.GetOrderByAppointmentId(_viewModel.CurrentOrder.AppointmentId.Value);
                if (existingOrder != null)
                {
                    MessageBox.Show($"Для этой записи уже создан заказ #{existingOrder.Id}.\n\n" +
                        "Нельзя создать повторный заказ.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

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

                // Получаем запись
                var appointment = _SqliteDataService.GetAppointmentById(_viewModel.CurrentOrder.AppointmentId.Value);
                if (appointment == null)
                {
                    MessageBox.Show("Запись не найдена!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Создаём новый заказ
                var newOrder = new CarWashOrder
                {
                    Id = 0,
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
                    ShiftId = _currentShift != null ? _currentShift.Id : 0
                };

                // Сохраняем заказ в БД
                _SqliteDataService.AddOrder(newOrder, serviceIds);

                // Отмечаем запись как выполненную
                appointment.IsCompleted = true;
                appointment.OrderId = newOrder.Id;
                _SqliteDataService.UpdateAppointment(appointment);

                _viewModel.CurrentOrder.Id = newOrder.Id;
                _viewModel.CurrentOrder.IsAppointment = false;

                string clientName = GetClientName(newOrder.ClientId);

                var allServices = _SqliteDataService.GetAllServices();
                var calc = OrderMath.Calculate(newOrder, allServices);

                MessageBox.Show($"✅ Заказ успешно создан из записи!\n\n" +
                    $"🚗 {newOrder.CarModel} ({newOrder.CarNumber})\n" +
                    $"🚘 Категория кузова: {newOrder.CarBodyType}\n" +
                    $"👤 Мойщик: {selectedWasher.FullName}\n" +
                    $"👤 Клиент: {clientName}\n" +
                    $"📅 {newOrder.Time:dd.MM.yyyy HH:mm}\n" +
                    $"💰 Услуги: {newOrder.TotalPrice:N0} ₽\n" +
                    $"➕ Дополнительно: {newOrder.ExtraCost:N0} ₽\n" +
                    $"💵 Итоговая сумма: {newOrder.FinalPrice:N0} ₽\n" +
                    $"💳 Способ оплаты: {newOrder.PaymentMethod}\n" +
                    $"👤 Мойщику (35%): {calc.WasherEarnings:N0} ₽\n" +
                    $"🏢 Компании (65%): {calc.CompanyEarnings:N0} ₽\n\n" +
                    $"📝 Примечания: {(string.IsNullOrEmpty(newOrder.Notes) ? "нет" : newOrder.Notes)}",
                    "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);

                SqliteDataService.NotifyDataChanged();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при преобразовании: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MarkAsCompletedButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedBodyTypeCategory == 0) return;

            var result = MessageBox.Show("Отметить запись как выполненную без создания заказа?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            // Находим запись (нужно сохранить ID при создании)
            // Либо передавать ID через свойство
        }

        private string GetClientName(int? clientId)
        {
            if (!clientId.HasValue) return "Не указан";
            var client = _SqliteDataService.GetClientById(clientId.Value);
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

        private void LoadClients()
        {
            var clients = _SqliteDataService.GetAllClients();
            System.Diagnostics.Debug.WriteLine($"LoadClients: загружено {clients.Count} клиентов");
            ClientComboBox.ItemsSource = clients;

            if (_viewModel.CurrentOrder.ClientId.HasValue)
            {
                var client = clients.FirstOrDefault(c => c.Id == _viewModel.CurrentOrder.ClientId.Value);
                if (client != null)
                {
                    ClientComboBox.SelectedItem = client;
                    System.Diagnostics.Debug.WriteLine($"LoadClients: выбран клиент {client.FullName}");
                }
            }
        }

        private void AddNewClient_Click(object sender, RoutedEventArgs e)
        {
            var addClientWin = new AddEditClientWindow(_SqliteDataService, null);
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
