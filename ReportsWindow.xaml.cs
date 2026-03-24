using MyPanelCarWashing.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace MyPanelCarWashing
{
    public partial class ReportsWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private List<ShiftReport> _reports;
        private ShiftReport _selectedReport;

        public List<ShiftReport> Reports
        {
            get => _reports;
            set
            {
                _reports = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Reports"));
            }
        }

        public ShiftReport SelectedReport
        {
            get => _selectedReport;
            set
            {
                _selectedReport = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedReport"));
            }
        }

        public ReportsWindow()
        {
            InitializeComponent();
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

        private void ReportSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs args)
        {
            SelectedReport = ReportsListBox.SelectedItem as ShiftReport;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs args)
        {
            LoadReports();
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
                    Filter = "JSON файлы (*.json)|*.json|CSV файлы (*.csv)|*.csv",
                    DefaultExt = "json",
                    FileName = $"ShiftReport_{SelectedReport.Date:yyyy-MM-dd}"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    string content;
                    if (saveDialog.FilterIndex == 1)
                    {
                        content = Newtonsoft.Json.JsonConvert.SerializeObject(SelectedReport, Newtonsoft.Json.Formatting.Indented);
                    }
                    else
                    {
                        content = "Дата;Начало;Конец;Машин;Выручка;Сотрудники\n";
                        content += $"{SelectedReport.Date:dd.MM.yyyy};{SelectedReport.StartTime:HH:mm};{SelectedReport.EndTime:HH:mm};";
                        content += $"{SelectedReport.TotalCars};{SelectedReport.TotalRevenue};";
                        content += string.Join(";", SelectedReport.EmployeesWork.Select(e => $"{e.EmployeeName}({e.CarsWashed})"));
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