using MyPanelCarWashing.Models;
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

                // Отладочный вывод
                System.Diagnostics.Debug.WriteLine($"Путь к отчетам: {reportsPath}");
                System.Diagnostics.Debug.WriteLine($"Папка существует: {Directory.Exists(reportsPath)}");

                if (!Directory.Exists(reportsPath))
                {
                    System.Diagnostics.Debug.WriteLine("Папка Reports не существует");
                    Reports = new List<ShiftReport>();
                    return;
                }

                var reportFiles = Directory.GetFiles(reportsPath, "ShiftReport_*.json");
                System.Diagnostics.Debug.WriteLine($"Найдено файлов: {reportFiles.Length}");

                var reports = new List<ShiftReport>();

                foreach (var file in reportFiles)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"Загрузка файла: {file}");
                        var json = File.ReadAllText(file);
                        var report = Newtonsoft.Json.JsonConvert.DeserializeObject<ShiftReport>(json);
                        if (report != null)
                        {
                            reports.Add(report);
                            System.Diagnostics.Debug.WriteLine($"Загружен отчет за {report.Date:yyyy-MM-dd}");
                        }
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
                    System.Diagnostics.Debug.WriteLine($"Загружено отчетов: {Reports.Count}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Отчеты не найдены");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки отчетов: {ex.Message}");
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