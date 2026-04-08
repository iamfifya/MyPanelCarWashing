using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MyPanelCarWashing
{
    public partial class ReportsWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private SqliteDataService _SqliteDataService;
        private List<ShiftReport> _reports;
        private ShiftReport _selectedReport;

        public List<ShiftReport> Reports
        {
            get => _reports;
            set
            {
                _reports = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Reports)));
            }
        }

        public ShiftReport SelectedReport
        {
            get => _selectedReport;
            set
            {
                _selectedReport = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedReport)));
            }
        }

        public ReportsWindow(SqliteDataService SqliteDataService)
        {
            InitializeComponent();
            _SqliteDataService = SqliteDataService;
            DataContext = this;
            LoadReports();
        }

        private void LoadReports()
        {
            try
            {
                string reportsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");

                if (!Directory.Exists(reportsPath))
                {
                    Reports = new List<ShiftReport>();
                    return;
                }

                var reportFiles = Directory.GetFiles(reportsPath, "ShiftReport_*.json");
                var reports = new List<ShiftReport>();

                foreach (var file in reportFiles)
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var report = Newtonsoft.Json.JsonConvert.DeserializeObject<ShiftReport>(json);
                        if (report != null)
                            reports.Add(report);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка загрузки {file}: {ex.Message}");
                    }
                }

                Reports = reports.OrderByDescending(r => r.Date).ToList();

                if (Reports.Any())
                {
                    ReportsListBox.ItemsSource = Reports;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки отчетов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReportSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            SelectedReport = ReportsListBox.SelectedItem as ShiftReport;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs args)
        {
            LoadReports();
        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs args)
        {
            string reportsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
            if (Directory.Exists(reportsPath))
            {
                System.Diagnostics.Process.Start("explorer.exe", reportsPath);
            }
            else
            {
                MessageBox.Show("Папка Reports не найдена", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void MonthlyReportButton_Click(object sender, RoutedEventArgs e)
        {
            var monthlyReportWin = new MonthlyReportWindow(_SqliteDataService);
            monthlyReportWin.ShowDialog();
        }
        private void CustomReportButton_Click(object sender, RoutedEventArgs e)
        {
            var customReportWin = new CustomReportWindow(_SqliteDataService);
            customReportWin.ShowDialog();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs args)
        {
            if (SelectedReport == null)
            {
                MessageBox.Show("Выберите отчет для экспорта", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel файлы (*.xlsx)|*.xlsx|CSV файлы (*.csv)|*.csv|JSON файлы (*.json)|*.json",
                    DefaultExt = "xlsx",
                    FileName = $"ShiftReport_{SelectedReport.Date:yyyy-MM-dd}"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    string extension = System.IO.Path.GetExtension(saveDialog.FileName).ToLower();

                    if (extension == ".xlsx")
                    {
                        ExcelExporter.ExportShiftReport(SelectedReport, saveDialog.FileName);
                        MessageBox.Show($"Отчет успешно экспортирован в Excel\n\n{saveDialog.FileName}",
                            "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else if (extension == ".csv")
                    {
                        ExportToCsv(SelectedReport, saveDialog.FileName);
                        MessageBox.Show($"Отчет успешно экспортирован в CSV\n\n{saveDialog.FileName}",
                            "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else if (extension == ".json")
                    {
                        var json = Newtonsoft.Json.JsonConvert.SerializeObject(SelectedReport, Newtonsoft.Json.Formatting.Indented);
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

        private void ExportToCsv(ShiftReport report, string filePath)
        {
            var lines = new List<string>();

            lines.Add("Дата;Время начала;Время окончания;Машин;Выручка;Мойщикам;Компании;Примечание");
            lines.Add($"{report.Date:dd.MM.yyyy};{report.StartTime:HH:mm};{report.EndTime:HH:mm};" +
                      $"{report.TotalCars};{report.TotalRevenue:N0} ₽;{report.TotalWasherEarnings:N0} ₽;" +
                      $"{report.TotalCompanyEarnings:N0} ₽;{report.Notes}");

            lines.Add("");
            lines.Add("Сотрудник;Машин;Выручка;Заработок (35%)");

            foreach (var emp in report.EmployeesWork)
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
}
