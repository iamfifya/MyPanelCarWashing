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
using System.Windows.Media.Animation;

namespace MyPanelCarWashing.Controls
{
    public partial class AppointmentsOverlay : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private DataService _dataService;
        private List<AppointmentDisplayItem> _box1Items;
        private List<AppointmentDisplayItem> _box2Items;
        private List<AppointmentDisplayItem> _box3Items;
        private AppointmentDisplayItem _selectedItem;

        public DataService DataService
        {
            get => _dataService;
            set
            {
                _dataService = value;
                if (_dataService != null)
                {
                    FilterDatePicker.SelectedDate = DateTime.Now;
                    LoadAppointments();
                }
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

            // Подписываемся на изменение даты
            FilterDatePicker.SelectedDateChanged += FilterDatePicker_SelectedDateChanged;
        }

        private void FilterDatePicker_SelectedDateChanged(object sender, DateTime? selectedDate)
        {
            // Автоматически загружаем записи при выборе даты
            LoadAppointments();
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            // Кнопка "Показать" - для ручного обновления
            LoadAppointments();
        }

        public void Show()
        {
            System.Diagnostics.Debug.WriteLine("=== AppointmentsOverlay.Show() ===");
            if (_dataService != null)
            {
                LoadAppointments();
            }

            this.Visibility = Visibility.Visible;
            OverlayBackground.Visibility = Visibility.Visible;

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
                };
                hideAnimation.Begin();
            }
            else
            {
                this.Visibility = Visibility.Collapsed;
                OverlayBackground.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadAppointments()
        {
            if (_dataService == null) return;

            DateTime? filterDate = FilterDatePicker.SelectedDate;
            if (!filterDate.HasValue) return;

            var allAppointments = _dataService.GetAllAppointments();
            var allServices = _dataService.GetAllServices();

            var appointments = allAppointments
                .Where(a => a.AppointmentDate.Date == filterDate.Value.Date && !a.IsCompleted)
                .ToList();

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
        }

        private void NewAppointmentButton_Click(object sender, RoutedEventArgs e)
        {
            var appointmentWin = App.GetService<AppointmentWindow>();
            appointmentWin.Closed += (s, args) =>
            {
                LoadAppointments();
                DataService.NotifyDataChanged();
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

            var appointment = _dataService.GetAppointmentById(SelectedItem.Id);
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

                var viewModel = App.GetService<AddEditOrderViewModel>();
                var editWin = new AddEditOrderWindow(_dataService, viewModel, null, tempOrder);
                editWin.Closed += (s, args) =>
                {
                    LoadAppointments();
                    DataService.NotifyDataChanged();
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
                _dataService.DeleteAppointment(SelectedItem.Id);
                LoadAppointments();
                DataService.NotifyDataChanged();
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

    public class AppointmentDisplayItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public int Id { get; set; }
        public string CarNumber { get; set; }
        public string CarModel { get; set; }
        public DateTime Time { get; set; }
        public DateTime EndTime { get; set; }
        public string ServicesList { get; set; }
        public decimal FinalPrice { get; set; }
        public decimal ExtraCost { get; set; }
        public string ExtraCostReason { get; set; }
        public int BoxNumber { get; set; }
        public string Status { get; set; }
        public bool IsCompleted { get; set; }

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
