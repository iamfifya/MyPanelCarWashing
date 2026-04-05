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
        private bool _isDataModified = false; // Флаг изменения данных

        public ScheduleWindow(DataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
            _currentDate = DateTime.Now;
            LoadSchedule();
        }

        private void LoadSchedule()
        {
            // Загружаем существующий график, НЕ создаем автоматически
            _scheduleData = _dataService.GetSchedule(_currentDate.Year, _currentDate.Month);

            if (_scheduleData == null || !_scheduleData.Any())
            {
                // Если графика нет, показываем пустую таблицу
                _scheduleData = new List<EmployeeSchedule>();
                System.Diagnostics.Debug.WriteLine($"График на {_currentDate:MMMM yyyy} не найден");

                // Показываем сообщение пользователю
                MessageBox.Show($"График на {_currentDate:MMMM yyyy} не найден.\n\n" +
                    "Нажмите кнопку '📋 Шаблон', чтобы создать график по умолчанию.",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Загружен существующий график для {_scheduleData.Count} сотрудников");
            }

            MonthYearText.Text = _currentDate.ToString("MMMM yyyy");
            BuildScheduleTable();
            _isDataModified = false;
            UpdateSaveButtonState();
        }

        private void UpdateSaveButtonState()
        {
            // Кнопка сохранения активна только если есть изменения И есть данные
            SaveButton.IsEnabled = _isDataModified && _scheduleData.Any();
        }

        private void Cell_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border?.Tag == null) return;

            dynamic tag = border.Tag;
            int employeeId = tag.EmployeeId;
            int day = tag.Day;
            string currentStatus = tag.Status;

            // Переключение только между "р" и "в"
            string newStatus = currentStatus == "р" ? "в" : "р";

            // Обновляем данные
            var employeeSchedule = _scheduleData.FirstOrDefault(s => s.EmployeeId == employeeId);
            if (employeeSchedule != null)
            {
                employeeSchedule.Days[day] = newStatus;
                border.Tag = new { EmployeeId = employeeId, Day = day, Status = newStatus };

                // Отмечаем, что данные изменены
                if (!_isDataModified)
                {
                    _isDataModified = true;
                    UpdateSaveButtonState();
                }
            }

            e.Handled = true;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_scheduleData.Any())
            {
                MessageBox.Show("Нет данных для сохранения. Сначала создайте шаблон.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _dataService.SaveSchedule(_currentDate.Year, _currentDate.Month, _scheduleData);
            _isDataModified = false;
            UpdateSaveButtonState();

            MessageBox.Show($"График на {_currentDate:MMMM yyyy} сохранен", "Успешно",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void TemplateButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Создать шаблон графика?\n\n" +
                "Текущий график будет заменен новым.\n\n" +
                "Шаблон:\n" +
                "• Администраторы: чередование через день\n" +
                "• Мойщики: 6 человек, каждый день работают 3",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                CreateDefaultSchedule();
                BuildScheduleTable();
                _isDataModified = true; // Шаблон считается изменением
                UpdateSaveButtonState();

                MessageBox.Show("Шаблон графика создан.\n\nНе забудьте сохранить изменения!",
                    "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void PrevMonth_Click(object sender, RoutedEventArgs e)
        {
            if (_isDataModified)
            {
                var result = MessageBox.Show("У вас есть несохраненные изменения.\n\n" +
                    "Перейти к другому месяцу без сохранения?",
                    "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            _currentDate = _currentDate.AddMonths(-1);
            LoadSchedule();
        }

        private void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            if (_isDataModified)
            {
                var result = MessageBox.Show("У вас есть несохраненные изменения.\n\n" +
                    "Перейти к другому месяцу без сохранения?",
                    "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            _currentDate = _currentDate.AddMonths(1);
            LoadSchedule();
        }


        private void CreateDefaultSchedule()
        {
            var employees = _dataService.GetAllUsers();
            var daysInMonth = DateTime.DaysInMonth(_currentDate.Year, _currentDate.Month);

            // Получаем график предыдущего месяца
            var prevMonthDate = _currentDate.AddMonths(-1);
            var prevMonthSchedule = _dataService.GetSchedule(prevMonthDate.Year, prevMonthDate.Month);

            // Разделяем сотрудников
            var admins = employees.Where(e => e.IsAdmin).OrderBy(e => e.Id).ToList();
            var workers = employees.Where(e => !e.IsAdmin).OrderBy(e => e.Id).ToList();

            _scheduleData = new List<EmployeeSchedule>();

            // ========== АДМИНИСТРАТОРЫ (2 дня работы / 2 дня отдыха) ==========
            // Цикл: Р-Р-В-В (4 дня)
            for (int i = 0; i < admins.Count; i++)
            {

                var admin = admins[i];
                var empSchedule = new EmployeeSchedule
                {
                    EmployeeId = admin.Id,
                    EmployeeName = admin.FullName,
                    Position = "Администратор",
                    Days = new Dictionary<int, string>()
                };

                // Сдвиг фазы для разных админов, чтобы они не всегда совпадали
                int phaseShift = i * 2; // Каждый следующий админ сдвинут на 2 дня

                // Учитываем последний день предыдущего месяца для непрерывности цикла
                var prevSchedule = prevMonthSchedule?.FirstOrDefault(s => s.EmployeeId == admin.Id);
                int carryOver = 0;
                if (prevSchedule != null && prevSchedule.Days.Any())
                {
                    var lastDayPrev = DateTime.DaysInMonth(prevMonthDate.Year, prevMonthDate.Month);
                    for (int d = lastDayPrev; d >= 1; d--)
                    {
                        if (prevSchedule.Days.ContainsKey(d) && !string.IsNullOrEmpty(prevSchedule.Days[d]))
                        {
                            // Считаем позицию в цикле 0-3: Р=0,1; В=2,3
                            string last = prevSchedule.Days[d].ToUpper();
                            if (last == "Р") carryOver = 0;
                            else if (last == "В") carryOver = 2;
                            else if (last == "П") carryOver = 0; // Пропуск не влияет на цикл
                            break;
                        }
                    }
                }

                for (int day = 1; day <= daysInMonth; day++)
                {
                    // Позиция в 4-дневном цикле (0,1,2,3)
                    int cyclePos = (day + phaseShift + carryOver) % 4;

                    // 0,1 = работаем; 2,3 = отдыхаем
                    empSchedule.Days[day] = (cyclePos <= 1) ? "Р" : "В";
                }

                _scheduleData.Add(empSchedule);
            }

            // ========== МОЙЩИКИ (6 человек, каждый день 3 на работе) ==========
            // Цикл 6 дней: дни 1-3 работают мойщики 1-3, дни 4-6 работают мойщики 4-6
            // Сдвиг между мойщиками - 1 день

            for (int i = 0; i < workers.Count; i++)
            {
                var worker = workers[i];
                var empSchedule = new EmployeeSchedule
                {
                    EmployeeId = worker.Id,
                    EmployeeName = worker.FullName,
                    Position = "Мойщик",
                    Days = new Dictionary<int, string>()
                };

                // Сдвиг: каждый следующий мойщик начинает на 1 день позже
                int shift = i % 6; // 0,1,2,3,4,5

                // Учитываем предыдущий месяц
                var prevSchedule = prevMonthSchedule?.FirstOrDefault(s => s.EmployeeId == worker.Id);
                int prevOffset = 0;
                if (prevSchedule != null && prevSchedule.Days.Any())
                {
                    var lastDayPrevMonth = DateTime.DaysInMonth(prevMonthDate.Year, prevMonthDate.Month);
                    int lastActualDay = lastDayPrevMonth;
                    while (lastActualDay > 0 && !prevSchedule.Days.ContainsKey(lastActualDay))
                    {
                        lastActualDay--;
                    }
                    if (lastActualDay > 0 && prevSchedule.Days[lastActualDay] == "р")
                    {
                        // Если последний день был рабочим, нужно продолжить цикл
                        // Определяем, сколько дней он уже отработал в этом цикле
                        // Упрощенно: добавляем смещение
                        prevOffset = 1;
                    }
                }

                int totalShift = (shift + prevOffset) % 6;

                for (int day = 1; day <= daysInMonth; day++)
                {
                    // Работает, если день входит в его 3-дневную рабочую смену
                    int cyclePosition = (day + totalShift) % 6;
                    // cyclePosition 0,1,2 - работает; 3,4,5 - отдыхает
                    empSchedule.Days[day] = (cyclePosition >= 0 && cyclePosition <= 2) ? "р" : "в";
                }

                _scheduleData.Add(empSchedule);
                System.Diagnostics.Debug.WriteLine($"Мойщик {worker.FullName}: сдвиг={shift}, работает в дни: {string.Join(",", Enumerable.Range(1, 31).Where(d => ((d + shift) % 6) <= 2).Take(10))}...");
            }

            System.Diagnostics.Debug.WriteLine($"Создан график для {_scheduleData.Count} сотрудников");

            // Проверяем количество мойщиков в первый день
            var firstDayWorkers = _scheduleData
                .Where(s => s.Position == "Мойщик" && s.Days[1] == "р")
                .Count();
            System.Diagnostics.Debug.WriteLine($"В первый день работает {firstDayWorkers} мойщиков (должно быть 3)");
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
            switch (shiftType?.ToUpper())
            {
                case "Р": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A9DFBF")); // Зелёный
                case "В": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FAD7A1")); // Оранжевый
                case "П": // Бледно-красный для пропуска
                    return new SolidColorBrush(Color.FromArgb(60, 231, 76, 60)); // #E74C3C с прозрачностью 60/255
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
                string current = emp.Days.ContainsKey(day) ? emp.Days[day].ToUpper() : "";

                // Цикл: Пусто → Р → В → П → Пусто
                string next;
                if (string.IsNullOrEmpty(current)) next = "Р";
                else if (current == "Р") next = "В";
                else if (current == "В") next = "П";
                else next = ""; // "П" → пусто

                emp.Days[day] = next;

                if (border.Child is TextBlock textBlock)
                    textBlock.Text = next;
                border.Background = GetColorForShift(next);

                // Отмечаем изменение
                if (!_isDataModified)
                {
                    _isDataModified = true;
                    UpdateSaveButtonState();
                }
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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
