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
    public partial class CustomReportWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private DataService _dataService;
        private DateTime _startDate;
        private DateTime _endDate;
        private CustomPeriodReport _currentReport;

        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StartDate)));
            }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EndDate)));
            }
        }

        public CustomReportWindow(DataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
            DataContext = this;

            // Устанавливаем даты по умолчанию (последние 7 дней)
            StartDate = DateTime.Now.AddDays(-6);
            EndDate = DateTime.Now;

            StartDatePicker.SelectedDate = StartDate;
            EndDatePicker.SelectedDate = EndDate;
        }

        private void GenerateReportButton_Click(object sender, RoutedEventArgs args)
        {
            try
            {
                DateTime startDate = StartDatePicker.SelectedDate ?? StartDate;
                DateTime endDate = EndDatePicker.SelectedDate ?? EndDate;

                if (startDate > endDate)
                {
                    MessageBox.Show("Дата начала не может быть позже даты окончания", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Загружаем все дневные отчеты за период
                string reportsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                if (!Directory.Exists(reportsPath))
                {
                    MessageBox.Show("Отчеты не найдены", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var reportFiles = Directory.GetFiles(reportsPath, "ShiftReport_*.json");
                var periodReports = new List<ShiftReport>();

                foreach (var file in reportFiles)
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var report = Newtonsoft.Json.JsonConvert.DeserializeObject<ShiftReport>(json);
                        if (report != null && report.Date.Date >= startDate.Date && report.Date.Date <= endDate.Date)
                        {
                            periodReports.Add(report);
                        }
                    }
                    catch { }
                }

                if (!periodReports.Any())
                {
                    MessageBox.Show($"За период с {startDate:dd.MM.yyyy} по {endDate:dd.MM.yyyy} нет отчетов",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Формируем отчет
                var totalCars = periodReports.Sum(r => r.TotalCars);
                var totalRevenue = periodReports.Sum(r => r.TotalRevenue);
                var totalWasherEarnings = periodReports.Sum(r => r.TotalWasherEarnings);
                var totalCompanyEarnings = periodReports.Sum(r => r.TotalCompanyEarnings);

                // Дневные сводки
                var dailySummaries = periodReports
                    .OrderBy(r => r.Date)
                    .Select(r => new DailyReportSummary
                    {
                        Date = r.Date,
                        Cars = r.TotalCars,
                        Revenue = r.TotalRevenue,
                        WasherEarnings = r.TotalWasherEarnings,
                        CompanyEarnings = r.TotalCompanyEarnings
                    }).ToList();

                // Статистика по сотрудникам за период
                var employeesData = new Dictionary<int, EmployeeMonthlyReport>();

                foreach (var report in periodReports.OrderBy(r => r.Date))
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
                int daysCount = (endDate - startDate).Days + 1;

                // Обновляем UI
                PeriodText.Text = $"Период: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}";
                DaysCountText.Text = $"Всего дней: {daysCount}";
                TotalCarsText.Text = totalCars.ToString();
                TotalRevenueText.Text = $"{totalRevenue:N0} ₽";
                TotalWasherText.Text = $"{totalWasherEarnings:N0} ₽";
                TotalCompanyText.Text = $"{totalCompanyEarnings:N0} ₽";

                DailyReportsList.ItemsSource = dailySummaries;
                EmployeesSalaryList.ItemsSource = employeesReport;
                EmployeesDetailControl.ItemsSource = employeesReport;

                ReportContent.Visibility = Visibility.Visible;
                NoDataText.Visibility = Visibility.Collapsed;

                // Сохраняем для экспорта
                _currentReport = new CustomPeriodReport
                {
                    StartDate = startDate,
                    EndDate = endDate,
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
            if (_currentReport == null)
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
                    FileName = $"CustomReport_{_currentReport.StartDate:yyyy-MM-dd}_to_{_currentReport.EndDate:yyyy-MM-dd}"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    string extension = System.IO.Path.GetExtension(saveDialog.FileName).ToLower();

                    if (extension == ".xlsx")
                    {
                        ExportToExcel(_currentReport, saveDialog.FileName);
                        MessageBox.Show($"Отчет успешно экспортирован в Excel\n\n{saveDialog.FileName}",
                            "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else if (extension == ".csv")
                    {
                        ExportToCsv(_currentReport, saveDialog.FileName);
                        MessageBox.Show($"Отчет успешно экспортирован в CSV\n\n{saveDialog.FileName}",
                            "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else if (extension == ".json")
                    {
                        var json = Newtonsoft.Json.JsonConvert.SerializeObject(_currentReport, Newtonsoft.Json.Formatting.Indented);
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

        private void ExportToExcel(CustomPeriodReport report, string filePath)
        {
            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add($"Отчет {report.StartDate:dd.MM.yyyy}-{report.EndDate:dd.MM.yyyy}");

                // Заголовок
                worksheet.Cell(1, 1).Value = $"Отчет за период с {report.StartDate:dd.MM.yyyy} по {report.EndDate:dd.MM.yyyy}";
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                worksheet.Range(1, 1, 1, 5).Merge().Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

                // Итоги
                int row = 3;
                worksheet.Cell(row, 1).Value = "Всего машин:";
                worksheet.Cell(row, 2).Value = report.TotalCars;
                row++;
                worksheet.Cell(row, 1).Value = "Общая выручка:";
                worksheet.Cell(row, 2).Value = report.TotalRevenue;
                worksheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00 ₽";
                row++;
                worksheet.Cell(row, 1).Value = "Выплаты мойщикам (35%):";
                worksheet.Cell(row, 2).Value = report.TotalWasherEarnings;
                worksheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00 ₽";
                row++;
                worksheet.Cell(row, 1).Value = "Доход компании (65%):";
                worksheet.Cell(row, 2).Value = report.TotalCompanyEarnings;
                worksheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00 ₽";
                row += 2;

                // Таблица по дням
                worksheet.Cell(row, 1).Value = "Дата";
                worksheet.Cell(row, 2).Value = "Машин";
                worksheet.Cell(row, 3).Value = "Выручка";
                worksheet.Cell(row, 4).Value = "Мойщикам";
                worksheet.Cell(row, 5).Value = "Компании";
                worksheet.Range(row, 1, row, 5).Style.Font.Bold = true;
                row++;

                foreach (var day in report.DailyReports)
                {
                    worksheet.Cell(row, 1).Value = day.Date.ToString("dd.MM.yyyy");
                    worksheet.Cell(row, 2).Value = day.Cars;
                    worksheet.Cell(row, 3).Value = day.Revenue;
                    worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00 ₽";
                    worksheet.Cell(row, 4).Value = day.WasherEarnings;
                    worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00 ₽";
                    worksheet.Cell(row, 5).Value = day.CompanyEarnings;
                    worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00 ₽";
                    row++;
                }

                row += 2;

                // Таблица сотрудников
                worksheet.Cell(row, 1).Value = "Сотрудник";
                worksheet.Cell(row, 2).Value = "Машин";
                worksheet.Cell(row, 3).Value = "Выручка";
                worksheet.Cell(row, 4).Value = "Заработок (35%)";
                worksheet.Range(row, 1, row, 4).Style.Font.Bold = true;
                row++;

                foreach (var emp in report.EmployeesReport)
                {
                    worksheet.Cell(row, 1).Value = emp.EmployeeName;
                    worksheet.Cell(row, 2).Value = emp.CarsWashed;
                    worksheet.Cell(row, 3).Value = emp.TotalAmount;
                    worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00 ₽";
                    worksheet.Cell(row, 4).Value = emp.Earnings;
                    worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00 ₽";
                    row++;
                }

                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(filePath);
            }
        }

        private void ExportToCsv(CustomPeriodReport report, string filePath)
        {
            var lines = new List<string>();

            lines.Add($"Отчет за период с {report.StartDate:dd.MM.yyyy} по {report.EndDate:dd.MM.yyyy}");
            lines.Add("");
            lines.Add($"Всего машин;{report.TotalCars}");
            lines.Add($"Общая выручка;{report.TotalRevenue:N0} ₽");
            lines.Add($"Выплаты мойщикам (35%);{report.TotalWasherEarnings:N0} ₽");
            lines.Add($"Доход компании (65%);{report.TotalCompanyEarnings:N0} ₽");
            lines.Add("");
            lines.Add("Дата;Машин;Выручка;Мойщикам;Компании");
            foreach (var day in report.DailyReports)
            {
                lines.Add($"{day.Date:dd.MM.yyyy};{day.Cars};{day.Revenue:N0} ₽;{day.WasherEarnings:N0} ₽;{day.CompanyEarnings:N0} ₽");
            }
            lines.Add("");
            lines.Add("Сотрудник;Машин;Выручка;Заработок");
            foreach (var emp in report.EmployeesReport)
            {
                lines.Add($"{emp.EmployeeName};{emp.CarsWashed};{emp.TotalAmount:N0} ₽;{emp.Earnings:N0} ₽");
            }

            System.IO.File.WriteAllLines(filePath, lines, System.Text.Encoding.UTF8);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs args)
        {
            Close();
        }
    }

    // Класс для выборочного отчета
    public class CustomPeriodReport
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalCars { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalWasherEarnings { get; set; }
        public decimal TotalCompanyEarnings { get; set; }
        public List<DailyReportSummary> DailyReports { get; set; } = new List<DailyReportSummary>();
        public List<EmployeeMonthlyReport> EmployeesReport { get; set; } = new List<EmployeeMonthlyReport>();
    }
}
