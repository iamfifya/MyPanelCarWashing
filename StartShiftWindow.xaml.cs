using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace MyPanelCarWashing
{
    public partial class StartShiftWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly DataService _dataService;
        private DateTime _selectedDate;
        private List<EmployeeSelection> _employees;

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                _selectedDate = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedDate)));
            }
        }

        public DateTime MinDate => DateTime.Now.Date;

        public List<EmployeeSelection> Employees
        {
            get => _employees;
            set
            {
                _employees = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Employees)));
            }
        }

        public StartShiftWindow(DataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
            DataContext = this;
            SelectedDate = DateTime.Now.Date;
            LoadEmployees();
        }

        private void LoadEmployees()
        {
            var allEmployees = _dataService.GetAllUsers();
            Employees = allEmployees.Select(e => new EmployeeSelection
            {
                Id = e.Id,
                FullName = e.FullName,
                IsAdmin = e.IsAdmin,
                IsSelected = false
            }).ToList();
        }

        private void StartShiftButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedEmployees = Employees.Where(emp => emp.IsSelected).ToList();

                if (!selectedEmployees.Any())
                {
                    MessageBox.Show("Выберите хотя бы одного сотрудника для смены", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var allShifts = _dataService.GetAllShifts();
                var existingOpenShift = allShifts.FirstOrDefault(s => s.Date.Date == SelectedDate.Date && !s.IsClosed);

                if (existingOpenShift != null)
                {
                    var result = MessageBox.Show($"На {SelectedDate:dd.MM.yyyy} уже есть открытая смена!\n\n" +
                        $"Время начала: {existingOpenShift.StartTime:HH:mm}\n" +
                        $"Сотрудников: {existingOpenShift.EmployeeIds.Count}\n\n" +
                        $"Закрыть её и начать новую?",
                        "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes)
                    {
                        return;
                    }

                    CloseExistingShift(existingOpenShift);
                    allShifts.Remove(existingOpenShift);
                }

                var newShift = new Shift
                {
                    Id = allShifts.Any() ? allShifts.Max(s => s.Id) + 1 : 1,
                    Date = SelectedDate,
                    StartTime = DateTime.Now,
                    IsClosed = false,
                    EmployeeIds = selectedEmployees.Select(emp => emp.Id).ToList(),
                    Orders = new List<CarWashOrder>()
                };

                allShifts.Add(newShift);

                var appData = FileDataService.LoadData();
                appData.Shifts = allShifts;
                FileDataService.SaveData(appData);

                MessageBox.Show($"Смена на {SelectedDate:dd.MM.yyyy} успешно открыта!\n\n" +
                    $"Сотрудников: {selectedEmployees.Count}\n" +
                    $"Список: {string.Join(", ", selectedEmployees.Select(s => s.FullName))}\n" +
                    $"Время начала: {DateTime.Now:HH:mm:ss}",
                    "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии смены: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseExistingShift(Shift shift)
        {
            try
            {
                shift.EndTime = DateTime.Now;
                shift.IsClosed = true;

                var orders = shift.Orders ?? new List<CarWashOrder>();
                var totalRevenue = orders.Sum(o => o.FinalPrice);
                var totalWasherEarnings = orders.Sum(o => o.WasherEarnings);
                var totalCompanyEarnings = orders.Sum(o => o.CompanyEarnings);

                var report = new ShiftReport
                {
                    Id = Guid.NewGuid().GetHashCode(),
                    Date = shift.Date,
                    StartTime = shift.StartTime.Value,
                    EndTime = shift.EndTime.Value,
                    TotalCars = orders.Count,
                    TotalRevenue = totalRevenue,
                    TotalWasherEarnings = totalWasherEarnings,
                    TotalCompanyEarnings = totalCompanyEarnings,
                    Notes = "Смена закрыта для начала новой"
                };

                var allUsers = _dataService.GetAllUsers();

                foreach (var empId in shift.EmployeeIds)
                {
                    var employee = allUsers.FirstOrDefault(u => u.Id == empId);
                    if (employee != null)
                    {
                        var employeeOrders = orders.Where(o => o.WasherId == empId).ToList();
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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class EmployeeSelection : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public int Id { get; set; }
        public string FullName { get; set; }
        public bool IsAdmin { get; set; }

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
}
