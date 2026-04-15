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

        private SqliteDataService _SqliteDataService;
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

        public CustomReportWindow(SqliteDataService SqliteDataService)
        {
            InitializeComponent();
            _SqliteDataService = SqliteDataService;
            DataContext = this;

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
                    MessageBox.Show("Дата начала не может быть позже даты окончания", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // === 1. МАГИЯ БАЗЫ ДАННЫХ ВМЕСТО JSON ===
                var periodReports = _SqliteDataService.GetShiftReportsFromDb(startDate, endDate);
                int newClients = _SqliteDataService.GetNewClientsCount(startDate, endDate);
                int uniqueClients = _SqliteDataService.GetUniqueClientsCount(startDate, endDate);
                // =======================================

                if (!periodReports.Any())
                {
                    MessageBox.Show($"За период с {startDate:dd.MM.yyyy} по {endDate:dd.MM.yyyy} нет закрытых смен",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Итоговые суммы
                var totalCars = periodReports.Sum(r => r.TotalCars);
                var totalRevenue = periodReports.Sum(r => r.TotalRevenue);
                var totalWasherEarnings = periodReports.Sum(r => r.TotalWasherEarnings);
                var totalCompanyEarnings = periodReports.Sum(r => r.TotalCompanyEarnings);
                var totalCashCount = periodReports.Sum(r => r.CashCount);
                var totalCashAmount = periodReports.Sum(r => r.CashAmount);
                var totalCardCount = periodReports.Sum(r => r.CardCount);
                var totalCardAmount = periodReports.Sum(r => r.CardAmount);
                var totalTransferCount = periodReports.Sum(r => r.TransferCount);
                var totalTransferAmount = periodReports.Sum(r => r.TransferAmount);
                var totalQrCount = periodReports.Sum(r => r.QrCount);
                var totalQrAmount = periodReports.Sum(r => r.QrAmount);

                // Дневные сводки
                var dailySummaries = periodReports
                    .OrderBy(r => r.Date)
                    .Select(r => new Models.DailyReportSummary
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

                // Статистика по сотрудникам
                // ИСПОЛЬЗУЕМ НОВЫЙ КЛАСС EmployeeReport для итогового отчета
                var employeesData = new Dictionary<int, EmployeeReport>();

                foreach (var report in periodReports.OrderBy(r => r.Date))
                {
                    // emp прилетает к нам как старый EmployeeWorkReport из одиночной смены
                    foreach (var emp in report.EmployeesWork)
                    {
                        if (!employeesData.ContainsKey(emp.EmployeeId))
                        {
                            // А складываем мы всё в новый EmployeeReport, у которого ЕСТЬ DailyWork
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
                int daysCount = (endDate - startDate).Days + 1;

                // Обновляем UI
                PeriodText.Text = $"Период: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}";
                DaysCountText.Text = $"Всего дней: {daysCount}";
                TotalCarsText.Text = totalCars.ToString();
                TotalRevenueText.Text = $"{totalRevenue:N0} ₽";
                TotalWasherText.Text = $"{totalWasherEarnings:N0} ₽";
                TotalCompanyText.Text = $"{totalCompanyEarnings:N0} ₽";

                // Обновляем стату по клиентам
                if (UniqueClientsText != null) UniqueClientsText.Text = uniqueClients.ToString();
                if (NewClientsText != null) NewClientsText.Text = newClients.ToString();

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
                    CashCount = totalCashCount,
                    CashAmount = totalCashAmount,
                    CardCount = totalCardCount,
                    CardAmount = totalCardAmount,
                    TransferCount = totalTransferCount,
                    TransferAmount = totalTransferAmount,
                    QrCount = totalQrCount,
                    QrAmount = totalQrAmount,
                    UniqueClientsCount = uniqueClients,
                    NewClientsCount = newClients,
                    DailyReports = dailySummaries,
                    EmployeesWork = employeesReport
                };

                CustomCashCountText.Text = totalCashCount.ToString();
                CustomCashAmountText.Text = $"{totalCashAmount:N0} ₽";
                CustomCardCountText.Text = totalCardCount.ToString();
                CustomCardAmountText.Text = $"{totalCardAmount:N0} ₽";
                CustomTransferCountText.Text = totalTransferCount.ToString();
                CustomTransferAmountText.Text = $"{totalTransferAmount:N0} ₽";
                CustomQrCountText.Text = totalQrCount.ToString();
                CustomQrAmountText.Text = $"{totalQrAmount:N0} ₽";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка формирования отчета: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs args)
        {
            if (_currentReport == null)
            {
                MessageBox.Show("Сначала сформируйте отчет", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel файл (*.xlsx)|*.xlsx",
                FileName = $"Выборочный_Отчет_{_currentReport.StartDate:dd.MM.yyyy}-{_currentReport.EndDate:dd.MM.yyyy}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    ExcelExporter.ExportCustomPeriodReport(_currentReport, saveDialog.FileName);
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
