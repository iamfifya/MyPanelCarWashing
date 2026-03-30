using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
using MyPanelCarWashing.ViewModels;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MyPanelCarWashing
{
    public partial class AppointmentWindow : Window
    {
        private DataService _dataService;
        private AppointmentViewModel _viewModel;
        private int _selectedBox = 1;

        public AppointmentWindow(DataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
            var viewModel = App.GetService<AppointmentViewModel>();
            _viewModel = viewModel;
            DataContext = _viewModel;

            // Устанавливаем начальные значения
            AppointmentDatePicker.SelectedDate = DateTime.Now.AddDays(1);
            AppointmentTimeTextBox.Text = "12:00";
            DurationTextBox.Text = "60";
            ExtraCostTextBox.Text = "0";

            // Подписываемся на события
            AppointmentTimeTextBox.TextChanged += (s, e) => CheckAvailability();
            DurationTextBox.TextChanged += (s, e) => CheckAvailability();
            Box1Radio.Checked += (s, e) => { _selectedBox = 1; CheckAvailability(); };
            Box2Radio.Checked += (s, e) => { _selectedBox = 2; CheckAvailability(); };
            Box3Radio.Checked += (s, e) => { _selectedBox = 3; CheckAvailability(); };
            ExtraCostTextBox.TextChanged += (s, e) =>
            {
                if (decimal.TryParse(ExtraCostTextBox.Text, out decimal cost))
                    _viewModel.ExtraCost = cost;
            };

            // Подписываемся на изменение выбора услуг
            ServicesListBox.SelectionChanged += (s, e) =>
            {
                foreach (ServiceViewModel service in ServicesListBox.Items)
                {
                    service.IsSelected = ServicesListBox.SelectedItems.Contains(service);
                }
                _viewModel.CalculateTotal();
                CheckAvailability();
            };

            // Подписываемся на изменение даты
            AppointmentDatePicker.SelectedDateChanged += (s, date) => CheckAvailability();

            // Подписываемся на изменение категории кузова
            BodyTypeComboBox.SelectionChanged += (s, e) =>
            {
                if (BodyTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    if (int.TryParse(selectedItem.Tag?.ToString(), out int category))
                    {
                        _viewModel.SelectedBodyTypeCategory = category;
                    }
                }
            };

            // Подписываемся на изменение цены в ViewModel
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.ServicesTotal) ||
                    e.PropertyName == nameof(_viewModel.FinalTotal))
                {
                    UpdateTotalDisplay();
                }
            };

            // Инициализируем отображение
            UpdateTotalDisplay();
            CheckAvailability();
        }

        private void UpdateTotalDisplay()
        {
            ServicesTotalText.Text = $"💰 Услуги: {_viewModel.ServicesTotal:N0} ₽";
            ExtraCostText.Text = $"➕ Дополнительно: {_viewModel.ExtraCost:N0} ₽";
            TotalPriceTextBlock.Text = $"💰 Итого: {_viewModel.FinalTotal:N0} ₽";
        }

        private void CheckAvailability()
        {
            try
            {
                var date = AppointmentDatePicker.SelectedDate;
                if (!date.HasValue)
                {
                    AvailabilityText.Text = "❌ Выберите дату";
                    AvailabilityText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
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

        private void CheckAvailabilityButton_Click(object sender, RoutedEventArgs e)
        {
            CheckAvailability();
        }

        private string GetCategoryName(int categoryId)
        {
            switch (categoryId)
            {
                case 1:
                    return "Категория 1 (Легковая)";
                case 2:
                    return "Категория 2 (Универсал)";
                case 3:
                    return "Категория 3 (Кроссовер)";
                case 4:
                    return "Категория 4 (Внедорожник)";
                default:
                    return "Категория 1 (Легковая)";
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
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

                var selectedServices = _viewModel.GetSelectedServiceIds();
                if (!selectedServices.Any())
                {
                    MessageBox.Show("Выберите хотя бы одну услугу", "Ошибка");
                    return;
                }

                bool isAvailable = _dataService.IsBoxAvailable(_selectedBox, startTime, duration);
                if (!isAvailable)
                {
                    MessageBox.Show($"Время {startTime:HH:mm} уже занято другой записью!\n\nПожалуйста, выберите другое время.", "Ошибка");
                    return;
                }

                // Получаем категорию кузова
                int bodyTypeCategory = _viewModel.SelectedBodyTypeCategory;
                string bodyTypeName = GetCategoryName(_viewModel.SelectedBodyTypeCategory);

                var appointment = new Appointment
                {
                    CarModel = CarModelTextBox.Text,
                    CarNumber = CarNumberTextBox.Text,
                    CarBodyType = bodyTypeName,
                    BodyTypeCategory = bodyTypeCategory,
                    AppointmentDate = startTime,
                    DurationMinutes = duration,
                    ServiceIds = selectedServices,
                    ExtraCost = _viewModel.ExtraCost,
                    ExtraCostReason = ExtraCostReasonTextBox.Text.Trim(),
                    BoxNumber = _selectedBox,
                    Notes = "",
                    IsCompleted = false
                };

                _dataService.AddAppointment(appointment);
                DataService.NotifyDataChanged(); // Оповещаем все окна

                MessageBox.Show($"✅ Запись создана!\n\n" +
                    $"🚗 {appointment.CarModel} ({appointment.CarNumber})\n" +
                    $"📅 {appointment.AppointmentDate:dd.MM.yyyy HH:mm}\n" +
                    $"💰 Итого: {_viewModel.FinalTotal:N0} ₽",
                    "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true; // ← ЭТО ВАЖНО! Без этого Closed событие не сработает
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
                DialogResult = false;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
