using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
using MyPanelCarWashing.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MyPanelCarWashing
{
    public partial class AppointmentWindow : Window
    {
        private DataService _dataService;
        private List<ServiceViewModel> _services;
        private decimal _servicesTotal;
        private decimal _extraCost;
        private int _selectedBox = 1;

        public AppointmentWindow(DataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;

            AppointmentDatePicker.SelectedDate = DateTime.Now.AddDays(1);
            AppointmentTimeTextBox.Text = "12:00";
            DurationTextBox.Text = "60";
            ExtraCostTextBox.Text = "0";

            LoadServices();

            // Подписываемся на события
            AppointmentTimeTextBox.TextChanged += (s, e) => CheckAvailability();
            DurationTextBox.TextChanged += (s, e) => CheckAvailability();
            Box1Radio.Checked += (s, e) => { _selectedBox = 1; CheckAvailability(); };
            Box2Radio.Checked += (s, e) => { _selectedBox = 2; CheckAvailability(); };
            Box3Radio.Checked += (s, e) => { _selectedBox = 3; CheckAvailability(); };
            ExtraCostTextBox.TextChanged += (s, e) => CalculateTotal();

            // Для услуг используем SelectionChanged
            ServicesListBox.SelectionChanged += (s, e) =>
            {
                CalculateTotal();
                CheckAvailability();
            };

            // Для даты используем событие из CustomDatePicker
            AppointmentDatePicker.SelectedDateChanged += (s, date) => CheckAvailability();

            // После загрузки услуг обновляем сумму
            CalculateTotal();
            CheckAvailability();
        }

        private void LoadServices()
        {
            var allServices = _dataService.GetAllServices();
            _services = allServices.Select(s => new ServiceViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Price = s.GetPrice(1),
                IsSelected = false // По умолчанию ничего не выбрано
            }).ToList();

            ServicesListBox.ItemsSource = _services;

            // НЕ ВЫБИРАЕМ НИЧЕГО АВТОМАТИЧЕСКИ
            // Оставляем все услуги невыбранными
        }

        private void CalculateTotal()
        {
            // Сумма выбранных услуг
            _servicesTotal = 0;

            if (ServicesListBox.SelectedItems != null)
            {
                foreach (ServiceViewModel service in ServicesListBox.SelectedItems)
                {
                    _servicesTotal += service.Price;
                }
            }

            // Дополнительная стоимость
            if (!decimal.TryParse(ExtraCostTextBox.Text, out _extraCost))
                _extraCost = 0;

            decimal finalTotal = _servicesTotal + _extraCost;
            TotalPriceTextBlock.Text = $"💰 Итого: {finalTotal:N0} ₽";
        }

        private void CheckAvailability()
        {
            try
            {
                var date = AppointmentDatePicker.SelectedDate;
                if (!date.HasValue)
                {
                    AvailabilityText.Text = "❌ Выберите дату";
                    return;
                }

                if (!DateTime.TryParse($"{date:yyyy-MM-dd} {AppointmentTimeTextBox.Text}", out DateTime startTime))
                {
                    AvailabilityText.Text = "❌ Неверный формат времени";
                    AvailabilityText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                    return;
                }

                if (!int.TryParse(DurationTextBox.Text, out int duration) || duration <= 0)
                {
                    AvailabilityText.Text = "❌ Укажите корректную длительность";
                    AvailabilityText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                    return;
                }

                var endTime = startTime.AddMinutes(duration);

                // Проверка рабочего времени
                var workingStart = date.Value.Date.AddHours(8);
                var workingEnd = date.Value.Date.AddHours(22);

                if (startTime < workingStart)
                {
                    AvailabilityText.Text = $"⚠️ Рабочий день начинается с 8:00";
                    AvailabilityText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange);
                    return;
                }

                if (endTime > workingEnd)
                {
                    AvailabilityText.Text = $"⚠️ Рабочий день заканчивается в 22:00";
                    AvailabilityText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange);
                    return;
                }

                // Проверка длительности
                if (duration < 15)
                {
                    AvailabilityText.Text = "⚠️ Минимальная длительность мойки - 15 минут";
                    AvailabilityText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange);
                    return;
                }

                if (duration > 240)
                {
                    AvailabilityText.Text = "⚠️ Максимальная длительность мойки - 4 часа";
                    AvailabilityText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange);
                    return;
                }

                // Проверяем доступность
                var isAvailable = _dataService.IsBoxAvailable(_selectedBox, startTime, duration);

                if (isAvailable)
                {
                    AvailabilityText.Text = $"🟢 Время свободно с {startTime:HH:mm} до {endTime:HH:mm}";
                    AvailabilityText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
                }
                else
                {
                    AvailabilityText.Text = $"🔴 Время {startTime:HH:mm} - {endTime:HH:mm} уже занято!";
                    AvailabilityText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                }
            }
            catch (Exception ex)
            {
                AvailabilityText.Text = $"❌ Ошибка проверки: {ex.Message}";
            }
        }

        private DateTime? GetNextFreeTime(DateTime startTime, int durationMinutes)
        {
            // Получаем все записи на эту дату
            var allAppointments = _dataService.GetAllAppointments()
                .Where(a => !a.IsCompleted && a.AppointmentDate.Date == startTime.Date)
                .OrderBy(a => a.AppointmentDate)
                .ToList();

            var workingStart = startTime.Date.AddHours(8);
            var workingEnd = startTime.Date.AddHours(22);

            // Начинаем с 8:00
            var checkTime = workingStart;

            System.Diagnostics.Debug.WriteLine($"=== Поиск свободного времени ===");
            System.Diagnostics.Debug.WriteLine($"Длительность: {durationMinutes} мин");

            while (checkTime.AddMinutes(durationMinutes) <= workingEnd)
            {
                var endTime = checkTime.AddMinutes(durationMinutes);

                bool isFree = true;
                foreach (var a in allAppointments)
                {
                    var aStart = a.AppointmentDate;
                    var aEnd = a.EndTime;

                    // Если интервалы пересекаются
                    if (!(endTime <= aStart || checkTime >= aEnd))
                    {
                        isFree = false;
                        // Перемещаем время на окончание этой записи
                        checkTime = aEnd;
                        break;
                    }
                }

                if (isFree)
                {
                    System.Diagnostics.Debug.WriteLine($"Найдено свободное время: {checkTime:HH:mm} - {endTime:HH:mm}");
                    return checkTime;
                }

                // Защита от бесконечного цикла
                if (checkTime > workingEnd)
                {
                    break;
                }
            }

            System.Diagnostics.Debug.WriteLine($"Свободное время не найдено");
            return null;
        }
        private void ServicesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                CalculateTotal();
                CheckAvailability();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в ServicesListBox_SelectionChanged: {ex.Message}");
            }
        }

        private void ExtraCostTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                CalculateTotal();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в ExtraCostTextBox_TextChanged: {ex.Message}");
            }
        }

        private void AppointmentDatePicker_SelectedDateChanged(DateTime? oldDate, DateTime? newDate)
        {
            try
            {
                CheckAvailability();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в AppointmentDatePicker_SelectedDateChanged: {ex.Message}");
            }
        }

        private void CheckAvailabilityButton_Click(object sender, RoutedEventArgs e)
        {
            CheckAvailability();
        }

        private bool ValidateAvailability()
        {
            var date = AppointmentDatePicker.SelectedDate;
            if (!date.HasValue) return false;

            if (!DateTime.TryParse($"{date:yyyy-MM-dd} {AppointmentTimeTextBox.Text}", out DateTime startTime))
                return false;

            if (!int.TryParse(DurationTextBox.Text, out int duration) || duration <= 0)
                return false;

            return _dataService.IsBoxAvailable(_selectedBox, startTime, duration);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка полей
                if (string.IsNullOrWhiteSpace(CarModelTextBox.Text))
                {
                    MessageBox.Show("Введите марку и модель автомобиля", "Ошибка");
                    return;
                }

                if (string.IsNullOrWhiteSpace(CarNumberTextBox.Text))
                {
                    MessageBox.Show("Введите государственный номер", "Ошибка");
                    return;
                }

                var date = AppointmentDatePicker.SelectedDate;
                if (!date.HasValue)
                {
                    MessageBox.Show("Выберите дату записи", "Ошибка");
                    return;
                }

                if (!DateTime.TryParse($"{date:yyyy-MM-dd} {AppointmentTimeTextBox.Text}", out DateTime startTime))
                {
                    MessageBox.Show("Введите корректное время (например 14:30)", "Ошибка");
                    return;
                }

                if (!int.TryParse(DurationTextBox.Text, out int duration) || duration < 15)
                {
                    MessageBox.Show("Минимальная длительность - 15 минут", "Ошибка");
                    return;
                }

                // Проверка рабочего времени
                if (startTime < date.Value.Date.AddHours(8))
                {
                    MessageBox.Show("Рабочий день начинается в 8:00", "Ошибка");
                    return;
                }

                if (startTime.AddMinutes(duration) > date.Value.Date.AddHours(22))
                {
                    MessageBox.Show("Рабочий день заканчивается в 22:00", "Ошибка");
                    return;
                }

                // Получаем выбранные услуги
                var selectedServices = ServicesListBox.SelectedItems.Cast<ServiceViewModel>().ToList();
                if (!selectedServices.Any())
                {
                    MessageBox.Show("Выберите хотя бы одну услугу", "Ошибка");
                    return;
                }

                // Проверяем доступность времени
                bool isAvailable = _dataService.IsBoxAvailable(_selectedBox, startTime, duration);

                if (!isAvailable)
                {
                    MessageBox.Show($"Время {startTime:HH:mm} уже занято другой записью!\n\n" +
                        "Пожалуйста, выберите другое время.", "Ошибка");
                    return;
                }

                // СОЗДАЕМ ТОЛЬКО ЗАПИСЬ, НЕ КОНВЕРТИРУЕМ В ЗАКАЗ!
                var appointment = new Appointment
                {
                    CarModel = CarModelTextBox.Text,
                    CarNumber = CarNumberTextBox.Text,
                    CarBodyType = (BodyTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Седан",
                    AppointmentDate = startTime,
                    DurationMinutes = duration,
                    ServiceIds = selectedServices.Select(s => s.Id).ToList(),
                    ExtraCost = _extraCost,
                    ExtraCostReason = ExtraCostReasonTextBox.Text.Trim(),
                    BoxNumber = _selectedBox,
                    Notes = "",
                    IsCompleted = false
                };

                // Добавляем запись
                _dataService.AddAppointment(appointment);

                MessageBox.Show($"✅ Запись создана!\n\n" +
                    $"🚗 {appointment.CarModel} ({appointment.CarNumber})\n" +
                    $"📅 {appointment.AppointmentDate:dd.MM.yyyy HH:mm}\n" +
                    $"⏱️ Длительность: {appointment.DurationMinutes} мин\n\n" +
                    $"⏰ Запись будет выполнена при начале смены {appointment.AppointmentDate:dd.MM.yyyy}",
                    "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
