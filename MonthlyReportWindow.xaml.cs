using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
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

        private DataService _dataService;
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

        public MonthlyReportWindow(DataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
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

                var totalCars = monthlyReports.Sum(r => r.TotalCars);
                var totalRevenue = monthlyReports.Sum(r => r.TotalRevenue);
                var totalWasherEarnings = monthlyReports.Sum(r => r.TotalWasherEarnings);
                var totalCompanyEarnings = monthlyReports.Sum(r => r.TotalCompanyEarnings);

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

                var employeesData = new Dictionary<int, EmployeeMonthlyReport>();

                foreach (var report in monthlyReports.OrderBy(r => r.Date))
                {
                    foreach (var emp in report.EmployeesWork)
                    {
                        if (!employeesData.ContainsKey(emp.EmployeeId))
                        {
                            employeesData[emp.EmployeeId] = new EmployeeMonthlyReport
                            {
                                EmployeeId = emp.EmployeeId,
                                EmployeeName = emp.EmployeeName,
                                CarsWashed = 0,
                                TotalAmount = 0,
                                Earnings = 0,
                                DailyWork = new List<DailyEmployeeReport>()
                            };
                        }

                        employeesData[emp.EmployeeId].CarsWashed += emp.CarsWashed;
                        employeesData[emp.EmployeeId].TotalAmount += emp.TotalAmount;
                        employeesData[emp.EmployeeId].Earnings += emp.Earnings;

                        employeesData[emp.EmployeeId].DailyWork.Add(new DailyEmployeeReport
                        {
                            Date = report.Date,
                            CarsWashed = emp.CarsWashed,
                            TotalAmount = emp.TotalAmount,
                            Earnings = emp.Earnings
                        });
                    }
                }

                var employeesReport = employeesData.Values.OrderByDescending(e => e.Earnings).ToList();

                TotalCarsText.Text = totalCars.ToString();
                TotalRevenueText.Text = $"{totalRevenue:N0} ₽";
                TotalWasherText.Text = $"{totalWasherEarnings:N0} ₽";
                TotalCompanyText.Text = $"{totalCompanyEarnings:N0} ₽";

                DailyReportsList.ItemsSource = dailySummaries;
                EmployeesSalaryList.ItemsSource = employeesReport;
                EmployeesDetailControl.ItemsSource = employeesReport;

                ReportContent.Visibility = Visibility.Visible;
                NoDataText.Visibility = Visibility.Collapsed;

                _currentMonthlyReport = new MonthlyReport
                {
                    Year = year,
                    Month = month,
                    TotalCars = totalCars,
                    TotalRevenue = totalRevenue,
                    TotalWasherEarnings = totalWasherEarnings,
                    TotalCompanyEarnings = totalCompanyEarnings,
                    DailyReports = dailySummaries,
                    EmployeesReport = employeesReport
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
                    Filter = "Excel файлы (*.xlsx)|*.xlsx|CSV файлы (*.csv)|*.csv|JSON файлы (*.json)|*.json",
                    DefaultExt = "xlsx",
                    FileName = $"MonthlyReport_{_currentMonthlyReport.Year:0000}-{_currentMonthlyReport.Month:00}"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    string extension = System.IO.Path.GetExtension(saveDialog.FileName).ToLower();

                    if (extension == ".xlsx")
                    {
                        ExcelExporter.ExportMonthlyReport(_currentMonthlyReport, saveDialog.FileName);
                        MessageBox.Show($"Отчет успешно экспортирован в Excel\n\n{saveDialog.FileName}",
                            "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else if (extension == ".csv")
                    {
                        ExportMonthlyToCsv(_currentMonthlyReport, saveDialog.FileName);
                        MessageBox.Show($"Отчет успешно экспортирован в CSV\n\n{saveDialog.FileName}",
                            "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else if (extension == ".json")
                    {
                        var json = Newtonsoft.Json.JsonConvert.SerializeObject(_currentMonthlyReport, Newtonsoft.Json.Formatting.Indented);
                        System.IO.File.WriteAllText(saveDialog.FileName, json);
                        MessageBox.Show($"Отчет успешно экспортирован в JSON\n\n{saveDialog.FileName}",
                            "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportMonthlyToCsv(MonthlyReport report, string filePath)
        {
            var lines = new List<string>();

            lines.Add($"Месячный отчет за {report.MonthName}");
            lines.Add("");
            lines.Add("ИТОГОВАЯ СТАТИСТИКА");
            lines.Add($"Всего машин;{report.TotalCars}");
            lines.Add($"Общая выручка;{report.TotalRevenue:N0} ₽");
            lines.Add($"Выплаты мойщикам (35%);{report.TotalWasherEarnings:N0} ₽");
            lines.Add($"Доход компании (65%);{report.TotalCompanyEarnings:N0} ₽");
            lines.Add("");
            lines.Add("СТАТИСТИКА ПО ДНЯМ");
            lines.Add("Дата;Машин;Выручка;Мойщикам;Компании");
            foreach (var day in report.DailyReports)
            {
                lines.Add($"{day.Date:dd.MM.yyyy};{day.Cars};{day.Revenue:N0} ₽;{day.WasherEarnings:N0} ₽;{day.CompanyEarnings:N0} ₽");
            }
            lines.Add("");
            lines.Add("СТАТИСТИКА ПО СОТРУДНИКАМ");
            lines.Add("Сотрудник;Кол-во машин;Выручка сотрудника;Заработная плата (35%)");
            foreach (var emp in report.EmployeesReport)
            {
                lines.Add($"{emp.EmployeeName};{emp.CarsWashed};{emp.TotalAmount:N0} ₽;{emp.Earnings:N0} ₽");
            }
            lines.Add("");
            lines.Add($"ИТОГО;{report.TotalCars};{report.TotalRevenue:N0} ₽;{report.TotalWasherEarnings:N0} ₽");
            lines.Add("");
            lines.Add("ДЕТАЛЬНАЯ СТАТИСТИКА ПО СОТРУДНИКАМ (ПО ДНЯМ)");
            foreach (var emp in report.EmployeesReport)
            {
                lines.Add($"");
                lines.Add($"СОТРУДНИК: {emp.EmployeeName}");
                lines.Add($"Всего: {emp.CarsWashed} машин, выручка: {emp.TotalAmount:N0} ₽, заработок: {emp.Earnings:N0} ₽");
                lines.Add("Дата;Машин;Выручка;Заработок");
                foreach (var day in emp.DailyWork.OrderBy(d => d.Date))
                {
                    lines.Add($"{day.Date:dd.MM.yyyy};{day.CarsWashed};{day.TotalAmount:N0} ₽;{day.Earnings:N0} ₽");
                }
            }

            System.IO.File.WriteAllLines(filePath, lines, System.Text.Encoding.UTF8);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs args)
        {
            Close();
        }
    }
}
