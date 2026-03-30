using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MyPanelCarWashing
{
    public partial class ScheduleWindow : Window
    {
        private DataService _dataService;
        private DateTime _currentDate;
        private List<EmployeeSchedule> _scheduleData;
        private Dictionary<int, Border> _dayHeaders = new Dictionary<int, Border>();
        private Dictionary<string, Border> _cells = new Dictionary<string, Border>();

        public ScheduleWindow(DataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
            _currentDate = DateTime.Now;
            LoadSchedule();
        }

        private void LoadSchedule()
        {
            // 1. Загружаем то, что сохранено в файле
            _scheduleData = _dataService.GetSchedule(_currentDate.Year, _currentDate.Month) ?? new List<EmployeeSchedule>();

            // 2. Получаем актуальный список ВСЕХ сотрудников (админы + мойщики)
            var actualEmployees = _dataService.GetAllUsers().ToList();

            if (!_scheduleData.Any())
            {
                // Если файла нет вообще — создаем с нуля
                CreateDefaultSchedule();
            }
            else
            {
                // Если файл есть, проверяем, есть ли все сотрудники (например, Анастасия)
                foreach (var emp in actualEmployees)
                {
                    if (!_scheduleData.Any(s => s.EmployeeId == emp.Id))
                    {
                        string empName = !string.IsNullOrWhiteSpace(emp.FullName) ? emp.FullName : emp.Login;
                        _scheduleData.Add(new EmployeeSchedule
                        {
                            EmployeeId = emp.Id,
                            EmployeeName = empName,
                            Position = emp.IsAdmin ? "Администратор" : "Мойщик",
                            Days = new Dictionary<int, string>()
                        });
                    }
                }
            }

            MonthYearText.Text = _currentDate.ToString("MMMM yyyy");
            BuildScheduleTable();
        }

        private void CreateDefaultSchedule()
        {
            var actualEmployees = _dataService.GetAllUsers().ToList();
            _scheduleData = new List<EmployeeSchedule>();

            foreach (var emp in actualEmployees)
            {
                string empName = !string.IsNullOrWhiteSpace(emp.FullName) ? emp.FullName : emp.Login;
                var empSchedule = new EmployeeSchedule
                {
                    EmployeeId = emp.Id,
                    EmployeeName = empName,
                    Position = emp.IsAdmin ? "Администратор" : "Мойщик",
                    Days = new Dictionary<int, string>()
                };

                int daysInMonth = DateTime.DaysInMonth(_currentDate.Year, _currentDate.Month);
                for (int i = 1; i <= daysInMonth; i++)
                {
                    empSchedule.Days[i] = ""; // По умолчанию пустые дни
                }

                _scheduleData.Add(empSchedule);
            }
        }

        private void BuildScheduleTable()
        {
            // Полная очистка сетки
            ScheduleGrid.Children.Clear();
            ScheduleGrid.RowDefinitions.Clear();

            // Восстанавливаем первую колонку
            while (ScheduleGrid.ColumnDefinitions.Count > 1)
                ScheduleGrid.ColumnDefinitions.RemoveAt(1);

            while (HeaderGrid.ColumnDefinitions.Count > 1)
            {
                HeaderGrid.Children.RemoveAt(HeaderGrid.Children.Count - 1);
                HeaderGrid.ColumnDefinitions.RemoveAt(1);
            }

            int daysInMonth = DateTime.DaysInMonth(_currentDate.Year, _currentDate.Month);
            _dayHeaders.Clear();
            _cells.Clear();

            // Отрисовка шапки с днями
            for (int day = 1; day <= daysInMonth; day++)
            {
                // Заменяем фиксированные 45 пикселей на "резиновые" (1*)
                HeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                ScheduleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                Border dayBorder = new Border
                {
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#34495E")),
                    BorderThickness = new Thickness(1, 0, 0, 0),
                    Background = Brushes.Transparent
                };

                DateTime date = new DateTime(_currentDate.Year, _currentDate.Month, day);
                bool isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;

                TextBlock dayText = new TextBlock
                {
                    Text = day.ToString(),
                    Style = (Style)FindResource("DayHeaderCellStyle"),
                    Foreground = isWeekend ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")) : Brushes.White
                };

                dayBorder.Child = dayText;
                Grid.SetColumn(dayBorder, day);
                HeaderGrid.Children.Add(dayBorder);
                _dayHeaders[day] = dayBorder;
            }

            // Отрисовка сотрудников
            for (int i = 0; i < _scheduleData.Count; i++)
            {
                // ВАЖНО: жестко задаем высоту строки, чтобы они не растягивались и не было пустот!
                ScheduleGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });

                var emp = _scheduleData[i];

                // Ячейка с именем
                Border nameBorder = new Border
                {
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(0, 0, 1, 1),
                    Background = Brushes.White
                };

                TextBlock nameText = new TextBlock
                {
                    Text = emp.EmployeeName,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 0, 0),
                    FontWeight = FontWeights.Medium,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C3E50"))
                };

                nameBorder.Child = nameText;
                Grid.SetRow(nameBorder, i);
                Grid.SetColumn(nameBorder, 0);
                ScheduleGrid.Children.Add(nameBorder);

                // Ячейки рабочих дней
                for (int day = 1; day <= daysInMonth; day++)
                {
                    string cellKey = $"{emp.EmployeeId}_{day}";
                    string dayValue = emp.Days.ContainsKey(day) ? emp.Days[day] : "";

                    Border cellBorder = new Border
                    {
                        BorderBrush = Brushes.LightGray,
                        BorderThickness = new Thickness(0, 0, 1, 1),
                        Background = GetColorForShift(dayValue),
                        Cursor = Cursors.Hand,
                        Tag = cellKey
                    };

                    TextBlock cellText = new TextBlock
                    {
                        Text = dayValue,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.DarkSlateGray
                    };

                    cellBorder.Child = cellText;
                    cellBorder.MouseLeftButtonDown += Cell_MouseLeftButtonDown;
                    cellBorder.MouseRightButtonDown += Cell_MouseRightButtonDown;

                    Grid.SetRow(cellBorder, i);
                    Grid.SetColumn(cellBorder, day);
                    ScheduleGrid.Children.Add(cellBorder);

                    _cells[cellKey] = cellBorder;
                }
            }
        }

        private Brush GetColorForShift(string shiftType)
        {
            switch (shiftType)
            {
                case "Р": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A9DFBF"));
                case "В": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FAD7A1"));
                default: return Brushes.White;
            }
        }

        private void Cell_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag != null)
            {
                string key = border.Tag.ToString();
                var parts = key.Split('_');
                int empId = int.Parse(parts[0]);
                int day = int.Parse(parts[1]);

                var emp = _scheduleData.First(s => s.EmployeeId == empId);
                string current = emp.Days.ContainsKey(day) ? emp.Days[day] : "";

                // Переключение: Пусто -> Р -> В -> Пусто
                string next = current == "" ? "Р" : (current == "Р" ? "В" : "");
                emp.Days[day] = next;

                if (border.Child is TextBlock textBlock) textBlock.Text = next;
                border.Background = GetColorForShift(next);
            }
        }

        private void Cell_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag != null)
            {
                string key = border.Tag.ToString();
                var parts = key.Split('_');
                int empId = int.Parse(parts[0]);
                int day = int.Parse(parts[1]);

                var emp = _scheduleData.First(s => s.EmployeeId == empId);
                emp.Days[day] = "";

                if (border.Child is TextBlock textBlock) textBlock.Text = "";
                border.Background = Brushes.White;
            }
        }

        private void PrevMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentDate = _currentDate.AddMonths(-1);
            LoadSchedule();
        }

        private void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentDate = _currentDate.AddMonths(1);
            LoadSchedule();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _dataService.SaveSchedule(_currentDate.Year, _currentDate.Month, _scheduleData);
            MessageBox.Show($"График на {_currentDate:MMMM yyyy} сохранен", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void TemplateButton_Click(object sender, RoutedEventArgs e)
        {
            CreateDefaultSchedule();
            BuildScheduleTable();
            MessageBox.Show("Шаблон графика создан (все сотрудники загружены)", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
