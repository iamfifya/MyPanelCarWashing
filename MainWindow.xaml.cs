using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace MyPanelCarWashing
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private List<CarWashOrder> _ordersList;
        private Shift _currentShift;
        private User _currentUser;

        public List<CarWashOrder> OrdersList
        {
            get
            {
                var Result = _ordersList;

                if (SearchFilter != "")
                {
                    Result = Result.Where(o => o.CarNumber.IndexOf(SearchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                o.CarModel.IndexOf(SearchFilter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                }

                return Result.Skip((CurrentPage - 1) * 6).Take(6).ToList();
            }
            set
            {
                _ordersList = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("OrdersList"));
            }
        }

        public MainWindow(User user)
        {
            InitializeComponent();
            _currentUser = user;
            DataContext = this;

            // Получаем или создаем смену на сегодня
            _currentShift = Core.DB.GetShiftByDate(DateTime.Now);
            LoadOrders();
        }

        private void LoadOrders()
        {
            if (_currentShift != null)
            {
                OrdersList = _currentShift.Orders.ToList();
            }
            else
            {
                OrdersList = new List<CarWashOrder>();
            }

            _currentPage = 1;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentPage"));
        }

        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (value > 0 && _ordersList != null)
                {
                    int totalPages = (int)Math.Ceiling((double)_ordersList.Count() / 6);
                    if (value <= totalPages && totalPages > 0)
                    {
                        _currentPage = value;
                        Invalidate();
                    }
                    else if (value == 1 && totalPages == 0)
                    {
                        _currentPage = value;
                        Invalidate();
                    }
                }
            }
        }

        private string _searchFilter = "";
        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                _searchFilter = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("OrdersList"));
            }
        }

        private void Invalidate()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("OrdersList"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentPage"));
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
                LoadOrders();
            }
        }

        private void EditItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedOrder = OrdersListView.SelectedItem as CarWashOrder;
            if (selectedOrder != null)
            {
                var editWin = new EditOrderServicesWindow(selectedOrder);
                if (editWin.ShowDialog() == true)
                {
                    LoadOrders();
                }
            }
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedOrder = OrdersListView.SelectedItem as CarWashOrder;
            if (selectedOrder != null && _currentShift != null)
            {
                if (MessageBox.Show("Удалить заказ?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        _currentShift.Orders.Remove(selectedOrder);
                        Core.DB.SaveData();
                        LoadOrders();
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
                _currentShift = Core.DB.GetShiftByDate(DateTime.Now);
                LoadOrders();
                MessageBox.Show("Смена успешно начата!", "Успешно",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CloseShiftButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentShift == null || _currentShift.IsClosed)
            {
                MessageBox.Show("Нет активной смены для закрытия", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_currentShift.Orders.Any())
            {
                var result = MessageBox.Show("Смена не содержит заказов.\nЗакрыть смену?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;
            }
            else
            {
                var result = MessageBox.Show($"Закрыть смену?\n\n" +
                    $"Всего машин: {_currentShift.TotalCars}\n" +
                    $"Общая выручка: {_currentShift.TotalRevenue:C}\n\n" +
                    $"Это действие нельзя отменить!",
                    "Подтверждение закрытия смены",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;
            }

            try
            {
                _currentShift.EndTime = DateTime.Now;
                _currentShift.IsClosed = true;

                // Создаем отчет
                var report = new ShiftReport
                {
                    Id = Guid.NewGuid().GetHashCode(),
                    Date = _currentShift.Date,
                    StartTime = _currentShift.StartTime.Value,
                    EndTime = _currentShift.EndTime.Value,
                    TotalCars = _currentShift.TotalCars,
                    TotalRevenue = _currentShift.TotalRevenue,
                    Notes = "Смена закрыта штатно"
                };

                // Собираем информацию по сотрудникам
                foreach (var empId in _currentShift.EmployeeIds)
                {
                    var employee = Core.DB.GetAllUsers().FirstOrDefault(u => u.Id == empId);
                    if (employee != null)
                    {
                        var employeeOrders = _currentShift.Orders.Count;
                        var employeeRevenue = _currentShift.Orders.Sum(o => o.TotalPrice);

                        report.EmployeesWork.Add(new EmployeeWorkReport
                        {
                            EmployeeId = empId,
                            EmployeeName = employee.FullName,
                            CarsWashed = employeeOrders,
                            TotalAmount = employeeRevenue
                        });
                    }
                }

                // Сохраняем отчет
                SaveShiftReport(report);

                // Обновляем данные
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

                _currentShift = Core.DB.GetShiftByDate(DateTime.Now);
                LoadOrders();

                MessageBox.Show($"Смена успешно закрыта!\n\n" +
                    $"📅 Дата: {report.Date:dd.MM.yyyy}\n" +
                    $"🚗 Машин: {report.TotalCars}\n" +
                    $"💰 Выручка: {report.TotalRevenue:C}\n\n" +
                    $"Отчет сохранен в папке Reports",
                    "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при закрытии смены: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReportsButton_Click(object sender, RoutedEventArgs e)
        {
            var reportsWin = new ReportsWindow();
            reportsWin.ShowDialog();
        }

        private void SaveShiftReport(ShiftReport report)
        {
            try
            {
                string reportsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                if (!System.IO.Directory.Exists(reportsPath))
                    System.IO.Directory.CreateDirectory(reportsPath);

                string fileName = $"ShiftReport_{report.Date:yyyy-MM-dd_HHmmss}.json";
                string filePath = System.IO.Path.Combine(reportsPath, fileName);

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented);
                System.IO.File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения отчета: {ex.Message}");
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void SearchFilterTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            SearchFilter = SearchFilterTextBox.Text;
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e) => CurrentPage--;
        private void NextPage_Click(object sender, RoutedEventArgs e) => CurrentPage++;
    }
}