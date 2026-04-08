using MyPanelCarWashing.Controls;
using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
using MyPanelCarWashing.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MyPanelCarWashing
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private SqliteDataService _SqliteDataService;
        private List<CarWashOrder> _allOrders;
        private Shift _currentShift;
        private User _currentUser;
        private string _searchFilter = "";
        private List<WasherStat> _washersStats;
        private decimal _companyEarnings;
        private decimal _totalRevenue;

        public string ActiveUserInfo
        {
            get
            {
                if (_currentUser == null) return "Гость";

                string role = _currentUser.IsAdmin ? "👑 Админ" : "👤 Сотрудник";
                return $"{_currentUser.FullName} • {role}";
            }
        }
        public string CurrentShiftInfo { get; private set; }
        public string TotalOrdersInfo { get; private set; }

        // Добавляем свойство для выбранного элемента
        private OrderDisplayItem _selectedItem;
        public OrderDisplayItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                // Сбрасываем выделение у предыдущего элемента
                if (_selectedItem != null)
                {
                    _selectedItem.IsSelected = false;
                }

                _selectedItem = value;

                // Устанавливаем выделение новому элементу
                if (_selectedItem != null)
                {
                    _selectedItem.IsSelected = true;
                }

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedItem)));

                // Принудительно обновляем UI
                RefreshItems();
            }
        }
        private int _cashCount;
        private decimal _cashAmount;
        private int _cardCount;
        private decimal _cardAmount;
        private int _transferCount;
        private decimal _transferAmount;
        private int _qrCount;
        private decimal _qrAmount;

        public int QrCount
        {
            get => _qrCount;
            set
            {
                _qrCount = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(QrCount)));
            }
        }

        public decimal QrAmount
        {
            get => _qrAmount;
            set
            {
                _qrAmount = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(QrAmount)));
            }
        }

        public int CashCount
        {
            get => _cashCount;
            set
            {
                _cashCount = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CashCount)));
            }
        }

        public decimal CashAmount
        {
            get => _cashAmount;
            set
            {
                _cashAmount = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CashAmount)));
            }
        }

        public int CardCount
        {
            get => _cardCount;
            set
            {
                _cardCount = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CardCount)));
            }
        }

        public decimal CardAmount
        {
            get => _cardAmount;
            set
            {
                _cardAmount = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CardAmount)));
            }
        }

        public int TransferCount
        {
            get => _transferCount;
            set
            {
                _transferCount = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TransferCount)));
            }
        }

        public decimal TransferAmount
        {
            get => _transferAmount;
            set
            {
                _transferAmount = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TransferAmount)));
            }
        }
        private void UpdatePaymentStats()
        {
            if (_allOrders == null || !_allOrders.Any())
            {
                CashCount = 0;
                CashAmount = 0;
                CardCount = 0;
                CardAmount = 0;
                TransferCount = 0;
                TransferAmount = 0;
                QrCount = 0;
                QrAmount = 0;
                return;
            }

            CashCount = _allOrders.Count(o => o.PaymentMethod == "Наличные");
            CashAmount = _allOrders.Where(o => o.PaymentMethod == "Наличные").Sum(o => o.FinalPrice);

            CardCount = _allOrders.Count(o => o.PaymentMethod == "Карта");
            CardAmount = _allOrders.Where(o => o.PaymentMethod == "Карта").Sum(o => o.FinalPrice);

            TransferCount = _allOrders.Count(o => o.PaymentMethod == "Перевод");
            TransferAmount = _allOrders.Where(o => o.PaymentMethod == "Перевод").Sum(o => o.FinalPrice);

            QrCount = _allOrders.Count(o => o.PaymentMethod == "QR-код");
            QrAmount = _allOrders.Where(o => o.PaymentMethod == "QR-код").Sum(o => o.FinalPrice);
        }

        private void RefreshItems()
        {
            // Обновляем списки, чтобы UI перерисовался
            var temp1 = Box1Items;
            Box1Items = null;
            Box1Items = temp1;

            var temp2 = Box2Items;
            Box2Items = null;
            Box2Items = temp2;

            var temp3 = Box3Items;
            Box3Items = null;
            Box3Items = temp3;
        }
        public void RefreshData()
        {
            // Полностью перезагружаем данные
            _SqliteDataService = new SqliteDataService();
            LoadData();
        }

        private List<OrderDisplayItem> _box1Items;
        private List<OrderDisplayItem> _box2Items;
        private List<OrderDisplayItem> _box3Items;

        public List<OrderDisplayItem> Box1Items
        {
            get => _box1Items;
            set
            {
                _box1Items = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Box1Items)));
            }
        }

        public List<OrderDisplayItem> Box2Items
        {
            get => _box2Items;
            set
            {
                _box2Items = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Box2Items)));
            }
        }

        public List<OrderDisplayItem> Box3Items
        {
            get => _box3Items;
            set
            {
                _box3Items = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Box3Items)));
            }
        }
        private void LoadData()
        {
            try
            {
                Logger.Info("Загрузка данных в MainWindow", "UI_MAIN");

                _currentShift = _SqliteDataService.GetCurrentOpenShift();
                Logger.Info("Текущая смена получена", "UI_MAIN", $"Найдена: {_currentShift != null} | ID: {_currentShift?.Id}");

                if (_currentShift != null && !_currentShift.IsClosed)
                {
                    _allOrders = _currentShift.Orders ?? new List<CarWashOrder>();
                    Logger.Info("Заказы загружены", "UI_MAIN", $"Количество: {_allOrders.Count}");
                }
                else
                {
                    _allOrders = new List<CarWashOrder>();
                    Logger.Warn("Нет активной смены или смена закрыта", "UI_MAIN");
                }

                var todayAppointments = new List<Appointment>();
                if (_currentShift != null && !_currentShift.IsClosed)
                {
                    todayAppointments = _SqliteDataService.GetAppointmentsByDate(DateTime.Now);
                    Logger.Info("Записи на сегодня загружены", "UI_MAIN", $"Количество: {todayAppointments.Count}");
                }

                var allServices = _SqliteDataService.GetAllServices();
                Logger.Info("Услуги загружены", "UI_MAIN", $"Количество: {allServices.Count}");

                // ===== ПОЛНЫЙ МАППИНГ ЗАКАЗОВ (без сокращений) =====
                var orderItems = _allOrders.Select(o => new OrderDisplayItem
                {
                    Id = o.Id,
                    CarModel = o.CarModel,
                    CarNumber = o.CarNumber,
                    Time = o.Time,
                    WasherName = GetWasherName(o.WasherId),
                    ServicesList = string.Join(", ", (o.ServiceIds ?? new List<int>()).Select(id =>
                    {
                        var svc = allServices.FirstOrDefault(s => s.Id == id);
                        return svc != null ? svc.Name : "Unknown";
                    })),
                    FinalPrice = o.FinalPrice,
                    OriginalTotalPrice = o.OriginalTotalPrice,
                    DiscountPercent = o.DiscountPercent,
                    DiscountAmount = o.DiscountAmount,
                    ExtraCost = o.ExtraCost,
                    ExtraCostReason = o.ExtraCostReason,
                    BoxNumber = o.BoxNumber,
                    Status = o.Status,
                    PaymentMethod = o.PaymentMethod,
                    IsAppointment = false,
                    IsCompleted = false,
                    AppointmentId = null
                }).ToList();

                // ===== ПОЛНЫЙ МАППИНГ ЗАПИСЕЙ =====
                var appointmentItems = todayAppointments
                    .Where(a => !a.IsCompleted)
                    .Select(a => new OrderDisplayItem
                    {
                        Id = 0,
                        CarModel = a.CarModel,
                        CarNumber = a.CarNumber,
                        Time = a.AppointmentDate,
                        WasherName = "📅 Запись",
                        ServicesList = string.Join(", ", (a.ServiceIds ?? new List<int>()).Select(id =>
                        {
                            var svc = allServices.FirstOrDefault(s => s.Id == id);
                            return svc != null ? svc.Name : "Unknown";
                        })),
                        FinalPrice = (a.ServiceIds ?? new List<int>()).Sum(id =>
                        {
                            var svc = allServices.FirstOrDefault(s => s.Id == id);
                            return svc != null ? svc.GetPrice(a.BodyTypeCategory) : 0;
                        }) + a.ExtraCost,
                        OriginalTotalPrice = 0,
                        DiscountPercent = 0,
                        DiscountAmount = 0,
                        ExtraCost = a.ExtraCost,
                        ExtraCostReason = a.ExtraCostReason,
                        BoxNumber = a.BoxNumber,
                        Status = "Предварительная запись",
                        IsAppointment = true,
                        IsCompleted = a.IsCompleted,
                        AppointmentId = a.Id
                    }).ToList();

                var allItems = orderItems.Concat(appointmentItems).OrderBy(i => i.Time).ToList();
                Logger.Info("Карточки сформированы", "UI_MAIN", $"Всего: {allItems.Count} | Заказов: {orderItems.Count} | Записей: {appointmentItems.Count}");

                // Распределяем по боксам
                Box1Items = allItems.Where(i => i.BoxNumber == 1).ToList();
                Box2Items = allItems.Where(i => i.BoxNumber == 2).ToList();
                Box3Items = allItems.Where(i => i.BoxNumber == 3).ToList();

                UpdateInfo();

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Box1Items)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Box2Items)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Box3Items)));

                Logger.Info("Интерфейс MainWindow успешно обновлён", "UI_MAIN");
            }
            catch (Exception ex)
            {
                Logger.Error("Критическая ошибка при загрузке данных в MainWindow", ex, "UI_MAIN");
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnDataChanged()
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    Logger.Info("Получено уведомление об изменении данных", "UI_SYNC");
                    _SqliteDataService = new SqliteDataService();
                    _currentShift = _SqliteDataService.GetCurrentOpenShift();

                    if (AppointmentsOverlay != null)
                    {
                        AppointmentsOverlay.SqliteDataService = _SqliteDataService;
                        AppointmentsOverlay.CurrentShift = _currentShift;
                    }

                    LoadData();
                }
                catch (Exception ex)
                {
                    Logger.Error("Ошибка при синхронизации данных (OnDataChanged)", ex, "UI_SYNC");
                }
            });
        }

        public List<WasherStat> WashersStats
        {
            get => _washersStats;
            set
            {
                _washersStats = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WashersStats)));
            }
        }

        public decimal CompanyEarnings
        {
            get => _companyEarnings;
            set
            {
                _companyEarnings = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CompanyEarnings)));
            }
        }

        public decimal TotalRevenue
        {
            get => _totalRevenue;
            set
            {
                _totalRevenue = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalRevenue)));
            }
        }

        public MainWindow(SqliteDataService SqliteDataService, User user)
        {
            InitializeComponent();
            _SqliteDataService = SqliteDataService;
            _currentUser = user;
            DataContext = this;

            // Устанавливаем SqliteDataService для оверлея
            if (AppointmentsOverlay != null)
            {
                if (AppointmentsOverlay != null)
                {
                    AppointmentsOverlay.SqliteDataService = SqliteDataService;
                    AppointmentsOverlay.CurrentShift = _currentShift;
                }
                AppointmentsOverlay.SqliteDataService = SqliteDataService;
                System.Diagnostics.Debug.WriteLine("SqliteDataService set to AppointmentsOverlay");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("AppointmentsOverlay is NULL in constructor!");
            }

            // Подписываемся на глобальное событие изменения данных
            SqliteDataService.DataChanged += OnDataChanged;

            LoadData();
        }

        // Метод для установки пользователя (используется если MainWindow создается через DI)
        public void SetUser(User user)
        {
            _currentUser = user;
            LoadData();
        }

        private void ApplyFilterAndDisplay()
        {
            var allServices = _SqliteDataService.GetAllServices();
            var allDisplayItems = new List<OrderDisplayItem>();

            // Добавляем заказы
            if (_allOrders != null && _allOrders.Any())
            {
                var filteredOrders = _allOrders.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(_searchFilter))
                {
                    filteredOrders = filteredOrders.Where(o =>
                        o.CarNumber.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        o.CarModel.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0);
                }

                var orderItems = filteredOrders.Select(o => new OrderDisplayItem
                {
                    Id = o.Id,
                    CarModel = o.CarModel,
                    CarNumber = o.CarNumber,
                    Time = o.Time,
                    WasherName = GetWasherName(o.WasherId),
                    ServicesList = string.Join(", ", o.ServiceIds.Select(id => allServices.FirstOrDefault(s => s.Id == id)?.Name ?? "Unknown")),
                    FinalPrice = o.FinalPrice,
                    ExtraCost = o.ExtraCost,
                    ExtraCostReason = o.ExtraCostReason,
                    BoxNumber = o.BoxNumber,
                    Status = o.Status,
                    PaymentMethod = o.PaymentMethod,
                    IsAppointment = false,
                    IsCompleted = false,
                    AppointmentId = null
                });

                allDisplayItems.AddRange(orderItems);
            }

            // Добавляем записи на сегодня
            var todayAppointments = _SqliteDataService.GetAppointmentsByDate(DateTime.Now);
            var activeAppointments = todayAppointments.Where(a => !a.IsCompleted).ToList();

            if (activeAppointments.Any())
            {
                var filteredAppointments = activeAppointments.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(_searchFilter))
                {
                    filteredAppointments = filteredAppointments.Where(a =>
                        a.CarNumber.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        a.CarModel.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0);
                }

                var appointmentItems = filteredAppointments.Select(a => new OrderDisplayItem
                {
                    Id = 0,
                    CarModel = a.CarModel,
                    CarNumber = a.CarNumber,
                    Time = a.AppointmentDate,
                    WasherName = "📅 Запись",
                    ServicesList = string.Join(", ", a.ServiceIds.Select(id => allServices.FirstOrDefault(s => s.Id == id)?.Name ?? "Unknown")),
                    FinalPrice = a.ServiceIds.Sum(id => allServices.FirstOrDefault(s => s.Id == id)?.GetPrice(a.BodyTypeCategory) ?? 0) + a.ExtraCost,
                    ExtraCost = a.ExtraCost,
                    ExtraCostReason = a.ExtraCostReason,
                    BoxNumber = a.BoxNumber,
                    Status = "Предварительная запись",
                    IsAppointment = true,
                    IsCompleted = a.IsCompleted,
                    AppointmentId = a.Id
                });

                allDisplayItems.AddRange(appointmentItems);
            }

            // Сортируем по времени
            var sortedItems = allDisplayItems.OrderBy(i => i.Time).ToList();

            // Распределяем по боксам
            Box1ItemsControl.ItemsSource = sortedItems.Where(i => i.BoxNumber == 1).ToList();
            Box2ItemsControl.ItemsSource = sortedItems.Where(i => i.BoxNumber == 2).ToList();
            Box3ItemsControl.ItemsSource = sortedItems.Where(i => i.BoxNumber == 3).ToList();
        }

        private string GetWasherName(int washerId)
        {
            var washer = _SqliteDataService.GetAllUsers().FirstOrDefault(u => u.Id == washerId);
            return washer?.FullName ?? "Не назначен";
        }

        private void UpdateInfo()
        {
            if (_currentShift != null && !_currentShift.IsClosed)
            {
                CurrentShiftInfo = $"📅 Смена: {_currentShift.Date:dd.MM.yyyy} | Начало: {_currentShift.StartTime:HH:mm}";

                // Только выполненные заказы идут в выручку
                var completedOrders = _allOrders.Where(o => o.Status == "Выполнен").ToList();
                TotalRevenue = completedOrders.Sum(o => o.FinalPrice);
                var allServices = _SqliteDataService.GetAllServices();
                var totalWasherEarnings = completedOrders.Sum(o => OrderMath.Calculate(o, allServices).WasherEarnings);
                CompanyEarnings = completedOrders.Sum(o => OrderMath.Calculate(o, allServices).CompanyEarnings);

                var inProgressCount = _allOrders.Count(o => o.Status == "Выполняется");
                var cancelledCount = _allOrders.Count(o => o.Status == "Отменен");

                TotalOrdersInfo = $"🚗 Выполнено: {completedOrders.Count} | " +
                    $"🟢 В работе: {inProgressCount} | " +
                    $"❌ Отменено: {cancelledCount} | " +
                    $"👤 Мойщикам: {totalWasherEarnings:N0} ₽";
            }
            else if (_currentShift != null && _currentShift.IsClosed)
            {
                CurrentShiftInfo = $"📅 Смена закрыта: {_currentShift.Date:dd.MM.yyyy}";
                var completedOrders = _allOrders.Where(o => o.Status == "Выполнен").ToList();
                TotalRevenue = completedOrders.Sum(o => o.FinalPrice);
                TotalOrdersInfo = $"🚗 Итого за смену: {completedOrders.Count} машин | 💰 {TotalRevenue:N0} ₽";
                CompanyEarnings = 0;
            }
            else
            {
                CurrentShiftInfo = "⏰ Нет активной смены. Начните смену!";
                TotalOrdersInfo = "";
                TotalRevenue = 0;
                CompanyEarnings = 0;
            }

            UpdateWashersStats();
            UpdatePaymentStats();

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentShiftInfo)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalOrdersInfo)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WashersStats)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CompanyEarnings)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalRevenue)));

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CashCount)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CashAmount)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CardCount)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CardAmount)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TransferCount)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TransferAmount)));
        }

        private void UpdateWashersStats()
        {
            if (_currentShift == null || _currentShift.IsClosed || !_allOrders.Any())
            {
                WashersStats = new List<WasherStat>();
                return;
            }

            var allUsers = _SqliteDataService.GetAllUsers();
            var allServices = _SqliteDataService.GetAllServices();
            var completedOrders = _allOrders.Where(o => o.Status == "Выполнен").ToList();
            var totalShiftRevenue = completedOrders.Sum(o => OrderMath.Calculate(o, allServices).FinalPrice);

            var stats = completedOrders
                .Where(o => o.WasherId > 0)
                .GroupBy(o => o.WasherId)
                .Select(g =>
                {
                    var washerRevenue = g.Sum(o => OrderMath.Calculate(o, allServices).FinalPrice);
                    var washerEarnings = g.Sum(o => OrderMath.Calculate(o, allServices).WasherEarnings);

                    return new WasherStat
                    {
                        WasherName = allUsers.FirstOrDefault(u => u.Id == g.Key)?.FullName ?? "Неизвестный",
                        CarsCount = g.Count(),
                        Earnings = washerEarnings,
                        TotalRevenue = washerRevenue,
                        Percentage = totalShiftRevenue > 0 ? (washerRevenue / totalShiftRevenue) * 100m : 0m
                    };
                })
                .OrderByDescending(s => s.Earnings)
                .ToList();

            WashersStats = stats;
        }

        private void SearchFilterTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=== SearchFilterTextBox_GotFocus ===");
            if (SearchFilterTextBox.Text == "🔍 Поиск по гос. номеру или модели...")
            {
                SearchFilterTextBox.Text = "";
                SearchFilterTextBox.Foreground = Brushes.Black;
                System.Diagnostics.Debug.WriteLine("Text cleared");
            }
        }

        private void SearchFilterTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=== SearchFilterTextBox_LostFocus ===");
            if (string.IsNullOrWhiteSpace(SearchFilterTextBox.Text))
            {
                SearchFilterTextBox.Text = "🔍 Поиск по гос. номеру или модели...";
                SearchFilterTextBox.Foreground = new SolidColorBrush(Color.FromRgb(127, 140, 141));
                _searchFilter = "";
                ApplyFilterAndDisplay();
                System.Diagnostics.Debug.WriteLine("Placeholder restored");
            }
        }

        private void SearchFilterTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            _searchFilter = SearchFilterTextBox.Text.Trim();
            System.Diagnostics.Debug.WriteLine($"Search text: '{_searchFilter}'");

            if (_searchFilter == "🔍 Поиск по гос. номеру или модели...")
            {
                _searchFilter = "";
            }

            ApplyFilterAndDisplay();
        }
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentShift == null || _currentShift.IsClosed)
            {
                MessageBox.Show("Сначала начните смену!", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var orderViewModel = App.GetService<AddEditOrderViewModel>();
            var addWin = new AddEditOrderWindow(_SqliteDataService, orderViewModel, _currentShift);

            if (addWin.ShowDialog() == true)
            {
                System.Diagnostics.Debug.WriteLine("=== ЗАКАЗ ДОБАВЛЕН, ОБНОВЛЯЕМ ДАННЫЕ ===");
                _SqliteDataService = new SqliteDataService(); // Принудительно обновляем сервис
                LoadData();
            }
        }


        private void OpenEditOrder(OrderDisplayItem orderDisplay)
        {
            if (orderDisplay.IsAppointment && orderDisplay.AppointmentId.HasValue)
            {
                var appointment = _SqliteDataService.GetAppointmentById(orderDisplay.AppointmentId.Value);
                if (appointment != null)
                {
                    if (appointment.IsCompleted && appointment.OrderId.HasValue)
                    {
                        var order = _allOrders.FirstOrDefault(o => o.Id == appointment.OrderId.Value);
                        if (order != null)
                        {
                            // Переименовал переменную в editViewModel
                            var editViewModel = App.GetService<AddEditOrderViewModel>();
                            var orderEditWin = new AddEditOrderWindow(_SqliteDataService, editViewModel, _currentShift, order);
                            if (orderEditWin.ShowDialog() == true)
                            {
                                _SqliteDataService = new SqliteDataService();
                                LoadData();
                            }
                            return;
                        }
                    }

                    var tempOrder = new CarWashOrder
                    {
                        Id = 0,
                        CarModel = appointment.CarModel,
                        CarNumber = appointment.CarNumber,
                        CarBodyType = appointment.CarBodyType,
                        BodyTypeCategory = appointment.BodyTypeCategory,
                        Time = appointment.AppointmentDate,
                        BoxNumber = appointment.BoxNumber,
                        ServiceIds = appointment.ServiceIds,
                        ExtraCost = appointment.ExtraCost,
                        ExtraCostReason = appointment.ExtraCostReason,
                        Status = appointment.IsCompleted ? "Выполнен" : "Предварительная запись",
                        IsAppointment = true,
                        AppointmentId = appointment.Id
                    };

                    // Переименовал переменную в appointmentViewModel
                    var appointmentViewModel = App.GetService<AddEditOrderViewModel>();
                    var appointmentEditWin = new AddEditOrderWindow(_SqliteDataService, appointmentViewModel, _currentShift, tempOrder);
                    if (appointmentEditWin.ShowDialog() == true)
                    {
                        _SqliteDataService = new SqliteDataService();
                        LoadData();
                    }
                }
            }
            else if (!orderDisplay.IsAppointment && orderDisplay.Id > 0)
            {
                var originalOrder = _allOrders.FirstOrDefault(o => o.Id == orderDisplay.Id);
                if (originalOrder != null)
                {
                    // Переименовал переменную в orderViewModel
                    var orderViewModel = App.GetService<AddEditOrderViewModel>();
                    var orderEditWin = new AddEditOrderWindow(_SqliteDataService, orderViewModel, _currentShift, originalOrder);
                    if (orderEditWin.ShowDialog() == true)
                    {
                        _SqliteDataService = new SqliteDataService();
                        LoadData();
                    }
                }
            }
        }

        private void EmployeesButton_Click(object sender, RoutedEventArgs e)
        {
            var empWin = App.GetService<EmployeeCardWindow>();
            empWin.ShowDialog();
        }



        private void CloseShiftButton_Click(object sender, RoutedEventArgs e)
        {
            // === ВАЖНО: сохраняем данные смены ДО любых изменений ===
            int? shiftId = _currentShift?.Id;
            DateTime? shiftDate = _currentShift?.Date;

            if (_currentShift == null)
            {
                Logger.Warn("Попытка закрыть смену, но _currentShift = null", "SHIFT");
                MessageBox.Show("Нет активной смены для закрытия", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_currentShift.IsClosed)
            {
                Logger.Warn($"Попытка закрыть уже закрытую смену #{_currentShift.Id}", "SHIFT");
                MessageBox.Show("Эта смена уже закрыта", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Logger.Info($"Начало закрытия смены #{_currentShift.Id}", "SHIFT",
                $"Дата: {_currentShift.Date:dd.MM.yyyy} | Начало: {_currentShift.StartTime:HH:mm}");

            string canCloseError = _SqliteDataService.CanCloseShift(_currentShift.Id);
            if (canCloseError != null)
            {
                Logger.Warn($"Невозможно закрыть смену #{_currentShift.Id}: {canCloseError}", "SHIFT");
                MessageBox.Show(canCloseError, "Невозможно закрыть смену",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var allUsers = _SqliteDataService.GetAllUsers();
            var completedOrders = _allOrders.Where(o => o.Status == "Выполнен").ToList();
            var totalRevenue = completedOrders.Sum(o => o.FinalPrice);
            var allServices = _SqliteDataService.GetAllServices();
            var totalWasherEarnings = completedOrders.Sum(o => OrderMath.Calculate(o, allServices).WasherEarnings);
            var totalCompanyEarnings = completedOrders.Sum(o => OrderMath.Calculate(o, allServices).CompanyEarnings);

            int cashCount = 0; decimal cashAmount = 0;
            int cardCount = 0; decimal cardAmount = 0;
            int transferCount = 0; decimal transferAmount = 0;
            int qrCount = 0; decimal qrAmount = 0;

            foreach (var order in completedOrders)
            {
                switch (order.PaymentMethod)
                {
                    case "Наличные": cashCount++; cashAmount += order.FinalPrice; break;
                    case "Карта": cardCount++; cardAmount += order.FinalPrice; break;
                    case "Перевод": transferCount++; transferAmount += order.FinalPrice; break;
                    case "QR-код": qrCount++; qrAmount += order.FinalPrice; break;
                }
            }

            foreach (var order in completedOrders)
                if (order.ClientId.HasValue) ValidateClientStats(order.ClientId.Value);

            var inProgressOrders = _allOrders.Where(o => o.Status == "Выполняется").ToList();
            var pendingAppointments = _SqliteDataService.GetAppointmentsByDate(DateTime.Now).Where(a => !a.IsCompleted).ToList();

            string warningMessage = "";
            if (inProgressOrders.Any()) warningMessage += $"\n⚠️ Заказов в работе: {inProgressOrders.Count} (не войдут в отчёт)";
            if (pendingAppointments.Any()) warningMessage += $"\n⚠️ Предварительных записей: {pendingAppointments.Count} (не войдут в отчёт)";

            var result = MessageBox.Show($"Закрыть смену?\n\n" +
                $"📅 Дата: {_currentShift.Date:dd.MM.yyyy}\n" +
                $"🚗 Выполнено машин: {completedOrders.Count}\n" +
                $"💰 Общая выручка: {totalRevenue:N0} ₽\n" +
                $"👤 Мойщикам (35%): {totalWasherEarnings:N0} ₽\n" +
                $"🏢 Компании (65%): {totalCompanyEarnings:N0} ₽\n\n" +
                $"💳 Наличные: {cashCount} шт. / {cashAmount:N0} ₽\n" +
                $"💳 Карта: {cardCount} шт. / {cardAmount:N0} ₽\n" +
                $"📱 Перевод: {transferCount} шт. / {transferAmount:N0} ₽\n" +
                $"{warningMessage}\n\nЭто действие нельзя отменить!",
                "Подтверждение закрытия смены",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                // === ЗАКРЫВАЕМ СМЕНУ ===
                _currentShift.EndTime = DateTime.Now;
                _currentShift.IsClosed = true;

                var report = new ShiftReport
                {
                    Id = Guid.NewGuid().GetHashCode(),
                    Date = _currentShift.Date,
                    StartTime = _currentShift.StartTime.Value,
                    EndTime = _currentShift.EndTime.Value,
                    TotalCars = completedOrders.Count,
                    TotalRevenue = totalRevenue,
                    TotalWasherEarnings = totalWasherEarnings,
                    TotalCompanyEarnings = totalCompanyEarnings,
                    CashCount = cashCount,
                    CashAmount = cashAmount,
                    CardCount = cardCount,
                    CardAmount = cardAmount,
                    TransferCount = transferCount,
                    TransferAmount = transferAmount,
                    QrCount = qrCount,
                    QrAmount = qrAmount,
                    Notes = "Смена закрыта штатно"
                };

                // === РАСЧЁТ ЗП МОЙЩИКОВ ЗА СМЕНУ ===

                var washerPayReport = completedOrders
                    .Where(o => o.WasherId > 0)
                    .GroupBy(o => o.WasherId)
                    .Select(g =>
                    {
                        // Проверяем, админ ли этот мойщик
                        var washer = allUsers.FirstOrDefault(u => u.Id == g.Key);
                        bool isWasherAdmin = washer?.IsAdmin == true;

                        decimal basePay = g.Sum(o => OrderMath.Calculate(o, allServices).WasherEarnings);

                        // Админам не делаем надбавку!
                        decimal finalPay = isWasherAdmin ? basePay : Math.Max(basePay, OrderMath.MIN_WASHER_PAY_PER_SHIFT);
                        decimal topUp = isWasherAdmin ? 0 : (finalPay - basePay);

                        return new
                        {
                            WasherId = g.Key,
                            BasePay = basePay,
                            FinalPay = finalPay,
                            TopUp = topUp,
                            OrdersCount = g.Count(),
                            IsAdmin = isWasherAdmin  // ← Для отладки/логирования
                        };
                    }).ToList();

                // === ЛОГИРОВАНИЕ ===
                foreach (var wp in washerPayReport)
                {
                    var washer = _SqliteDataService.GetAllUsers().FirstOrDefault(u => u.Id == wp.WasherId);
                    string roleTag = wp.IsAdmin ? " [АДМИН]" : "";

                    if (wp.TopUp > 0)
                    {
                        Logger.Info($"ДОПЛАТА до мин. ЗП{roleTag} | Мойщик: {washer?.FullName} | Заработано: {wp.BasePay:N0}₽ | Доплата: {wp.TopUp:N0}₽ | Итого: {wp.FinalPay:N0}₽", "SHIFT_PAY");
                    }
                    else if (wp.IsAdmin)
                    {
                        Logger.Info($"ЗП админа (без мин. гарантии){roleTag} | {washer?.FullName} | Заказы: {wp.OrdersCount} | Заработано: {wp.FinalPay:N0}₽", "SHIFT_PAY");
                    }
                    else
                    {
                        Logger.Info($"ЗП мойщика{roleTag} | {washer?.FullName} | Заказы: {wp.OrdersCount} | Заработано: {wp.FinalPay:N0}₽", "SHIFT_PAY");
                    }
                }

                // === ФОРМИРОВАНИЕ ОТЧЁТА ПО СОТРУДНИКАМ ===
                foreach (var empId in _currentShift.EmployeeIds)
                {
                    var employee = allUsers.FirstOrDefault(u => u.Id == empId);
                    if (employee != null)
                    {
                        var pay = washerPayReport.FirstOrDefault(p => p.WasherId == empId);
                        report.EmployeesWork.Add(new EmployeeWorkReport
                        {
                            EmployeeId = empId,
                            EmployeeName = employee.FullName,
                            CarsWashed = pay?.OrdersCount ?? 0,
                            TotalAmount = pay?.BasePay ?? 0,
                            Earnings = pay != null ? pay.FinalPay : 0m
                        });
                    }
                }

                // Добавляем мойщиков, которые работали, но не в текущей смене
                foreach (var pay in washerPayReport.Where(p => !_currentShift.EmployeeIds.Contains(p.WasherId)))
                {
                    var employee = allUsers.FirstOrDefault(u => u.Id == pay.WasherId);
                    if (employee != null)
                    {
                        report.EmployeesWork.Add(new EmployeeWorkReport
                        {
                            EmployeeId = pay.WasherId,
                            EmployeeName = employee.FullName,
                            CarsWashed = pay.OrdersCount,
                            TotalAmount = pay.BasePay,
                            Earnings = pay.FinalPay
                        });
                    }
                }

                SaveShiftReport(report);

                // ВАЖНО: закрываем смену в БД
                _SqliteDataService.CloseShift(_currentShift.Id, "Смена закрыта штатно");

                // Обновляем данные
                _SqliteDataService = new SqliteDataService();
                _currentShift = _SqliteDataService.GetCurrentOpenShift();

                if (AppointmentsOverlay != null)
                    AppointmentsOverlay.SqliteDataService = _SqliteDataService;

                LoadData();

                // === ЛОГИРОВАНИЕ (используем сохранённые переменные, т.к. _currentShift может быть null) ===
                Logger.Info($"Смена успешно закрыта", "SHIFT",
                    $"ID: {shiftId} | Дата: {shiftDate?.ToString("dd.MM.yyyy")} | Выполнено: {report.TotalCars} | Выручка: {report.TotalRevenue:N0} ₽");

                MessageBox.Show($"Смена успешно закрыта!\n\n" +
                    $"📅 Дата: {report.Date:dd.MM.yyyy}\n" +
                    $"🚗 Выполнено машин: {report.TotalCars}\n" +
                    $"💰 Выручка: {report.TotalRevenue:N0} ₽\n" +
                    $"👤 Мойщикам (35%): {report.TotalWasherEarnings:N0} ₽\n" +
                    $"🏢 Компании (65%): {report.TotalCompanyEarnings:N0} ₽\n\n" +
                    $"💳 Наличные: {report.CashCount} шт. / {report.CashAmount:N0} ₽\n" +
                    $"💳 Карта: {report.CardCount} шт. / {report.CardAmount:N0} ₽\n" +
                    $"📱 Перевод: {report.TransferCount} шт. / {report.TransferAmount:N0} ₽\n\n" +
                    $"📁 Отчет сохранен в папке Reports",
                    "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.Error("Критическая ошибка при закрытии смены", ex, "SHIFT",
                    $"ID смены: {shiftId} | Дата: {shiftDate?.ToString("dd.MM.yyyy")}");
                MessageBox.Show($"Ошибка при закрытии смены: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ValidateClientStats(int clientId)
        {
            var client = _SqliteDataService.GetClientById(clientId);
            if (client == null) return;

            var clientOrders = _allOrders.Where(o => o.ClientId == clientId && o.Status == "Выполнен").ToList();
            int expectedVisits = clientOrders.Count;
            decimal expectedTotal = clientOrders.Sum(o => o.FinalPrice);

            if (client.VisitsCount != expectedVisits || client.TotalSpent != expectedTotal)
            {
                System.Diagnostics.Debug.WriteLine($"ВНИМАНИЕ: Статистика клиента {client.FullName} не совпадает!");
                System.Diagnostics.Debug.WriteLine($"  Ожидается: {expectedVisits} визитов, {expectedTotal:N0} ₽");
                System.Diagnostics.Debug.WriteLine($"  Фактически: {client.VisitsCount} визитов, {client.TotalSpent:N0} ₽");

                // Исправляем статистику
                client.VisitsCount = expectedVisits;
                client.TotalSpent = expectedTotal;
                client.LastVisitDate = clientOrders.Max(o => o.Time);

                _SqliteDataService.UpdateClient(client);
            }
        }
        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Простой диалог выбора типа экспорта
                var result = MessageBox.Show("Выберите тип экспорта:\n\n" +
                    "✅ Да — Полная выгрузка (клиенты + заказы + услуги)\n" +
                    "❌ Нет — Только клиенты",
                    "Экспорт данных", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (result == MessageBoxResult.Cancel) return;

                string exportType = result == MessageBoxResult.Yes ? "full" : "clients";
                string filePath = _SqliteDataService.ExportDataToJson(exportType);

                Logger.Info($"Экспорт инициирован пользователем | Тип: {exportType} | Файл: {Path.GetFileName(filePath)}", "EXPORT");

                MessageBox.Show($"Данные экспортированы!\n\n📁 Файл: {Path.GetFileName(filePath)}\n📂 Папка: Exports",
                    "Экспорт завершён", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка при экспорте по запросу пользователя", ex, "EXPORT");
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void SaveShiftReport(ShiftReport report)
        {
            try
            {
                string reportsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                if (!Directory.Exists(reportsPath))
                    Directory.CreateDirectory(reportsPath);

                string fileName = $"ShiftReport_{report.Date:yyyy-MM-dd_HHmmss}.json";
                string filePath = Path.Combine(reportsPath, fileName);

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(filePath, json);

                System.Diagnostics.Debug.WriteLine($"Отчет сохранен: {filePath}");

                // ← ДОБАВЬ ЭТУ СТРОКУ:
                Logger.Info($"Отчёт смены сохранён | Файл: {fileName} | Выручка: {report.TotalRevenue:N0} ₽", "REPORT");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения отчета: {ex.Message}");
                Logger.Error("Ошибка сохранения отчёта смены", ex, "REPORT");
            }
        }

        private void ServicesButton_Click(object sender, RoutedEventArgs e)
        {
            var servicesWin = App.GetService<ServiceManagementWindow>();
            servicesWin.ShowDialog();
        }

        private void ReportsButton_Click(object sender, RoutedEventArgs e)
        {
            var reportsWin = new ReportsWindow(_SqliteDataService); // SqliteDataService уже в DI
            reportsWin.ShowDialog();
        }


        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("Приложение закрыто пользователем", "APP");
            Application.Current.Shutdown();
        }

        private void EditOrderMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem != null)
            {
                OpenEditOrder(SelectedItem);
            }
            else
            {
                MessageBox.Show("Выберите заказ для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteOrderMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem == null)
            {
                MessageBox.Show("Выберите заказ для удаления", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            System.Diagnostics.Debug.WriteLine("=========================================");
            System.Diagnostics.Debug.WriteLine("=== УДАЛЕНИЕ ЗАКАЗА ===");
            System.Diagnostics.Debug.WriteLine($"SelectedItem.Id: {SelectedItem.Id}");
            System.Diagnostics.Debug.WriteLine($"SelectedItem.CarNumber: {SelectedItem.CarNumber}");
            System.Diagnostics.Debug.WriteLine($"SelectedItem.Time: {SelectedItem.Time}");
            System.Diagnostics.Debug.WriteLine($"SelectedItem.IsAppointment: {SelectedItem.IsAppointment}");
            System.Diagnostics.Debug.WriteLine($"SelectedItem.AppointmentId: {SelectedItem.AppointmentId}");

            // ========== 1. ОБРАБОТКА ЗАПИСИ (Appointment) ==========
            if (SelectedItem.IsAppointment && SelectedItem.AppointmentId.HasValue)
            {
                var appointment = _SqliteDataService.GetAppointmentById(SelectedItem.AppointmentId.Value);
                if (appointment != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Найдена запись ID: {appointment.Id}, IsCompleted: {appointment.IsCompleted}, OrderId: {appointment.OrderId}");

                    // Если запись уже преобразована в заказ
                    if (appointment.IsCompleted && appointment.OrderId.HasValue)
                    {
                        var result = MessageBox.Show($"Эта запись уже преобразована в заказ #{appointment.OrderId.Value}.\n\n" +
                            $"Удалить запись из списка предварительных записей?\n" +
                            $"(Заказ останется в системе и не будет удален)",
                            "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            Logger.Info($"Удаление записи (заказ сохранён): #{appointment.Id} | Авто: {SelectedItem.CarNumber}", "ORDER");
                            _SqliteDataService.DeleteAppointment(appointment.Id);
                            LoadData();
                            MessageBox.Show("Запись удалена (заказ сохранен)", "Успешно");
                        }
                    }
                    // Если запись не выполнена (обычная предварительная запись)
                    else if (!appointment.IsCompleted)
                    {
                        var result = MessageBox.Show($"Удалить запись?\n\n{SelectedItem.CarModel} ({SelectedItem.CarNumber})\n" +
                            $"Время: {SelectedItem.Time:HH:mm}\n\nЭто действие нельзя отменить!",
                            "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            Logger.Info($"Удаление записи: #{appointment.Id} | Авто: {appointment.CarNumber}", "ORDER");
                            _SqliteDataService.DeleteAppointment(appointment.Id);
                            LoadData();
                            MessageBox.Show("Запись удалена", "Успешно");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Эта запись уже выполнена и не может быть удалена", "Внимание",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Запись не найдена", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return;
            }

            // ========== 2. ОБРАБОТКА ОБЫЧНОГО ЗАКАЗА ==========
            if (!SelectedItem.IsAppointment && SelectedItem.Id > 0)
            {
                // Ищем заказ по ID
                var originalOrder = _allOrders.FirstOrDefault(o => o.Id == SelectedItem.Id);

                if (originalOrder != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Найден заказ ID: {originalOrder.Id}, Статус: {originalOrder.Status}");
                    System.Diagnostics.Debug.WriteLine($"Заказ IsAppointment: {originalOrder.IsAppointment}");
                    System.Diagnostics.Debug.WriteLine($"Заказ AppointmentId: {originalOrder.AppointmentId}");
                    System.Diagnostics.Debug.WriteLine($"Заказ ShiftId: {originalOrder.ShiftId}");

                    var result = MessageBox.Show($"Удалить заказ?\n\n{originalOrder.CarModel} ({originalOrder.CarNumber})\n" +
                        $"Время: {originalOrder.Time:HH:mm}\nСумма: {originalOrder.FinalPrice:N0} ₽\n\nЭто действие нельзя отменить!",
                        "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        Logger.Info($"Удаление начато: Заказ #{originalOrder.Id} | Авто: {originalOrder.CarNumber}", "ORDER");

                        try
                        {
                            // Обновляем статус заказа на "Отменен" вместо удаления
                            _SqliteDataService.UpdateOrderStatus(originalOrder.Id, "Отменен");

                            // Обновляем данные
                            _SqliteDataService = new SqliteDataService();
                            LoadData();
                            MessageBox.Show("Заказ отменен", "Успешно");
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Ошибка при отмене заказа", ex, "ORDER");
                            MessageBox.Show($"Ошибка при отмене: {ex.Message}", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else
                {
                    // Fallback: поиск по номеру+времени
                    var fallbackOrder = _allOrders.FirstOrDefault(o => o.CarNumber == SelectedItem.CarNumber && o.Time == SelectedItem.Time);

                    if (fallbackOrder != null)
                    {
                        var result = MessageBox.Show($"Удалить заказ?\n\n{fallbackOrder.CarModel} ({fallbackOrder.CarNumber})\n" +
                            $"Время: {fallbackOrder.Time:HH:mm}\nСумма: {fallbackOrder.FinalPrice:N0} ₽\n\nЭто действие нельзя отменить!",
                            "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            try
                            {
                                _SqliteDataService.UpdateOrderStatus(originalOrder.Id, "Отменен");
                                _SqliteDataService = new SqliteDataService();
                                LoadData();
                                MessageBox.Show("Заказ удален", "Успешно");
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Ошибка при удалении заказа (fallback)", ex, "ORDER");
                                MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Заказ не найден для удаления", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                return;
            }

            // ========== 3. FALLBACK ДЛЯ НЕИЗВЕСТНОГО ТИПА ==========
            System.Diagnostics.Debug.WriteLine("Не удалось определить тип элемента для удаления");
            MessageBox.Show("Не удалось найти заказ для удаления", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        private void AppointmentButton_Click(object sender, RoutedEventArgs e)
        {
            // Используем DI для создания окна
            var appointmentWin = App.GetService<AppointmentWindow>();

            // Подписываемся на закрытие окна
            appointmentWin.Closed += (s, args) =>
            {
                System.Diagnostics.Debug.WriteLine($"=== AppointmentWindow Closed: DialogResult = {appointmentWin.DialogResult} ===");

                // Проверяем, что окно закрыто с сохранением
                if (appointmentWin.DialogResult == true)
                {
                    System.Diagnostics.Debug.WriteLine("=== Запись добавлена, обновляем MainWindow ===");
                    // Полностью перезагружаем данные
                    _SqliteDataService = new SqliteDataService();
                    LoadData();
                }
            };

            appointmentWin.ShowDialog();
        }

        private void ViewAppointmentsButton_Click(object sender, RoutedEventArgs e)
        {
            AppointmentsOverlay.Show();
        }

        private void AppointmentsBoardButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=== AppointmentsBoardButton_Click ===");
            if (AppointmentsOverlay == null)
            {
                System.Diagnostics.Debug.WriteLine("AppointmentsOverlay is NULL!");
                return;
            }
            System.Diagnostics.Debug.WriteLine("Calling AppointmentsOverlay.Show()");
            AppointmentsOverlay.Show();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }
        private void LoadAppointmentsToDisplay()
        {
            var todayAppointments = _SqliteDataService.GetAppointmentsByDate(DateTime.Now);

            if (_currentShift == null || _currentShift.IsClosed)
            {
                if (todayAppointments.Any())
                {
                    ShowAppointmentsAsWarning(todayAppointments);
                }
                return;
            }

            var unconvertedAppointments = todayAppointments.Where(a => !a.IsCompleted).ToList();

            if (unconvertedAppointments.Any())
            {
                foreach (var appointment in unconvertedAppointments)
                {
                    int defaultWasherId = _currentShift.EmployeeIds.FirstOrDefault();
                    if (defaultWasherId == 0)
                    {
                        var allUsers = _SqliteDataService.GetAllUsers();
                        defaultWasherId = allUsers.FirstOrDefault()?.Id ?? 1;
                    }

                    var order = _SqliteDataService.ConvertAppointmentToOrder(appointment, _currentShift.Id, defaultWasherId);
                    _currentShift.Orders.Add(order);
                }
                // Убираем вызов SaveData() – изменения уже сохранены в ConvertAppointmentToOrder через транзакцию

                // Перезагружаем заказы из БД
                _currentShift = _SqliteDataService.GetCurrentOpenShift();
                _allOrders = _currentShift?.Orders.ToList() ?? new List<CarWashOrder>();
                ApplyFilterAndDisplay();
                UpdateInfo();
            }
        }

        private void ShowAppointmentsAsWarning(List<Appointment> appointments)
        {
            // Создаем временные отображаемые элементы для записей
            var allServices = _SqliteDataService.GetAllServices();

            var warningAppointmentItems = appointments.Select(a => new OrderDisplayItem
            {
                CarModel = a.CarModel,
                CarNumber = a.CarNumber,
                Time = a.AppointmentDate,
                WasherName = "⚠️ Нет смены",
                ServicesList = string.Join(", ", a.ServiceIds.Select(id => allServices.FirstOrDefault(s => s.Id == id)?.Name ?? "Unknown")),
                FinalPrice = a.ServiceIds.Sum(id => allServices.FirstOrDefault(s => s.Id == id)?.GetPrice(1) ?? 0) + a.ExtraCost,
                ExtraCost = a.ExtraCost,
                ExtraCostReason = a.ExtraCostReason,
                BoxNumber = a.BoxNumber,
                Status = "Ожидает смены",
                IsAppointment = true
            }).ToList();

            // Распределяем по боксам
            Box1ItemsControl.ItemsSource = warningAppointmentItems.Where(i => i.BoxNumber == 1).ToList();
            Box2ItemsControl.ItemsSource = warningAppointmentItems.Where(i => i.BoxNumber == 2).ToList();
            Box3ItemsControl.ItemsSource = warningAppointmentItems.Where(i => i.BoxNumber == 3).ToList();

            // Показываем сообщение пользователю
            CurrentShiftInfo = "⏰ Нет активной смены. Для выполнения записей начните смену!";
            TotalOrdersInfo = $"📋 Есть {appointments.Count} невыполненных записей на сегодня";
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentShiftInfo)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalOrdersInfo)));
        }

        private void BoxItemsControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox == null) return;

            var selectedOrder = listBox.SelectedItem as OrderDisplayItem;
            if (selectedOrder != null)
            {
                OpenEditOrder(selectedOrder);
            }
        }
        private void ClientsButton_Click(object sender, RoutedEventArgs e)
        {
            var clientsWin = App.GetService<ClientsWindow>();
            clientsWin.ShowDialog();
        }

        private void StartShiftButton_Click(object sender, RoutedEventArgs e)
        {
            var startShiftWin = new StartShiftWindow(_SqliteDataService);
            if (startShiftWin.ShowDialog() == true)
            {
                // Принудительно пересоздаём сервис и загружаем данные
                _SqliteDataService = new SqliteDataService();
                _currentShift = _SqliteDataService.GetCurrentOpenShift();

                System.Diagnostics.Debug.WriteLine($"=== Смена создана, ID: {_currentShift?.Id} ===");

                // Обновляем оверлей
                if (AppointmentsOverlay != null)
                {
                    AppointmentsOverlay.SqliteDataService = _SqliteDataService;
                    AppointmentsOverlay.CurrentShift = _currentShift;
                }

                LoadData();

                // Принудительно обновляем UI
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentShiftInfo)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalOrdersInfo)));

                Logger.Info($"Смена начата | Дата: {DateTime.Now:dd.MM.yyyy} | ID смены: {_currentShift?.Id}", "SHIFT");
                MessageBox.Show($"Смена успешно начата!", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    public class OrderDisplayItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public int Id { get; set; }  // Добавьте это свойство
        public string CarNumber { get; set; }
        public string CarModel { get; set; }
        public DateTime Time { get; set; }
        public string WasherName { get; set; }
        public string ServicesList { get; set; }
        public decimal FinalPrice { get; set; }
        public decimal ExtraCost { get; set; }
        public string ExtraCostReason { get; set; }
        public int BoxNumber { get; set; }
        public string Status { get; set; }
        public bool IsAppointment { get; set; }
        public bool IsCompleted { get; set; }
        public int? AppointmentId { get; set; }
        public string PaymentMethod { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal OriginalTotalPrice { get; set; }

        public string DiscountDisplay
        {
            get
            {
                if (DiscountPercent > 0) return $"−{DiscountPercent:F0}%";
                if (DiscountAmount > 0) return $"−{DiscountAmount:N0} ₽";
                return "";
            }
        }

        public bool HasDiscount => DiscountPercent > 0 || DiscountAmount > 0;

        public string OriginalPriceDisplay => OriginalTotalPrice > 0
            ? $"{OriginalTotalPrice:N0} ₽"
            : "";

        public bool ShowOriginalPrice => HasDiscount && OriginalTotalPrice > 0;
    }



    public class WasherStat
    {
        public string WasherName { get; set; }
        public int CarsCount { get; set; }
        public decimal Earnings { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal Percentage { get; set; }
    }
}
