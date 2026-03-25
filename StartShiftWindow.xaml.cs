using MyPanelCarWashing.Models;
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

        private DateTime _selectedDate;
        private List<EmployeeSelection> _employees;

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                _selectedDate = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedDate"));
            }
        }

        public DateTime MinDate => DateTime.Now.Date;

        public List<EmployeeSelection> Employees
        {
            get => _employees;
            set
            {
                _employees = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Employees"));
            }
        }

        public StartShiftWindow()
        {
            InitializeComponent();
            DataContext = this;
            SelectedDate = DateTime.Now.Date;
            LoadEmployees();
        }

        private void LoadEmployees()
        {
            var allEmployees = Core.DB.GetAllUsers();
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

                // Получаем все смены
                var allShifts = Core.DB.GetAllShifts();

                // Проверяем, есть ли открытая смена на эту дату
                var existingOpenShift = allShifts.FirstOrDefault(s => s.Date.Date == SelectedDate.Date && !s.IsClosed);

                if (existingOpenShift != null)
                {
                    // Если есть открытая смена, предлагаем закрыть её
                    var result = MessageBox.Show($"На {SelectedDate:dd.MM.yyyy} уже есть открытая смена!\n\n" +
                        $"Время начала: {existingOpenShift.StartTime:HH:mm}\n" +
                        $"Закрыть её и начать новую?",
                        "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes)
                    {
                        return;
                    }

                    // Закрываем старую смену
                    existingOpenShift.EndTime = DateTime.Now;
                    existingOpenShift.IsClosed = true;

                    // Удаляем старую смену из списка
                    allShifts.Remove(existingOpenShift);
                }

                // Создаем новую смену
                var newShift = new Shift
                {
                    Id = allShifts.Any() ? allShifts.Max(s => s.Id) + 1 : 1,
                    Date = SelectedDate,
                    StartTime = DateTime.Now,
                    IsClosed = false,
                    EmployeeIds = selectedEmployees.Select(emp => emp.Id).ToList(),
                    Orders = new List<CarWashOrder>()
                };

                // Добавляем новую смену
                allShifts.Add(newShift);

                // Сохраняем
                var appData = FileDataService.LoadData();
                appData.Shifts = allShifts;
                FileDataService.SaveData(appData);

                // Обновляем Core.DB
                Core.RefreshData();

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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsSelected"));
            }
        }
    }
}