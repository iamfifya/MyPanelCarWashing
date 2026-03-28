using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
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

        private DataService _dataService;
        private List<CarWashOrder> _allOrders;
        private Shift _currentShift;
        private User _currentUser;
        private string _searchFilter = "";
        private List<WasherStat> _washersStats;
        private decimal _companyEarnings;
        private decimal _totalRevenue;

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
                return;
            }

            CashCount = _allOrders.Count(o => o.PaymentMethod == "Наличные");
            CashAmount = _allOrders.Where(o => o.PaymentMethod == "Наличные").Sum(o => o.FinalPrice);

            CardCount = _allOrders.Count(o => o.PaymentMethod == "Карта");
            CardAmount = _allOrders.Where(o => o.PaymentMethod == "Карта").Sum(o => o.FinalPrice);

            TransferCount = _allOrders.Count(o => o.PaymentMethod == "Перевод");
            TransferAmount = _allOrders.Where(o => o.PaymentMethod == "Перевод").Sum(o => o.FinalPrice);
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
                System.Diagnostics.Debug.WriteLine($"=== LoadData ===");
                System.Diagnostics.Debug.WriteLine($"Текущая дата: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");

                // Используем _dataService вместо Core.DB
                _currentShift = _dataService.GetShiftByDate(DateTime.Now);

                System.Diagnostics.Debug.WriteLine($"_currentShift: {_currentShift != null}");
                if (_currentShift != null)
                {
                    System.Diagnostics.Debug.WriteLine($"  ID: {_currentShift.Id}, StartTime: {_currentShift.StartTime}");
                }

                if (_currentShift != null && !_currentShift.IsClosed)
                {
                    _allOrders = _currentShift.Orders ?? new List<CarWashOrder>();
                }
                else
                {
                    _allOrders = new List<CarWashOrder>();
                }

                // Получаем записи на сегодня
                var todayAppointments = _dataService.GetAppointmentsByDate(DateTime.Now);
                var allServices = _dataService.GetAllServices();

                // Заказы
                var orderItems = _allOrders.Select(o => new OrderDisplayItem
                {
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
                    IsCompleted = false
                }).ToList();

                // Записи
                var appointmentItems = todayAppointments.Select(a => new OrderDisplayItem
                {
                    CarModel = a.CarModel,
                    CarNumber = a.CarNumber,
                    Time = a.AppointmentDate,
                    WasherName = "📅 Запись",
                    ServicesList = string.Join(", ", a.ServiceIds.Select(id => allServices.FirstOrDefault(s => s.Id == id)?.Name ?? "Unknown")),
                    FinalPrice = a.ServiceIds.Sum(id => allServices.FirstOrDefault(s => s.Id == id)?.Price ?? 0) + a.ExtraCost,
                    ExtraCost = a.ExtraCost,
                    ExtraCostReason = a.ExtraCostReason,
                    BoxNumber = a.BoxNumber,
                    Status = "Предварительная запись",
                    IsAppointment = true,
                    IsCompleted = a.IsCompleted,
                    AppointmentId = a.Id
                }).ToList();

                // Объединяем и сортируем по времени
                var allItems = orderItems.Concat(appointmentItems).OrderBy(i => i.Time).ToList();

                // Распределяем по боксам
                Box1Items = allItems.Where(i => i.BoxNumber == 1).ToList();
                Box2Items = allItems.Where(i => i.BoxNumber == 2).ToList();
                Box3Items = allItems.Where(i => i.BoxNumber == 3).ToList();

                UpdateInfo();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _allOrders = new List<CarWashOrder>();
            }
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

        public MainWindow(DataService dataService, User user)
        {
            InitializeComponent();
            _dataService = dataService;
            _currentUser = user;
            DataContext = this;
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
            // Если нет заказов, но есть записи, они уже загружены через LoadAppointmentsToDisplay
            if (_allOrders == null || !_allOrders.Any())
            {
                // Проверяем, не загружены ли уже записи через ShowAppointmentsAsWarning
                if (Box1ItemsControl.ItemsSource == null || !(Box1ItemsControl.ItemsSource as IEnumerable<object>)?.Any() == true)
                {
                    Box1ItemsControl.ItemsSource = new List<OrderDisplayItem>();
                    Box2ItemsControl.ItemsSource = new List<OrderDisplayItem>();
                    Box3ItemsControl.ItemsSource = new List<OrderDisplayItem>();
                }
                return;
            }

            var filteredOrders = _allOrders.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(_searchFilter))
            {
                filteredOrders = filteredOrders.Where(o =>
                    o.CarNumber.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    o.CarModel.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            var sortedOrders = filteredOrders.OrderBy(o => o.Time).ToList();
            var allServices = _dataService.GetAllServices();

            var box1Orders = sortedOrders.Where(o => o.BoxNumber == 1).Select(o => new OrderDisplayItem
            {
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
                IsAppointment = o.IsAppointment
            }).ToList();

            var box2Orders = sortedOrders.Where(o => o.BoxNumber == 2).Select(o => new OrderDisplayItem
            {
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
                IsAppointment = o.IsAppointment
            }).ToList();

            var box3Orders = sortedOrders.Where(o => o.BoxNumber == 3).Select(o => new OrderDisplayItem
            {
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
                IsAppointment = o.IsAppointment
            }).ToList();

            Box1ItemsControl.ItemsSource = box1Orders;
            Box2ItemsControl.ItemsSource = box2Orders;
            Box3ItemsControl.ItemsSource = box3Orders;
        }

        private string GetWasherName(int washerId)
        {
            var washer = _dataService.GetAllUsers().FirstOrDefault(u => u.Id == washerId);
            return washer?.FullName ?? "Не назначен";
        }

        private void UpdateInfo()
        {
            if (_currentShift != null && !_currentShift.IsClosed)
            {
                CurrentShiftInfo = $"📅 Смена: {_currentShift.Date:dd.MM.yyyy} | Начало: {_currentShift.StartTime:HH:mm}";

                TotalRevenue = _allOrders.Sum(o => o.FinalPrice);
                var totalWasherEarnings = _allOrders.Sum(o => o.WasherEarnings);
                CompanyEarnings = _allOrders.Sum(o => o.CompanyEarnings);

                TotalOrdersInfo = $"🚗 Всего машин: {_allOrders.Count} | " +
                    $"👤 Мойщикам: {totalWasherEarnings:N0} ₽";
            }
            else if (_currentShift != null && _currentShift.IsClosed)
            {
                CurrentShiftInfo = $"📅 Смена закрыта: {_currentShift.Date:dd.MM.yyyy}";
                TotalRevenue = _allOrders.Sum(o => o.FinalPrice);
                TotalOrdersInfo = $"🚗 Итого за смену: {_allOrders.Count} машин | 💰 {TotalRevenue:N0} ₽";
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
            UpdatePaymentStats(); // Добавьте эту строку

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentShiftInfo)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalOrdersInfo)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WashersStats)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CompanyEarnings)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalRevenue)));

            // Добавьте уведомления для новых свойств
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

            var allUsers = _dataService.GetAllUsers();
            var totalShiftRevenue = _allOrders.Sum(o => o.FinalPrice);

            var stats = _allOrders
                .GroupBy(o => o.WasherId)
                .Select(g =>
                {
                    var washerRevenue = g.Sum(o => o.FinalPrice);
                    return new WasherStat
                    {
                        WasherName = allUsers.FirstOrDefault(u => u.Id == g.Key)?.FullName ?? "Неизвестный",
                        CarsCount = g.Count(),
                        Earnings = g.Sum(o => o.WasherEarnings),
                        TotalRevenue = washerRevenue,
                        Percentage = totalShiftRevenue > 0 ? (washerRevenue / totalShiftRevenue) * 100m : 0m
                    };
                })
                .OrderByDescending(s => s.Earnings)
                .ToList();

            WashersStats = stats;
        }

        private void SearchFilterTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            _searchFilter = SearchFilterTextBox.Text;
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

            var addWin = new AddOrderWindow(_dataService, _currentShift);
            if (addWin.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void EmployeesButton_Click(object sender, RoutedEventArgs e)
        {
            var empWin = new EmployeeCardWindow(_dataService);
            empWin.ShowDialog();
        }

        private void StartShiftButton_Click(object sender, RoutedEventArgs e)
        {
            var startWin = new StartShiftWindow(_dataService);
            if (startWin.ShowDialog() == true)
            {
                // Обновляем _dataService, чтобы получить свежие данные
                _dataService = new DataService();
                LoadData();
                MessageBox.Show($"Смена успешно начата!", "Успешно",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CloseShiftButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentShift == null)
            {
                MessageBox.Show("Нет активной смены для закрытия", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_currentShift.IsClosed)
            {
                MessageBox.Show("Эта смена уже закрыта", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var totalRevenue = _allOrders.Sum(o => o.FinalPrice);
            var totalWasherEarnings = _allOrders.Sum(o => o.WasherEarnings);
            var totalCompanyEarnings = _allOrders.Sum(o => o.CompanyEarnings);

            // Сначала объявляем переменные
            int cashCount = 0;
            decimal cashAmount = 0;
            int cardCount = 0;
            decimal cardAmount = 0;
            int transferCount = 0;
            decimal transferAmount = 0;

            // Затем собираем статистику по оплатам
            foreach (var order in _allOrders)
            {
                switch (order.PaymentMethod)
                {
                    case "Наличные":
                        cashCount++;
                        cashAmount += order.FinalPrice;
                        break;
                    case "Карта":
                        cardCount++;
                        cardAmount += order.FinalPrice;
                        break;
                    case "Перевод":
                        transferCount++;
                        transferAmount += order.FinalPrice;
                        break;
                }
            }

            var result = MessageBox.Show($"Закрыть смену?\n\n" +
                $"📅 Дата: {_currentShift.Date:dd.MM.yyyy}\n" +
                $"🚗 Всего машин: {_allOrders.Count}\n" +
                $"💰 Общая выручка: {totalRevenue:N0} ₽\n" +
                $"👤 Мойщикам (35%): {totalWasherEarnings:N0} ₽\n" +
                $"🏢 Компании (65%): {totalCompanyEarnings:N0} ₽\n\n" +
                $"💳 Наличные: {cashCount} шт. / {cashAmount:N0} ₽\n" +
                $"💳 Карта: {cardCount} шт. / {cardAmount:N0} ₽\n" +
                $"📱 Перевод: {transferCount} шт. / {transferAmount:N0} ₽\n\n" +
                $"Это действие нельзя отменить!",
                "Подтверждение закрытия смены",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                _currentShift.EndTime = DateTime.Now;
                _currentShift.IsClosed = true;

                var report = new ShiftReport
                {
                    Id = Guid.NewGuid().GetHashCode(),
                    Date = _currentShift.Date,
                    StartTime = _currentShift.StartTime.Value,
                    EndTime = _currentShift.EndTime.Value,
                    TotalCars = _allOrders.Count,
                    TotalRevenue = totalRevenue,
                    TotalWasherEarnings = totalWasherEarnings,
                    TotalCompanyEarnings = totalCompanyEarnings,
                    CashCount = cashCount,
                    CashAmount = cashAmount,
                    CardCount = cardCount,
                    CardAmount = cardAmount,
                    TransferCount = transferCount,
                    TransferAmount = transferAmount,
                    Notes = "Смена закрыта штатно"
                };

                var allUsers = _dataService.GetAllUsers();

                foreach (var empId in _currentShift.EmployeeIds)
                {
                    var employee = allUsers.FirstOrDefault(u => u.Id == empId);
                    if (employee != null)
                    {
                        var employeeOrders = _allOrders.Where(o => o.WasherId == empId).ToList();
                        var employeeRevenue = employeeOrders.Sum(o => o.FinalPrice);
                        var employeeEarnings = employeeOrders.Sum(o => o.WasherEarnings);

                        report.EmployeesWork.Add(new EmployeeWorkReport
                        {
                            EmployeeId = empId,
                            EmployeeName = employee.FullName,
                            CarsWashed = employeeOrders.Count,
                            TotalAmount = employeeRevenue,
                            Earnings = employeeEarnings
                        });
                    }
                }

                SaveShiftReport(report);

                var allShifts = _dataService.GetAllShifts();
                var existingShift = allShifts.FirstOrDefault(s => s.Id == _currentShift.Id);
                if (existingShift != null)
                {
                    existingShift.EndTime = _currentShift.EndTime;
                    existingShift.IsClosed = true;
                }

                var appData = FileDataService.LoadData();
                appData.Shifts = allShifts;
                FileDataService.SaveData(appData);

                LoadData();

                MessageBox.Show($"Смена успешно закрыта!\n\n" +
                    $"📅 Дата: {report.Date:dd.MM.yyyy}\n" +
                    $"🚗 Машин: {report.TotalCars}\n" +
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
                MessageBox.Show($"Ошибка при закрытии смены: {ex.Message}", "Ошибка",
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения отчета: {ex.Message}");
            }
        }

        private void ServicesButton_Click(object sender, RoutedEventArgs e)
        {
            var servicesWin = new ServiceManagementWindow(); // Без параметров
            servicesWin.ShowDialog();
        }

        private void ReportsButton_Click(object sender, RoutedEventArgs e)
        {
            var reportsWin = new ReportsWindow(_dataService);
            reportsWin.ShowDialog();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
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
            if (SelectedItem != null)
            {
                if (SelectedItem.IsAppointment && SelectedItem.AppointmentId.HasValue)
                {
                    var appointment = _dataService.GetAppointmentById(SelectedItem.AppointmentId.Value);
                    if (appointment != null)
                    {
                        var result = MessageBox.Show($"Удалить запись?\n\n{SelectedItem.CarModel} ({SelectedItem.CarNumber})",
                            "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            _dataService.DeleteAppointment(appointment.Id);
                            LoadData();
                            MessageBox.Show("Запись удалена", "Успешно");
                        }
                    }
                }
                else
                {
                    var originalOrder = _allOrders.FirstOrDefault(o => o.CarNumber == SelectedItem.CarNumber &&
                                                                        o.Time == SelectedItem.Time);
                    if (originalOrder != null && _currentShift != null)
                    {
                        var result = MessageBox.Show($"Удалить заказ?\n\n{originalOrder.CarModel} ({originalOrder.CarNumber})",
                            "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            _currentShift.Orders.Remove(originalOrder);
                            _dataService.SaveData();
                            LoadData();
                            MessageBox.Show("Заказ удален", "Успешно");
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите заказ для удаления", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void AppointmentButton_Click(object sender, RoutedEventArgs e)
        {
            var appointmentWin = new AppointmentWindow(_dataService);
            // Подписываемся на закрытие окна
            appointmentWin.Closed += (s, args) =>
            {
                // Обновляем данные после добавления записи
                LoadData();
            };
            appointmentWin.ShowDialog();
        }

        private void ViewAppointmentsButton_Click(object sender, RoutedEventArgs e)
        {
            var appointmentsWin = new AppointmentsWindow(_dataService);
            appointmentsWin.ShowDialog();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }
        private void LoadAppointmentsToDisplay()
        {
            // Получаем все записи на сегодня, которые еще не выполнены
            var todayAppointments = _dataService.GetAppointmentsByDate(DateTime.Now);

            // Если нет активной смены, показываем записи с предупреждением
            if (_currentShift == null || _currentShift.IsClosed)
            {
                if (todayAppointments.Any())
                {
                    // Показываем записи в специальном виде
                    ShowAppointmentsAsWarning(todayAppointments);
                }
                return;
            }

            // Если есть активная смена, записи должны быть конвертированы в заказы
            // Проверяем, все ли записи на сегодня конвертированы
            var unconvertedAppointments = todayAppointments.Where(a => !a.IsCompleted).ToList();

            if (unconvertedAppointments.Any())
            {
                // Конвертируем неконвертированные записи
                foreach (var appointment in unconvertedAppointments)
                {
                    int defaultWasherId = _currentShift.EmployeeIds.FirstOrDefault();
                    if (defaultWasherId == 0)
                    {
                        var allUsers = _dataService.GetAllUsers();
                        defaultWasherId = allUsers.FirstOrDefault()?.Id ?? 1;
                    }

                    var order = _dataService.ConvertAppointmentToOrder(appointment, _currentShift.Id, defaultWasherId);
                    _currentShift.Orders.Add(order);
                }
                _dataService.SaveData();

                // Перезагружаем заказы
                _allOrders = _currentShift.Orders.ToList();
                ApplyFilterAndDisplay();
                UpdateInfo();
            }
        }

        private void ShowAppointmentsAsWarning(List<Appointment> appointments)
        {
            // Создаем временные отображаемые элементы для записей
            var allServices = _dataService.GetAllServices();

            var appointmentDisplayItems = appointments.Select(a => new OrderDisplayItem
            {
                CarModel = a.CarModel,
                CarNumber = a.CarNumber,
                Time = a.AppointmentDate,
                WasherName = "⚠️ Нет смены",
                ServicesList = string.Join(", ", a.ServiceIds.Select(id => allServices.FirstOrDefault(s => s.Id == id)?.Name ?? "Unknown")),
                FinalPrice = a.ServiceIds.Sum(id => allServices.FirstOrDefault(s => s.Id == id)?.Price ?? 0) + a.ExtraCost,
                ExtraCost = a.ExtraCost,
                ExtraCostReason = a.ExtraCostReason,
                BoxNumber = a.BoxNumber,
                Status = "Ожидает смены",
                IsAppointment = true
            }).ToList();

            // Распределяем по боксам
            Box1ItemsControl.ItemsSource = appointmentDisplayItems.Where(i => i.BoxNumber == 1).ToList();
            Box2ItemsControl.ItemsSource = appointmentDisplayItems.Where(i => i.BoxNumber == 2).ToList();
            Box3ItemsControl.ItemsSource = appointmentDisplayItems.Where(i => i.BoxNumber == 3).ToList();

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

        private void OpenEditOrder(OrderDisplayItem orderDisplay)
        {
            if (orderDisplay.IsAppointment && orderDisplay.AppointmentId.HasValue)
            {
                var appointment = _dataService.GetAppointmentById(orderDisplay.AppointmentId.Value);
                if (appointment != null)
                {
                    var tempOrder = new CarWashOrder
                    {
                        Id = 0,
                        CarModel = appointment.CarModel,
                        CarNumber = appointment.CarNumber,
                        CarBodyType = appointment.CarBodyType,
                        Time = appointment.AppointmentDate,
                        BoxNumber = appointment.BoxNumber,
                        ServiceIds = appointment.ServiceIds,
                        ExtraCost = appointment.ExtraCost,
                        ExtraCostReason = appointment.ExtraCostReason,
                        Status = "Предварительная запись",
                        IsAppointment = true,
                        AppointmentId = appointment.Id
                    };

                    var editWin = new EditOrderWindow(_dataService, tempOrder, _currentShift);
                    if (editWin.ShowDialog() == true)
                    {
                        LoadData();
                    }
                }
            }
            else
            {
                var originalOrder = _allOrders.FirstOrDefault(o => o.CarNumber == orderDisplay.CarNumber &&
                                                                    o.Time == orderDisplay.Time);
                if (originalOrder != null)
                {
                    var editWin = new EditOrderWindow(_dataService, originalOrder, _currentShift);
                    if (editWin.ShowDialog() == true)
                    {
                        LoadData();
                    }
                }
            }
        }
    }

    public class OrderDisplayItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

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
