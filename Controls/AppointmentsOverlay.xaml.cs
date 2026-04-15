using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
using MyPanelCarWashing.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MyPanelCarWashing.Controls
{
    public partial class AppointmentsOverlay : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private SqliteDataService _SqliteDataService;
        private Shift _currentShift;
        private List<AppointmentDisplayItem> _box1Items;
        private List<AppointmentDisplayItem> _box2Items;
        private List<AppointmentDisplayItem> _box3Items;
        private AppointmentDisplayItem _selectedItem;

        public SqliteDataService SqliteDataService
        {
            get => _SqliteDataService;
            set
            {
                _SqliteDataService = value;
                if (_SqliteDataService != null)
                {
                    FilterDatePicker.SelectedDate = DateTime.Now;
                    LoadAppointments();
                }
            }
        }

        public Shift CurrentShift
        {
            get => _currentShift;
            set
            {
                _currentShift = value;
                System.Diagnostics.Debug.WriteLine($"AppointmentsOverlay: CurrentShift updated (Id={_currentShift?.Id})");
            }
        }

        public List<AppointmentDisplayItem> Box1Items
        {
            get => _box1Items;
            set
            {
                _box1Items = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Box1Items)));
            }
        }

        public List<AppointmentDisplayItem> Box2Items
        {
            get => _box2Items;
            set
            {
                _box2Items = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Box2Items)));
            }
        }

        public List<AppointmentDisplayItem> Box3Items
        {
            get => _box3Items;
            set
            {
                _box3Items = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Box3Items)));
            }
        }

        public AppointmentDisplayItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem != null)
                    _selectedItem.IsSelected = false;

                _selectedItem = value;

                if (_selectedItem != null)
                    _selectedItem.IsSelected = true;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedItem)));
            }
        }

        public AppointmentsOverlay()
        {
            InitializeComponent();
            DataContext = this;

            // Подписываемся на глобальное событие изменения данных
            SqliteDataService.DataChanged += OnDataChanged;

            // Подписываемся на изменение даты
            FilterDatePicker.SelectedDateChanged += FilterDatePicker_SelectedDateChanged;

            // Подписываемся на событие выгрузки контрола
            this.Unloaded += AppointmentsOverlay_Unloaded;

            // Устанавливаем дату по умолчанию
            FilterDatePicker.SelectedDate = DateTime.Now;
        }
        private void OnDataChanged()
        {
            // Обновляем данные в UI потоке
            Dispatcher.Invoke(() =>
            {
                System.Diagnostics.Debug.WriteLine("AppointmentsOverlay: DataChanged received, reloading...");
                LoadAppointments();
            });
        }
        private void AppointmentsOverlay_Unloaded(object sender, RoutedEventArgs e)
        {
            // Отписываемся от событий при выгрузке контрола
            SqliteDataService.DataChanged -= OnDataChanged;
            FilterDatePicker.SelectedDateChanged -= FilterDatePicker_SelectedDateChanged;
        }

        private void FilterDatePicker_SelectedDateChanged(object sender, DateTime? selectedDate)
        {
            // Автоматически загружаем записи при выборе даты
            LoadAppointments();
        }

        public void Show()
        {
            System.Diagnostics.Debug.WriteLine("=== AppointmentsOverlay.Show() ===");

            // Делаем видимым
            this.Visibility = Visibility.Visible;
            OverlayBackground.Visibility = Visibility.Visible;
            PopupPanel.Visibility = Visibility.Visible;

            // Загружаем данные
            if (_SqliteDataService != null)
            {
                LoadAppointments();
            }

            // Запускаем анимацию появления
            var showAnimation = Resources["ShowAnimation"] as Storyboard;
            if (showAnimation != null)
            {
                showAnimation.Begin();
            }
        }

        public void Hide()
        {
            System.Diagnostics.Debug.WriteLine("=== AppointmentsOverlay.Hide() ===");

            // Запускаем анимацию скрытия
            var hideAnimation = Resources["HideAnimation"] as Storyboard;
            if (hideAnimation != null)
            {
                hideAnimation.Completed += (s, e) =>
                {
                    this.Visibility = Visibility.Collapsed;
                    OverlayBackground.Visibility = Visibility.Collapsed;
                    PopupPanel.Visibility = Visibility.Collapsed;
                };
                hideAnimation.Begin();
            }
            else
            {
                this.Visibility = Visibility.Collapsed;
                OverlayBackground.Visibility = Visibility.Collapsed;
                PopupPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadAppointments()
        {
            if (_SqliteDataService == null)
            {
                System.Diagnostics.Debug.WriteLine("LoadAppointments: SqliteDataService is null!");
                return;
            }

            DateTime? filterDate = FilterDatePicker.SelectedDate;
            if (!filterDate.HasValue)
            {
                System.Diagnostics.Debug.WriteLine("LoadAppointments: No date selected, using today");
                filterDate = DateTime.Now;
                FilterDatePicker.SelectedDate = filterDate;
            }

            System.Diagnostics.Debug.WriteLine($"LoadAppointments: Loading for date {filterDate:dd.MM.yyyy}");

            var allAppointments = _SqliteDataService.GetAllAppointments();
            var allServices = _SqliteDataService.GetAllServices();

            var appointments = allAppointments
                .Where(a => a.AppointmentDate.Date == filterDate.Value.Date && !a.IsCompleted)
                .ToList();

            System.Diagnostics.Debug.WriteLine($"LoadAppointments: Found {appointments.Count} appointments");

            var displayItems = appointments.Select(a => new AppointmentDisplayItem
            {
                Id = a.Id,
                CarModel = a.CarModel,
                CarNumber = a.CarNumber,
                Time = a.AppointmentDate,
                EndTime = a.EndTime,
                ServicesList = string.Join(", ", a.ServiceIds.Select(id => allServices.FirstOrDefault(s => s.Id == id)?.Name ?? "Unknown")),
                FinalPrice = a.ServiceIds.Sum(id => allServices.FirstOrDefault(s => s.Id == id)?.GetPrice(a.BodyTypeCategory) ?? 0) + a.ExtraCost,
                ExtraCost = a.ExtraCost,
                ExtraCostReason = a.ExtraCostReason,
                BoxNumber = a.BoxNumber,
                Status = a.AppointmentDate <= DateTime.Now ? "⚠️ Просрочена" : "⏳ Ожидает",
                IsCompleted = a.IsCompleted
            }).ToList();

            Box1Items = displayItems.Where(i => i.BoxNumber == 1).OrderBy(i => i.Time).ToList();
            Box2Items = displayItems.Where(i => i.BoxNumber == 2).OrderBy(i => i.Time).ToList();
            Box3Items = displayItems.Where(i => i.BoxNumber == 3).OrderBy(i => i.Time).ToList();

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Box1Items)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Box2Items)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Box3Items)));

            System.Diagnostics.Debug.WriteLine($"LoadAppointments: Box1={Box1Items.Count}, Box2={Box2Items.Count}, Box3={Box3Items.Count}");
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            LoadAppointments();
        }

        private void NewAppointmentButton_Click(object sender, RoutedEventArgs e)
        {
            var appointmentWin = App.GetService<AppointmentWindow>();
            appointmentWin.Closed += (s, args) =>
            {
                LoadAppointments();
                SqliteDataService.NotifyDataChanged();
            };
            appointmentWin.ShowDialog();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem == null)
            {
                MessageBox.Show("Выберите запись для редактирования", "Внимание");
                return;
            }

            var appointment = _SqliteDataService.GetAppointmentById(SelectedItem.Id);
            if (appointment != null)
            {
                var tempOrder = new CarWashOrder
                {
                    Id = 0,
                    CarModel = appointment.CarModel,
                    CarNumber = appointment.CarNumber,
                    CarBodyType = appointment.CarBodyType,
                    BodyTypeCategory = appointment.BodyTypeCategory,
                    Time = appointment.AppointmentDate,
                    BoxNumber = appointment.BoxNumber,
                    ServiceIds = appointment.ServiceIds,
                    ExtraCost = appointment.ExtraCost,
                    ExtraCostReason = appointment.ExtraCostReason,
                    IsAppointment = true,
                    AppointmentId = appointment.Id
                };

                // Используем App.GetService, а не _viewModel
                var viewModel = App.GetService<AddEditOrderViewModel>();
                var editWin = new AddEditOrderWindow(_SqliteDataService, viewModel, _currentShift, tempOrder);
                editWin.Closed += (s, args) =>
                {
                    LoadAppointments();
                    SqliteDataService.NotifyDataChanged();
                };
                editWin.ShowDialog();
            }
        }

        

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem == null)
            {
                MessageBox.Show("Выберите запись для удаления", "Внимание");
                return;
            }

            if (MessageBox.Show("Удалить выбранную запись?", "Подтверждение",
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _SqliteDataService.DeleteAppointment(SelectedItem.Id);
                LoadAppointments();
                SqliteDataService.NotifyDataChanged();
                MessageBox.Show("Запись удалена", "Успешно");
            }
        }

        private void AppointmentList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SelectedItem != null)
            {
                EditButton_Click(sender, null);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }
    }
}
