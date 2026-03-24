using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;

namespace MyPanelCarWashing
{
    public partial class MainWindow : Window
    {
        private User _currentUser;
        private DataService _dataService;
        private Shift _currentShift;

        // Конструктор по умолчанию (без параметров) - нужен для XAML
        public MainWindow()
        {
            InitializeComponent();
            _dataService = new DataService();
            Loaded += MainWindow_Loaded;
        }

        // Конструктор с пользователем - вызывается из LoginWindow
        public MainWindow(User user) : this()
        {
            _currentUser = user;
            this.Title = $"Панель управления мойкой - {_currentUser.FullName}";
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            DatePicker.SelectedDate = DateTime.Today;
            LoadShift(DateTime.Today);
            LoadAllShifts();
            LoadMonthlyReport(DateTime.Now.Year, DateTime.Now.Month);
        }

        private void LoadShift(DateTime date)
        {
            if (_currentUser == null)
            {
                _currentUser = new User { Id = 1, FullName = "Тестовый пользователь" };
            }

            _currentShift = _dataService.GetShiftByDate(date);

            // Загружаем сотрудников смены
            var allUsers = _dataService.GetAllUsers();
            var employees = _currentShift.EmployeeIds
                .Select(id => allUsers.FirstOrDefault(u => u.Id == id)?.FullName ?? "Неизвестный")
                .ToList();
            EmployeesList.ItemsSource = employees;

            // Загружаем заказы
            OrdersGrid.ItemsSource = _currentShift.Orders.ToList();

            UpdateTotals();
        }

        private void UpdateTotals()
        {
            if (TotalCarsText != null && TotalRevenueText != null)
            {
                TotalCarsText.Text = $"Машин за смену: {_currentShift?.Orders?.Count ?? 0}";
                var totalRevenue = _currentShift?.Orders?.Sum(o => o.TotalPrice) ?? 0;
                TotalRevenueText.Text = $"Выручка: {totalRevenue:C}";
            }
        }

        private void LoadAllShifts()
        {
            var allShifts = _dataService.GetAllShifts();
            var allUsers = _dataService.GetAllUsers();

            var displayShifts = allShifts.Select(s => new
            {
                Дата = s.Date.ToString("dd.MM.yyyy"),
                ВремяНачала = s.StartTime?.ToString("HH:mm") ?? "—",
                ВремяОкончания = s.EndTime?.ToString("HH:mm") ?? "—",
                Сотрудники = string.Join(", ", s.EmployeeIds.Select(id => allUsers.FirstOrDefault(u => u.Id == id)?.FullName ?? "—")),
                КоличествоМашин = s.Orders?.Count ?? 0,
                Выручка = s.Orders?.Sum(o => o.TotalPrice) ?? 0,
                Завершена = s.IsClosed ? "Да" : "Нет"
            }).ToList();

            AllShiftsGrid.ItemsSource = displayShifts;
        }

        private void LoadMonthlyReport(int year, int month)
        {
            var report = _dataService.GetMonthlyReport(year, month);

            MonthlyReportGrid.ItemsSource = report.Select(r => new
            {
                r.Year,
                r.Month,
                r.MonthName,
                r.TotalCars,
                r.TotalRevenue
            }).ToList();
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DatePicker.SelectedDate.HasValue)
            {
                LoadShift(DatePicker.SelectedDate.Value);
            }
        }

        private void AddEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(NewEmployeeBox.Text))
            {
                var allUsers = _dataService.GetAllUsers();
                var employee = allUsers.FirstOrDefault(u => u.FullName == NewEmployeeBox.Text);

                if (employee == null)
                {
                    employee = new User
                    {
                        Login = NewEmployeeBox.Text.Replace(" ", "").ToLower(),
                        Password = "123",
                        FullName = NewEmployeeBox.Text,
                        IsAdmin = false
                    };
                    _dataService.AddUser(employee);
                }

                _dataService.AddEmployeeToShift(_currentShift.Id, employee.Id);
                NewEmployeeBox.Clear();
                LoadShift(_currentShift.Date);
            }
            else
            {
                MessageBox.Show("Введите имя сотрудника", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AddOrder_Click(object sender, RoutedEventArgs e)
        {
            var orderWindow = new AddOrderWindow(_currentShift.Id, _dataService);
            if (orderWindow.ShowDialog() == true)
            {
                LoadShift(_currentShift.Date);
                UpdateTotals();
            }
        }

        private void SelectServices_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var order = button?.Tag as CarWashOrder;

            if (order != null)
            {
                var servicesWindow = new EditOrderServicesWindow(order.Id, _dataService);
                if (servicesWindow.ShowDialog() == true)
                {
                    LoadShift(_currentShift.Date);
                    UpdateTotals();
                }
            }
        }

        private void FinishShift_Click(object sender, RoutedEventArgs e)
        {
            var totalRevenue = _currentShift?.Orders?.Sum(o => o.TotalPrice) ?? 0;
            var totalCars = _currentShift?.Orders?.Count ?? 0;

            var notesDialog = new Window
            {
                Title = "Завершение смены",
                Width = 400,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var stackPanel = new StackPanel { Margin = new Thickness(10) };

            stackPanel.Children.Add(new TextBlock
            {
                Text = $"Смена {_currentShift.Date:dd.MM.yyyy} завершена.\nМашин: {totalCars}\nВыручка: {totalRevenue:C}\n\nВведите заметки по смене:",
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap
            });

            var notesBox = new TextBox
            {
                Height = 80,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                Margin = new Thickness(0, 0, 0, 10)
            };
            stackPanel.Children.Add(notesBox);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var okButton = new Button { Content = "OK", Width = 80, Margin = new Thickness(5), IsDefault = true };
            var cancelButton = new Button { Content = "Отмена", Width = 80, Margin = new Thickness(5), IsCancel = true };

            okButton.Click += (s, args) => { notesDialog.DialogResult = true; notesDialog.Close(); };
            cancelButton.Click += (s, args) => { notesDialog.DialogResult = false; notesDialog.Close(); };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            stackPanel.Children.Add(buttonPanel);

            notesDialog.Content = stackPanel;

            if (notesDialog.ShowDialog() == true)
            {
                _dataService.CloseShift(_currentShift.Id, notesBox.Text);

                MessageBox.Show("Смена сохранена", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                LoadAllShifts();
                LoadMonthlyReport(DateTime.Now.Year, DateTime.Now.Month);

                if (DatePicker.SelectedDate?.Date == _currentShift.Date.Date)
                {
                    LoadShift(DateTime.Today);
                }
            }
        }
    }
}