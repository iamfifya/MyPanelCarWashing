using ClosedXML.Excel;
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

        private SqliteDataService _SqliteDataService;
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

        public MonthlyReportWindow(SqliteDataService SqliteDataService)
        {
            InitializeComponent();
            _SqliteDataService = SqliteDataService;
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

                // === 1. ПОЛУЧАЕМ ДАННЫЕ ИЗ БД В САМОМ НАЧАЛЕ ===
                DateTime startOfMonth = new DateTime(year, month, 1);
                DateTime endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                var monthReports = _SqliteDataService.GetShiftReportsFromDb(startOfMonth, endOfMonth);
                int newClients = _SqliteDataService.GetNewClientsCount(startOfMonth, endOfMonth);
                int uniqueClients = _SqliteDataService.GetUniqueClientsCount(startOfMonth, endOfMonth);
                // ===============================================

                if (!monthReports.Any())
                {
                    MessageBox.Show($"За {selectedDate:MMMM yyyy} нет закрытых смен", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 2. Итоговые суммы
                var totalCars = monthReports.Sum(r => r.TotalCars);
                var totalRevenue = monthReports.Sum(r => r.TotalRevenue);
                var totalWasherEarnings = monthReports.Sum(r => r.TotalWasherEarnings);
                var totalCompanyEarnings = monthReports.Sum(r => r.TotalCompanyEarnings);

                var totalCashCount = monthReports.Sum(r => r.CashCount);
                var totalCashAmount = monthReports.Sum(r => r.CashAmount);
                var totalCardCount = monthReports.Sum(r => r.CardCount);
                var totalCardAmount = monthReports.Sum(r => r.CardAmount);
                var totalTransferCount = monthReports.Sum(r => r.TransferCount);
                var totalTransferAmount = monthReports.Sum(r => r.TransferAmount);
                var totalQrCount = monthReports.Sum(r => r.QrCount);
                var totalQrAmount = monthReports.Sum(r => r.QrAmount);

                // 3. Дневные сводки
                var dailySummaries = monthReports
                    .OrderBy(r => r.Date)
                    .Select(r => new DailyReportSummary
                    {
                        Date = r.Date,
                        TotalCars = r.TotalCars,
                        TotalRevenue = r.TotalRevenue,
                        TotalWasherEarnings = r.TotalWasherEarnings,
                        TotalCompanyEarnings = r.TotalCompanyEarnings,
                        CashCount = r.CashCount,
                        CashAmount = r.CashAmount,
                        CardCount = r.CardCount,
                        CardAmount = r.CardAmount,
                        TransferCount = r.TransferCount,
                        TransferAmount = r.TransferAmount,
                        QrCount = r.QrCount,
                        QrAmount = r.QrAmount
                    }).ToList();

                // 4. Статистика по сотрудникам
                var employeesData = new Dictionary<int, EmployeeReport>();

                foreach (var report in monthReports.OrderBy(r => r.Date))
                {
                    foreach (var emp in report.EmployeesWork)
                    {
                        if (!employeesData.ContainsKey(emp.EmployeeId))
                        {
                            employeesData[emp.EmployeeId] = new EmployeeReport
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

                // 5. Обновляем UI
                TotalCarsText.Text = totalCars.ToString();
                TotalRevenueText.Text = $"{totalRevenue:N0} ₽";
                TotalWasherText.Text = $"{totalWasherEarnings:N0} ₽";
                TotalCompanyText.Text = $"{totalCompanyEarnings:N0} ₽";

                DailyReportsList.ItemsSource = dailySummaries;
                EmployeesSalaryList.ItemsSource = employeesReport;
                EmployeesDetailControl.ItemsSource = employeesReport;

                ReportContent.Visibility = Visibility.Visible;
                NoDataText.Visibility = Visibility.Collapsed;

                // 6. Сохраняем в модель для экспорта 
                _currentMonthlyReport = new MonthlyReport
                {
                    Year = year,
                    Month = month,
                    TotalCars = totalCars,
                    TotalRevenue = totalRevenue,
                    TotalWasherEarnings = totalWasherEarnings,
                    TotalCompanyEarnings = totalCompanyEarnings,
                    UniqueClientsCount = uniqueClients,
                    NewClientsCount = newClients,
                    CashCount = totalCashCount,
                    CashAmount = totalCashAmount,
                    CardCount = totalCardCount,
                    CardAmount = totalCardAmount,
                    TransferCount = totalTransferCount,
                    TransferAmount = totalTransferAmount,
                    QrCount = totalQrCount,
                    QrAmount = totalQrAmount,
                    DailyReports = dailySummaries,
                    EmployeesWork = employeesReport
                };

                UniqueClientsText.Text = uniqueClients.ToString();
                NewClientsText.Text = newClients.ToString();

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
                MessageBox.Show($"Ошибка формирования отчета: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs args)
        {
            if (_currentMonthlyReport == null)
            {
                MessageBox.Show("Сначала сформируйте отчет", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel файл (*.xlsx)|*.xlsx",
                FileName = $"Месячный_Отчет_{_currentMonthlyReport.MonthName}_{_currentMonthlyReport.Year}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    ExcelExporter.ExportMonthlyReport(_currentMonthlyReport, saveDialog.FileName);
                    MessageBox.Show("Отчет успешно экспортирован!", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при экспорте:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs args)
        {
            Close();
        }
    }
}
