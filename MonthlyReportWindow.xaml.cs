using MyPanelCarWashing.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace MyPanelCarWashing
{
    public partial class MonthlyReportWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private DateTime _selectedDate;
        private MonthlyReport _currentMonthlyReport;

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                _selectedDate = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedDate)));
            }
        }

        public MonthlyReportWindow()
        {
            InitializeComponent();
            DataContext = this;
            SelectedDate = DateTime.Now;
            MonthPicker.SelectedDate = DateTime.Now;
        }

        private void GenerateReportButton_Click(object sender, RoutedEventArgs args)
        {
            try
            {
                DateTime selectedDate = MonthPicker.SelectedDate ?? DateTime.Now;
                int year = selectedDate.Year;
                int month = selectedDate.Month;

                // Загружаем все дневные отчеты
                string reportsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                if (!Directory.Exists(reportsPath))
                {
                    MessageBox.Show("Отчеты не найдены", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var reportFiles = Directory.GetFiles(reportsPath, "ShiftReport_*.json");
                var monthlyReports = new List<ShiftReport>();

                foreach (var file in reportFiles)
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var report = Newtonsoft.Json.JsonConvert.DeserializeObject<ShiftReport>(json);
                        if (report != null && report.Date.Year == year && report.Date.Month == month)
                        {
                            monthlyReports.Add(report);
                        }
                    }
                    catch { }
                }

                if (!monthlyReports.Any())
                {
                    MessageBox.Show($"За {month:00}.{year} нет отчетов", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Формируем месячный отчет
                var totalCars = monthlyReports.Sum(r => r.TotalCars);
                var totalRevenue = monthlyReports.Sum(r => r.TotalRevenue);
                var totalWasherEarnings = monthlyReports.Sum(r => r.TotalWasherEarnings);
                var totalCompanyEarnings = monthlyReports.Sum(r => r.TotalCompanyEarnings);

                // Дневные сводки
                var dailySummaries = monthlyReports
                    .OrderBy(r => r.Date)
                    .Select(r => new DailyReportSummary
                    {
                        Date = r.Date,
                        Cars = r.TotalCars,
                        Revenue = r.TotalRevenue,
                        WasherEarnings = r.TotalWasherEarnings,
                        CompanyEarnings = r.TotalCompanyEarnings
                    }).ToList();

                // Статистика по сотрудникам за месяц (ЗАРАБОТНАЯ ПЛАТА)
                var employeesStats = new Dictionary<int, EmployeeWorkReport>();

                foreach (var report in monthlyReports)
                {
                    foreach (var emp in report.EmployeesWork)
                    {
                        if (employeesStats.ContainsKey(emp.EmployeeId))
                        {
                            employeesStats[emp.EmployeeId].CarsWashed += emp.CarsWashed;
                            employeesStats[emp.EmployeeId].TotalAmount += emp.TotalAmount;
                            employeesStats[emp.EmployeeId].Earnings += emp.Earnings;
                        }
                        else
                        {
                            employeesStats[emp.EmployeeId] = new EmployeeWorkReport
                            {
                                EmployeeId = emp.EmployeeId,
                                EmployeeName = emp.EmployeeName,
                                CarsWashed = emp.CarsWashed,
                                TotalAmount = emp.TotalAmount,
                                Earnings = emp.Earnings
                            };
                        }
                    }
                }

                // Обновляем UI
                TotalCarsText.Text = totalCars.ToString();
                TotalRevenueText.Text = $"{totalRevenue:N0} ₽";
                TotalWasherText.Text = $"{totalWasherEarnings:N0} ₽";
                TotalCompanyText.Text = $"{totalCompanyEarnings:N0} ₽";

                DailyReportsList.ItemsSource = dailySummaries;

                // Отображаем зарплату сотрудников в списке
                var salaryList = employeesStats.Values
                    .OrderByDescending(e => e.Earnings)
                    .Select(e => new
                    {
                        e.EmployeeName,
                        e.CarsWashed,
                        e.TotalAmount,
                        e.Earnings
                    }).ToList();
                EmployeesSalaryList.ItemsSource = salaryList;

                // Отображаем детальную информацию
                EmployeesDetailsControl.ItemsSource = salaryList;

                // Показываем контент
                ReportContent.Visibility = Visibility.Visible;
                NoDataText.Visibility = Visibility.Collapsed;

                // Сохраняем для экспорта
                _currentMonthlyReport = new MonthlyReport
                {
                    Year = year,
                    Month = month,
                    TotalCars = totalCars,
                    TotalRevenue = totalRevenue,
                    TotalWasherEarnings = totalWasherEarnings,
                    TotalCompanyEarnings = totalCompanyEarnings,
                    DailyReports = dailySummaries
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка формирования отчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs args)
        {
            if (_currentMonthlyReport == null)
            {
                MessageBox.Show("Сначала сформируйте отчет", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "JSON файлы (*.json)|*.json|CSV файлы (*.csv)|*.csv",
                    DefaultExt = "json",
                    FileName = $"MonthlyReport_{_currentMonthlyReport.Year:0000}-{_currentMonthlyReport.Month:00}"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    string content;
                    if (saveDialog.FilterIndex == 1)
                    {
                        content = Newtonsoft.Json.JsonConvert.SerializeObject(_currentMonthlyReport, Newtonsoft.Json.Formatting.Indented);
                    }
                    else
                    {
                        // CSV формат
                        content = "Дата;Машин;Выручка;Мойщикам;Компании\n";
                        foreach (var day in _currentMonthlyReport.DailyReports)
                        {
                            content += $"{day.Date:dd.MM.yyyy};{day.Cars};{day.Revenue:N0} ₽;{day.WasherEarnings:N0} ₽;{day.CompanyEarnings:N0} ₽\n";
                        }

                        // Добавляем информацию по сотрудникам
                        content += "\nЗАРАБОТНАЯ ПЛАТА СОТРУДНИКОВ\n";
                        content += "Сотрудник;Кол-во машин;Выручка сотрудника;Заработная плата (35%)\n";

                        var allUsers = Core.DB.GetAllUsers();
                        var allReports = new List<ShiftReport>();

                        string reportsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                        if (Directory.Exists(reportsPath))
                        {
                            var reportFiles = Directory.GetFiles(reportsPath, "ShiftReport_*.json");
                            foreach (var file in reportFiles)
                            {
                                try
                                {
                                    var json = File.ReadAllText(file);
                                    var report = Newtonsoft.Json.JsonConvert.DeserializeObject<ShiftReport>(json);
                                    if (report != null && report.Date.Year == _currentMonthlyReport.Year && report.Date.Month == _currentMonthlyReport.Month)
                                    {
                                        allReports.Add(report);
                                    }
                                }
                                catch { }
                            }
                        }

                        var employeesStats = new Dictionary<int, EmployeeWorkReport>();
                        foreach (var report in allReports)
                        {
                            foreach (var emp in report.EmployeesWork)
                            {
                                if (employeesStats.ContainsKey(emp.EmployeeId))
                                {
                                    employeesStats[emp.EmployeeId].CarsWashed += emp.CarsWashed;
                                    employeesStats[emp.EmployeeId].TotalAmount += emp.TotalAmount;
                                    employeesStats[emp.EmployeeId].Earnings += emp.Earnings;
                                }
                                else
                                {
                                    employeesStats[emp.EmployeeId] = new EmployeeWorkReport
                                    {
                                        EmployeeId = emp.EmployeeId,
                                        EmployeeName = emp.EmployeeName,
                                        CarsWashed = emp.CarsWashed,
                                        TotalAmount = emp.TotalAmount,
                                        Earnings = emp.Earnings
                                    };
                                }
                            }
                        }

                        foreach (var emp in employeesStats.Values.OrderByDescending(e => e.Earnings))
                        {
                            content += $"{emp.EmployeeName};{emp.CarsWashed};{emp.TotalAmount:N0} ₽;{emp.Earnings:N0} ₽\n";
                        }

                        content += $"\nИТОГО по сотрудникам;;;{_currentMonthlyReport.TotalWasherEarnings:N0} ₽\n";
                        content += $"\nИТОГО по компании;;;{_currentMonthlyReport.TotalCompanyEarnings:N0} ₽\n";
                    }

                    File.WriteAllText(saveDialog.FileName, content);
                    MessageBox.Show("Отчет успешно экспортирован", "Успешно",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs args)
        {
            Close();
        }
    }
}