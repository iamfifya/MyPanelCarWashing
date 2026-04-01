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
                var totalCashCount = monthlyReports.Sum(r => r.CashCount);
                var totalCashAmount = monthlyReports.Sum(r => r.CashAmount);
                var totalCardCount = monthlyReports.Sum(r => r.CardCount);
                var totalCardAmount = monthlyReports.Sum(r => r.CardAmount);
                var totalTransferCount = monthlyReports.Sum(r => r.TransferCount);
                var totalTransferAmount = monthlyReports.Sum(r => r.TransferAmount);
                var totalQrCount = monthlyReports.Sum(r => r.QrCount);
                var totalQrAmount = monthlyReports.Sum(r => r.QrAmount);

                var dailySummaries = monthlyReports
                    .OrderBy(r => r.Date)
                    .Select(r => new Models.DailyReportSummary
                    {
                        Date = r.Date,
                        Cars = r.TotalCars,
                        Revenue = r.TotalRevenue,
                        WasherEarnings = r.TotalWasherEarnings,
                        CompanyEarnings = r.TotalCompanyEarnings,
                        CashCount = r.CashCount,
                        CashAmount = r.CashAmount,
                        CardCount = r.CardCount,
                        CardAmount = r.CardAmount,
                        TransferCount = r.TransferCount,
                        TransferAmount = r.TransferAmount,
                        QrCount = r.QrCount,
                        QrAmount = r.QrAmount
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
                    TotalCashCount = totalCashCount,
                    TotalCashAmount = totalCashAmount,
                    TotalCardCount = totalCardCount,
                    TotalCardAmount = totalCardAmount,
                    TotalTransferCount = totalTransferCount,
                    TotalTransferAmount = totalTransferAmount,
                    TotalQrCount = totalQrCount,
                    TotalQrAmount = totalQrAmount,
                    DailyReports = dailySummaries,
                    EmployeesReport = employeesReport

                };

                MonthlyCashCountText.Text = totalCashCount.ToString();
                MonthlyCashAmountText.Text = $"{totalCashAmount:N0} ₽";
                MonthlyCardCountText.Text = totalCardCount.ToString();
                MonthlyCardAmountText.Text = $"{totalCardAmount:N0} ₽";
                MonthlyTransferCountText.Text = totalTransferCount.ToString();
                MonthlyTransferAmountText.Text = $"{totalTransferAmount:N0} ₽";
                MonthlyQrCountText.Text = totalQrCount.ToString();
                MonthlyQrAmountText.Text = $"{totalQrAmount:N0} ₽";
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
                        ExportToExcel(_currentMonthlyReport, saveDialog.FileName);
                        MessageBox.Show($"Отчет успешно экспортирован в Excel\n\n{saveDialog.FileName}",
                            "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else if (extension == ".csv")
                    {
                        ExportToCsv(_currentMonthlyReport, saveDialog.FileName);
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

        private void ExportToExcel(MonthlyReport report, string filePath)
        {
            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add($"Отчет за {report.MonthName}");

                // Заголовок
                worksheet.Cell(1, 1).Value = $"Месячный отчет за {report.MonthName}";
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                worksheet.Range(1, 1, 1, 7).Merge().Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

                // Итоги
                int row = 3;
                worksheet.Cell(row, 1).Value = "Всего машин за месяц:";
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

                // Статистика по способам оплаты
                worksheet.Cell(row, 1).Value = "СТАТИСТИКА ПО СПОСОБАМ ОПЛАТЫ";
                worksheet.Range(row, 1, row, 4).Merge().Style.Font.Bold = true;
                worksheet.Range(row, 1, row, 4).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
                row++;

                worksheet.Cell(row, 1).Value = "Способ оплаты";
                worksheet.Cell(row, 2).Value = "Количество";
                worksheet.Cell(row, 3).Value = "Сумма";
                worksheet.Range(row, 1, row, 3).Style.Font.Bold = true;
                row++;

                worksheet.Cell(row, 1).Value = "💵 Наличные";
                worksheet.Cell(row, 2).Value = report.TotalCashCount;
                worksheet.Cell(row, 3).Value = report.TotalCashAmount;
                worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00 ₽";
                row++;

                worksheet.Cell(row, 1).Value = "💳 Карта";
                worksheet.Cell(row, 2).Value = report.TotalCardCount;
                worksheet.Cell(row, 3).Value = report.TotalCardAmount;
                worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00 ₽";
                row++;

                worksheet.Cell(row, 1).Value = "📱 Перевод";
                worksheet.Cell(row, 2).Value = report.TotalTransferCount;
                worksheet.Cell(row, 3).Value = report.TotalTransferAmount;
                worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00 ₽";
                row++;

                worksheet.Cell(row, 1).Value = "ИТОГО:";
                worksheet.Cell(row, 2).Value = report.TotalCashCount + report.TotalCardCount + report.TotalTransferCount;
                worksheet.Cell(row, 3).Value = report.TotalCashAmount + report.TotalCardAmount + report.TotalTransferAmount;
                worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00 ₽";
                worksheet.Range(row, 1, row, 3).Style.Font.Bold = true;
                row += 2;

                // Таблица по дням
                worksheet.Cell(row, 1).Value = "📅 ОТЧЕТ ПО ДНЯМ";
                worksheet.Range(row, 1, row, 7).Merge().Style.Font.Bold = true;
                worksheet.Range(row, 1, row, 7).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
                row++;

                worksheet.Cell(row, 1).Value = "Дата";
                worksheet.Cell(row, 2).Value = "Машин";
                worksheet.Cell(row, 3).Value = "Выручка";
                worksheet.Cell(row, 4).Value = "Мойщикам";
                worksheet.Cell(row, 5).Value = "Компании";
                worksheet.Cell(row, 6).Value = "Наличные";
                worksheet.Cell(row, 7).Value = "Карта";
                worksheet.Cell(row, 8).Value = "Перевод";
                worksheet.Range(row, 1, row, 8).Style.Font.Bold = true;
                row++;

                foreach (var day in report.DailyReports.OrderBy(d => d.Date))
                {
                    worksheet.Cell(row, 1).Value = day.Date.ToString("dd.MM.yyyy");
                    worksheet.Cell(row, 2).Value = day.Cars;
                    worksheet.Cell(row, 3).Value = day.Revenue;
                    worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00 ₽";
                    worksheet.Cell(row, 4).Value = day.WasherEarnings;
                    worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00 ₽";
                    worksheet.Cell(row, 5).Value = day.CompanyEarnings;
                    worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00 ₽";
                    worksheet.Cell(row, 6).Value = day.CashAmount;
                    worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00 ₽";
                    worksheet.Cell(row, 7).Value = day.CardAmount;
                    worksheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00 ₽";
                    worksheet.Cell(row, 8).Value = day.TransferAmount;
                    worksheet.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00 ₽";
                    row++;
                }

                row += 2;

                // Сводная таблица сотрудников
                worksheet.Cell(row, 1).Value = "👥 СВОДНАЯ СТАТИСТИКА СОТРУДНИКОВ";
                worksheet.Range(row, 1, row, 4).Merge().Style.Font.Bold = true;
                worksheet.Range(row, 1, row, 4).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
                row++;

                worksheet.Cell(row, 1).Value = "Сотрудник";
                worksheet.Cell(row, 2).Value = "Кол-во машин";
                worksheet.Cell(row, 3).Value = "Выручка";
                worksheet.Cell(row, 4).Value = "Заработок (35%)";
                worksheet.Range(row, 1, row, 4).Style.Font.Bold = true;
                row++;

                foreach (var emp in report.EmployeesReport.OrderByDescending(e => e.Earnings))
                {
                    worksheet.Cell(row, 1).Value = emp.EmployeeName;
                    worksheet.Cell(row, 2).Value = emp.CarsWashed;
                    worksheet.Cell(row, 3).Value = emp.TotalAmount;
                    worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00 ₽";
                    worksheet.Cell(row, 4).Value = emp.Earnings;
                    worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00 ₽";
                    row++;
                }

                row++;
                worksheet.Cell(row, 1).Value = "ИТОГО:";
                worksheet.Cell(row, 2).Value = report.EmployeesReport.Sum(e => e.CarsWashed);
                worksheet.Cell(row, 3).Value = report.EmployeesReport.Sum(e => e.TotalAmount);
                worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00 ₽";
                worksheet.Cell(row, 4).Value = report.EmployeesReport.Sum(e => e.Earnings);
                worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00 ₽";
                worksheet.Range(row, 1, row, 4).Style.Font.Bold = true;
                row += 2;

                // Детальная статистика по сотрудникам
                worksheet.Cell(row, 1).Value = "📋 ДЕТАЛЬНАЯ СТАТИСТИКА СОТРУДНИКОВ (ПО ДНЯМ)";
                worksheet.Range(row, 1, row, 4).Merge().Style.Font.Bold = true;
                worksheet.Range(row, 1, row, 4).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
                row++;

                foreach (var emp in report.EmployeesReport.OrderBy(e => e.EmployeeName))
                {
                    row++;
                    worksheet.Cell(row, 1).Value = emp.EmployeeName;
                    worksheet.Range(row, 1, row, 1).Style.Font.Bold = true;
                    worksheet.Range(row, 1, row, 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
                    row++;

                    worksheet.Cell(row, 1).Value = "Дата";
                    worksheet.Cell(row, 2).Value = "Машин";
                    worksheet.Cell(row, 3).Value = "Выручка";
                    worksheet.Cell(row, 4).Value = "Заработок";
                    worksheet.Range(row, 1, row, 4).Style.Font.Bold = true;
                    row++;

                    foreach (var day in emp.DailyWork.OrderBy(d => d.Date))
                    {
                        worksheet.Cell(row, 1).Value = day.Date.ToString("dd.MM.yyyy");
                        worksheet.Cell(row, 2).Value = day.CarsWashed;
                        worksheet.Cell(row, 3).Value = day.TotalAmount;
                        worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00 ₽";
                        worksheet.Cell(row, 4).Value = day.Earnings;
                        worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00 ₽";
                        row++;
                    }

                    row++;
                    worksheet.Cell(row, 1).Value = "Итого:";
                    worksheet.Cell(row, 2).Value = emp.CarsWashed;
                    worksheet.Cell(row, 3).Value = emp.TotalAmount;
                    worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00 ₽";
                    worksheet.Cell(row, 4).Value = emp.Earnings;
                    worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00 ₽";
                    worksheet.Range(row, 1, row, 4).Style.Font.Bold = true;
                    row += 2;
                }

                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(filePath);
            }
        }

        private void ExportToCsv(MonthlyReport report, string filePath)
        {
            var lines = new List<string>();

            lines.Add($"Месячный отчет за {report.MonthName}");
            lines.Add("");
            lines.Add($"Всего машин;{report.TotalCars}");
            lines.Add($"Общая выручка;{report.TotalRevenue:N0} ₽");
            lines.Add($"Выплаты мойщикам (35%);{report.TotalWasherEarnings:N0} ₽");
            lines.Add($"Доход компании (65%);{report.TotalCompanyEarnings:N0} ₽");
            lines.Add("");
            lines.Add($"Наличные;{report.TotalCashCount};{report.TotalCashAmount:N0} ₽");
            lines.Add($"Карта;{report.TotalCardCount};{report.TotalCardAmount:N0} ₽");
            lines.Add($"Перевод;{report.TotalTransferCount};{report.TotalTransferAmount:N0} ₽");
            lines.Add("");
            lines.Add("ОТЧЕТ ПО ДНЯМ");
            lines.Add("Дата;Машин;Выручка;Мойщикам;Компании;Наличные;Карта;Перевод");
            foreach (var day in report.DailyReports.OrderBy(d => d.Date))
            {
                lines.Add($"{day.Date:dd.MM.yyyy};{day.Cars};{day.Revenue:N0} ₽;{day.WasherEarnings:N0} ₽;{day.CompanyEarnings:N0} ₽;{day.CashAmount:N0} ₽;{day.CardAmount:N0} ₽;{day.TransferAmount:N0} ₽");
            }
            lines.Add("");
            lines.Add("СВОДНАЯ СТАТИСТИКА СОТРУДНИКОВ");
            lines.Add("Сотрудник;Машин;Выручка;Заработок");
            foreach (var emp in report.EmployeesReport.OrderByDescending(e => e.Earnings))
            {
                lines.Add($"{emp.EmployeeName};{emp.CarsWashed};{emp.TotalAmount:N0} ₽;{emp.Earnings:N0} ₽");
            }
            lines.Add("");
            lines.Add($"ИТОГО;{report.EmployeesReport.Sum(e => e.CarsWashed)};{report.EmployeesReport.Sum(e => e.TotalAmount):N0} ₽;{report.EmployeesReport.Sum(e => e.Earnings):N0} ₽");
            lines.Add("");
            lines.Add("ДЕТАЛЬНАЯ СТАТИСТИКА СОТРУДНИКОВ (ПО ДНЯМ)");
            foreach (var emp in report.EmployeesReport.OrderBy(e => e.EmployeeName))
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
