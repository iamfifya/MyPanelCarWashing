using MyPanelCarWashing.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyPanelCarWashing
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private List<CarWashOrder> _allOrders;
        private Shift _currentShift;
        private User _currentUser;
        private string _searchFilter = "";
        private List<WasherStat> _washersStats;
        private decimal _companyEarnings;
        private decimal _totalRevenue;

        public string CurrentShiftInfo { get; private set; }
        public string TotalOrdersInfo { get; private set; }


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

        public MainWindow(User user)
        {
            InitializeComponent();
            _currentUser = user;
            LoadData();
            this.DataContext = this;
        }

        private void LoadData()
        {
            try
            {
                _currentShift = Core.DB.GetShiftByDate(DateTime.Now);

                System.Diagnostics.Debug.WriteLine($"=== LoadData вызван ===");
                System.Diagnostics.Debug.WriteLine($"_currentShift: {_currentShift != null}");

                if (_currentShift != null && !_currentShift.IsClosed)
                {
                    if (_currentShift.Orders != null)
                    {
                        _allOrders = _currentShift.Orders.ToList();
                        System.Diagnostics.Debug.WriteLine($"Загружено заказов: {_allOrders.Count}");
                    }
                    else
                    {
                        _allOrders = new List<CarWashOrder>();
                    }
                }
                else
                {
                    _allOrders = new List<CarWashOrder>();
                }

                ApplyFilterAndDisplay();
                UpdateInfo();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _allOrders = new List<CarWashOrder>();
            }
        }

        private void ApplyFilterAndDisplay()
        {
            var filteredOrders = _allOrders.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(_searchFilter))
            {
                filteredOrders = filteredOrders.Where(o =>
                    o.CarNumber.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    o.CarModel.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            var sortedOrders = filteredOrders.OrderBy(o => o.Time).ToList();
            var allServices = Core.DB.GetAllServices();

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
                BoxNumber = o.BoxNumber
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
                BoxNumber = o.BoxNumber
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
                BoxNumber = o.BoxNumber
            }).ToList();

            Box1ItemsControl.ItemsSource = box1Orders;
            Box2ItemsControl.ItemsSource = box2Orders;
            Box3ItemsControl.ItemsSource = box3Orders;
        }

        private string GetWasherName(int washerId)
        {
            var washer = Core.DB.GetAllUsers().FirstOrDefault(u => u.Id == washerId);
            return washer?.FullName ?? "Не назначен";
        }

        private void UpdateInfo()
        {
            if (_currentShift != null && !_currentShift.IsClosed)
            {
                CurrentShiftInfo = $"📅 Смена: {_currentShift.Date:dd.MM.yyyy} | Начало: {_currentShift.StartTime:HH:mm}";

                TotalRevenue = _allOrders.Sum(o => o.TotalPrice);
                var totalWasherEarnings = _allOrders.Sum(o => o.WasherEarnings);
                CompanyEarnings = _allOrders.Sum(o => o.CompanyEarnings);

                TotalOrdersInfo = $"🚗 Всего машин: {_allOrders.Count} | 💰 Выручка: {TotalRevenue:C} | " +
                    $"👤 Мойщикам: {totalWasherEarnings:C} | 🏢 Компании: {CompanyEarnings:C}";
            }
            else if (_currentShift != null && _currentShift.IsClosed)
            {
                CurrentShiftInfo = $"📅 Смена закрыта: {_currentShift.Date:dd.MM.yyyy}";
                TotalRevenue = _allOrders.Sum(o => o.TotalPrice);
                TotalOrdersInfo = $"🚗 Итого за смену: {_allOrders.Count} машин | 💰 {TotalRevenue:C}";
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

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentShiftInfo)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalOrdersInfo)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WashersStats)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CompanyEarnings)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalRevenue)));
        }

        private void UpdateWashersStats()
        {
            System.Diagnostics.Debug.WriteLine("=== UpdateWashersStats вызван ===");

            if (_currentShift == null || _currentShift.IsClosed || !_allOrders.Any())
            {
                WashersStats = new List<WasherStat>();
                return;
            }

            var allUsers = Core.DB.GetAllUsers();

            // Считаем общую выручку смены, чтобы вычислить долю каждого мойщика
            var totalShiftRevenue = _allOrders.Sum(o => o.TotalPrice);

            var stats = _allOrders
                .GroupBy(o => o.WasherId)
                .Select(g => {
                    var washerRevenue = g.Sum(o => o.TotalPrice);
                    return new WasherStat
                    {
                        WasherName = allUsers.FirstOrDefault(u => u.Id == g.Key)?.FullName ?? "Неизвестный",
                        CarsCount = g.Count(),
                        Earnings = g.Sum(o => o.WasherEarnings),
                        TotalRevenue = washerRevenue,

                        // ВЫСЧИТЫВАЕМ ПРОЦЕНТ: (Выручка мойщика / Общая выручка) * 100
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

            var addWin = new AddOrderWindow(_currentShift);
            if (addWin.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void EmployeesButton_Click(object sender, RoutedEventArgs e)
        {
            var empWin = new EmployeeCardWindow();
            empWin.ShowDialog();
        }

        private void StartShiftButton_Click(object sender, RoutedEventArgs e)
        {
            var startWin = new StartShiftWindow();
            if (startWin.ShowDialog() == true)
            {
                LoadData();
                MessageBox.Show($"Смена на {startWin.SelectedDate:dd.MM.yyyy} успешно начата!", "Успешно",
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

            var totalRevenue = _allOrders.Sum(o => o.TotalPrice);
            var totalWasherEarnings = _allOrders.Sum(o => o.WasherEarnings);
            var totalCompanyEarnings = _allOrders.Sum(o => o.CompanyEarnings);

            var result = MessageBox.Show($"Закрыть смену?\n\n" +
                $"📅 Дата: {_currentShift.Date:dd.MM.yyyy}\n" +
                $"🚗 Всего машин: {_allOrders.Count}\n" +
                $"💰 Общая выручка: {totalRevenue:C}\n" +
                $"👤 Мойщикам (35%): {totalWasherEarnings:C}\n" +
                $"🏢 Компании (65%): {totalCompanyEarnings:C}\n\n" +
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
                    Notes = "Смена закрыта штатно"
                };

                var allUsers = Core.DB.GetAllUsers();

                foreach (var empId in _currentShift.EmployeeIds)
                {
                    var employee = allUsers.FirstOrDefault(u => u.Id == empId);
                    if (employee != null)
                    {
                        var employeeOrders = _allOrders.Where(o => o.WasherId == empId).ToList();
                        var employeeRevenue = employeeOrders.Sum(o => o.TotalPrice);
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

                var allShifts = Core.DB.GetAllShifts();
                var existingShift = allShifts.FirstOrDefault(s => s.Id == _currentShift.Id);
                if (existingShift != null)
                {
                    existingShift.EndTime = _currentShift.EndTime;
                    existingShift.IsClosed = true;
                }

                var appData = FileDataService.LoadData();
                appData.Shifts = allShifts;
                FileDataService.SaveData(appData);

                Core.RefreshData();

                LoadData();

                MessageBox.Show($"Смена успешно закрыта!\n\n" +
                    $"📅 Дата: {report.Date:dd.MM.yyyy}\n" +
                    $"🚗 Машин: {report.TotalCars}\n" +
                    $"💰 Выручка: {report.TotalRevenue:C}\n" +
                    $"👤 Мойщикам (35%): {report.TotalWasherEarnings:C}\n" +
                    $"🏢 Компании (65%): {report.TotalCompanyEarnings:C}\n\n" +
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

        private void ReportsButton_Click(object sender, RoutedEventArgs e)
        {
            var reportsWin = new ReportsWindow();
            reportsWin.ShowDialog();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private void ServicesButton_Click(object sender, RoutedEventArgs e)
        {
            var servicesWin = new ServiceManagementWindow();
            servicesWin.ShowDialog();
        }
        private void EditOrderMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var contextMenu = menuItem?.Parent as ContextMenu;
            var groupBox = contextMenu?.PlacementTarget as GroupBox;

            if (groupBox == null) return;

            // Определяем, какой ItemsControl используется в GroupBox
            ItemsControl itemsControl = null;
            if (groupBox.Header.ToString().Contains("Бокс 1"))
                itemsControl = Box1ItemsControl;
            else if (groupBox.Header.ToString().Contains("Бокс 2"))
                itemsControl = Box2ItemsControl;
            else if (groupBox.Header.ToString().Contains("Бокс 3"))
                itemsControl = Box3ItemsControl;

            if (itemsControl == null) return;

            var selectedOrder = itemsControl.ItemsSource?.Cast<OrderDisplayItem>().FirstOrDefault();
            if (selectedOrder == null)
            {
                // Альтернативный способ - через клик на карточке
                return;
            }

            // Находим оригинальный заказ
            var originalOrder = _allOrders.FirstOrDefault(o => o.CarNumber == selectedOrder.CarNumber &&
                                                                o.Time == selectedOrder.Time);
            if (originalOrder != null)
            {
                var editWin = new EditOrderWindow(originalOrder, _currentShift);
                if (editWin.ShowDialog() == true)
                {
                    LoadData(); // Обновляем данные
                }
            }
        }

        private void DeleteOrderMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var contextMenu = menuItem?.Parent as ContextMenu;
            var groupBox = contextMenu?.PlacementTarget as GroupBox;

            if (groupBox == null) return;

            ItemsControl itemsControl = null;
            if (groupBox.Header.ToString().Contains("Бокс 1"))
                itemsControl = Box1ItemsControl;
            else if (groupBox.Header.ToString().Contains("Бокс 2"))
                itemsControl = Box2ItemsControl;
            else if (groupBox.Header.ToString().Contains("Бокс 3"))
                itemsControl = Box3ItemsControl;

            if (itemsControl == null) return;

            var selectedOrder = itemsControl.ItemsSource?.Cast<OrderDisplayItem>().FirstOrDefault();
            if (selectedOrder != null)
            {
                var originalOrder = _allOrders.FirstOrDefault(o => o.CarNumber == selectedOrder.CarNumber &&
                                                                    o.Time == selectedOrder.Time);
                if (originalOrder != null && _currentShift != null)
                {
                    if (MessageBox.Show($"Удалить заказ?\n\n{originalOrder.CarModel} ({originalOrder.CarNumber})", "Подтверждение",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        try
                        {
                            _currentShift.Orders.Remove(originalOrder);
                            Core.DB.SaveData();
                            LoadData();
                            MessageBox.Show("Заказ удален", "Успешно",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }
    }

    public class OrderDisplayItem
    {
        public string CarNumber { get; set; }
        public string CarModel { get; set; }
        public DateTime Time { get; set; }
        public string WasherName { get; set; }
        public string ServicesList { get; set; }
        public decimal FinalPrice { get; set; }
        public decimal ExtraCost { get; set; }
        public string ExtraCostReason { get; set; }
        public int BoxNumber { get; set; }
    }

    public class WasherStat
    {
        public string WasherName { get; set; }
        public int CarsCount { get; set; }
        public decimal Earnings { get; set; }
        public decimal TotalRevenue { get; set; }

        // ДОБАВЛЯЕМ НОВОЕ СВОЙСТВО:
        public decimal Percentage { get; set; }
    }
}