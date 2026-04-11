using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace MyPanelCarWashing
{
    public partial class StartShiftWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private SqliteDataService _SqliteDataService;
        private DateTime _selectedDate;
        private List<EmployeeSelection> _employees;

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                _selectedDate = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedDate)));
            }
        }

        public DateTime MinDate => DateTime.Now.Date;

        public List<EmployeeSelection> Employees
        {
            get => _employees;
            set
            {
                _employees = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Employees)));
            }
        }

        public StartShiftWindow(SqliteDataService SqliteDataService)
        {
            InitializeComponent();
            _SqliteDataService = SqliteDataService;
            DataContext = this;
            SelectedDate = DateTime.Now.Date;
            LoadEmployees();
        }

        private void LoadEmployees()
        {
            var allEmployees = _SqliteDataService.GetAllUsers();
            Employees = allEmployees.Select(e => new EmployeeSelection
            {
                Id = e.Id,
                FullName = e.FullName,
                IsAdmin = e.IsAdmin,
                IsSelected = false
            }).ToList();
        }

        private void StartShiftButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedEmployees = Employees.Where(emp => emp.IsSelected).ToList();

                if (!selectedEmployees.Any())
                {
                    MessageBox.Show("Выберите хотя бы одного сотрудника для смены", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверяем, есть ли открытая смена на выбранную дату
                var existingOpenShift = _SqliteDataService.GetAllShifts()
                    .FirstOrDefault(s => s.Date.Date == SelectedDate.Date && !s.IsClosed);

                if (existingOpenShift != null)
                {
                    var result = MessageBox.Show($"На {SelectedDate:dd.MM.yyyy} уже есть открытая смена!\n\n" +
                        $"Время начала: {existingOpenShift.StartTime:HH:mm}\n" +
                        $"Сотрудников: {existingOpenShift.EmployeeIds.Count}\n\n" +
                        $"Закрыть её и начать новую?",
                        "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes)
                        return;

                    // Закрываем существующую смену
                    _SqliteDataService.CloseShift(existingOpenShift.Id, "Автоматически закрыта при старте новой смены");
                }

                // Создаём новую смену - всегда на текущую дату, а не выбранную пользователем
                var newShift = new Shift
                {
                    Date = DateTime.Now.Date,  // Всегда сегодня
                    StartTime = DateTime.Now,
                    IsClosed = false,
                    EmployeeIds = selectedEmployees.Select(emp => emp.Id).ToList(),
                    Orders = new List<CarWashOrder>(),
                    Notes = null
                };

                // Сохраняем новую смену
                _SqliteDataService.StartShift(newShift);

                // Проверяем, что смена создалась
                var verifyShift = _SqliteDataService.GetCurrentOpenShift();
                if (verifyShift == null)
                {
                    MessageBox.Show("Ошибка: смена не была создана!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                MessageBox.Show($"Смена на {SelectedDate:dd.MM.yyyy} успешно открыта!\n\n" +
                    $"Сотрудников: {selectedEmployees.Count}\n" +
                    $"Список: {string.Join(", ", selectedEmployees.Select(s => s.FullName))}\n" +
                    $"Время начала: {DateTime.Now:HH:mm:ss}",
                    "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии смены: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                DialogResult = false;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class EmployeeSelection : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public int Id { get; set; }
        public string FullName { get; set; }
        public bool IsAdmin { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }
    }
}
