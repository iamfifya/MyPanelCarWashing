using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;  // ← Важно!
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace MyPanelCarWashing.ViewModels
{
    public class AddEditOrderViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly DataService _dataService;
        private Shift _currentShift;
        private CarWashOrder _existingOrder;
        private bool _isEditMode;
        private bool _isAppointment;
        private bool _isSubscribedToDataChanged = false;

        public AddEditOrderViewModel(DataService dataService)
        {
            _dataService = dataService;

            // Подписка на изменения данных (только один раз)
            if (!_isSubscribedToDataChanged)
            {
                DataService.DataChanged += OnDataChanged;
                _isSubscribedToDataChanged = true;
            }
        }

        public void Initialize(Shift currentShift, CarWashOrder order = null)
        {
            _currentShift = currentShift;
            _existingOrder = order;
            _isEditMode = order != null && order.Id > 0;
            _isAppointment = order != null && order.IsAppointment && order.Id == 0;

            InitializeOrder();
            LoadWashers();
            LoadServices();  // ← Загружаем услуги при инициализации
        }

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // === ПОЛЯ ===
        private CarWashOrder _currentOrder;
        private ObservableCollection<ServiceViewModel> _services;  // ← ЕДИНСТВЕННОЕ объявление _services
        private List<User> _washers;
        private decimal _servicesTotal;
        private decimal _extraCost;
        private int _selectedBodyTypeCategory = 1;
        private string _windowTitle;
        private decimal _discountPercent;
        private decimal _discountAmount;

        // === СВОЙСТВА ===

        public CarWashOrder CurrentOrder
        {
            get => _currentOrder;
            set
            {
                _currentOrder = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentOrder)));
            }
        }

        // === ЕДИНСТВЕННОЕ свойство Services (ObservableCollection) ===
        public ObservableCollection<ServiceViewModel> Services
        {
            get => _services;
            set
            {
                _services = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Services)));
            }
        }

        public List<User> Washers
        {
            get => _washers;
            set
            {
                _washers = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Washers)));
            }
        }

        public int SelectedBodyTypeCategory
        {
            get => _selectedBodyTypeCategory;
            set
            {
                _selectedBodyTypeCategory = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedBodyTypeCategory)));
                UpdateServicePrices();
                CalculateTotal();
            }
        }

        public decimal ServicesTotal
        {
            get => _servicesTotal;
            set
            {
                _servicesTotal = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ServicesTotal)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FinalTotal)));
            }
        }

        public decimal ExtraCost
        {
            get => _extraCost;
            set
            {
                _extraCost = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExtraCost)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FinalTotal)));
            }
        }

        public string WindowTitle => _windowTitle;
        public bool IsEditMode => _isEditMode;
        public bool IsAppointment => _isAppointment;

        public decimal DiscountPercent
        {
            get => _discountPercent;
            set
            {
                if (_discountPercent != value)
                {
                    _discountPercent = value;
                    OnPropertyChanged(nameof(DiscountPercent));
                    if (value > 0) DiscountAmount = 0;
                    CalculateTotal();
                }
            }
        }

        public decimal DiscountAmount
        {
            get => _discountAmount;
            set
            {
                if (_discountAmount != value)
                {
                    _discountAmount = value;
                    OnPropertyChanged(nameof(DiscountAmount));
                    if (value > 0) DiscountPercent = 0;
                    CalculateTotal();
                }
            }
        }

        // === МЕТОДЫ ===

        // Обработчик изменения данных (перезагружает услуги при изменении цен)
        private void OnDataChanged()
        {
            System.Diagnostics.Debug.WriteLine("=== [ViewModel] DataChanged received ===");
            System.Diagnostics.Debug.WriteLine($"  CurrentOrder.Id: {CurrentOrder?.Id ?? 0}");
            System.Diagnostics.Debug.WriteLine($"  Services before: {Services?.Count ?? 0}");

            LoadServices();

            System.Diagnostics.Debug.WriteLine($"  Services after: {Services?.Count ?? 0}");
            if (Services != null)
            {
                foreach (var s in Services.Take(3))
                {
                    System.Diagnostics.Debug.WriteLine($"    - {s.Name}: {s.Price:N0} ₽ (selected: {s.IsSelected})");
                }
            }

            UpdateServicePrices();
            CalculateTotal();

            System.Diagnostics.Debug.WriteLine($"  FinalTotal: {FinalTotal:N0} ₽");
            System.Diagnostics.Debug.WriteLine("=== [ViewModel] DataChanged processed ===");
        }

        private void InitializeOrder()
        {
            if (_existingOrder != null && _isEditMode)
            {
                CurrentOrder = new CarWashOrder
                {
                    Id = _existingOrder.Id,
                    CarModel = _existingOrder.CarModel,
                    CarNumber = _existingOrder.CarNumber,
                    CarBodyType = _existingOrder.CarBodyType,
                    BodyTypeCategory = _existingOrder.BodyTypeCategory,
                    Time = _existingOrder.Time,
                    BoxNumber = _existingOrder.BoxNumber,
                    WasherId = _existingOrder.WasherId,
                    ServiceIds = new List<int>(_existingOrder.ServiceIds),
                    ExtraCost = _existingOrder.ExtraCost,
                    ExtraCostReason = _existingOrder.ExtraCostReason,
                    Status = _existingOrder.Status,
                    PaymentMethod = _existingOrder.PaymentMethod,
                    IsAppointment = _existingOrder.IsAppointment,
                    AppointmentId = _existingOrder.AppointmentId,
                    ShiftId = _existingOrder.ShiftId,
                    ClientId = _existingOrder.ClientId,
                    Notes = _existingOrder.Notes,
                    DiscountPercent = _existingOrder.DiscountPercent,
                    DiscountAmount = _existingOrder.DiscountAmount,
                    OriginalTotalPrice = _existingOrder.OriginalTotalPrice
                };
                _discountPercent = CurrentOrder.DiscountPercent;
                _discountAmount = CurrentOrder.DiscountAmount;
                SelectedBodyTypeCategory = CurrentOrder.BodyTypeCategory;
                ExtraCost = CurrentOrder.ExtraCost;
                _windowTitle = "✏ Редактирование заказа";
            }
            else if (_existingOrder != null && _isAppointment)
            {
                CurrentOrder = new CarWashOrder
                {
                    Id = 0,
                    CarModel = _existingOrder.CarModel,
                    CarNumber = _existingOrder.CarNumber,
                    CarBodyType = _existingOrder.CarBodyType,
                    BodyTypeCategory = _existingOrder.BodyTypeCategory,
                    Time = _existingOrder.Time,
                    BoxNumber = _existingOrder.BoxNumber,
                    ServiceIds = new List<int>(_existingOrder.ServiceIds),
                    ExtraCost = _existingOrder.ExtraCost,
                    ExtraCostReason = _existingOrder.ExtraCostReason,
                    Status = "Предварительная запись",
                    IsAppointment = true,
                    AppointmentId = _existingOrder.AppointmentId,
                    ClientId = _existingOrder.ClientId,
                    Notes = _existingOrder.Notes,
                    DiscountPercent = _existingOrder.DiscountPercent,
                    DiscountAmount = _existingOrder.DiscountAmount,
                    OriginalTotalPrice = _existingOrder.OriginalTotalPrice
                };
                SelectedBodyTypeCategory = CurrentOrder.BodyTypeCategory;
                ExtraCost = CurrentOrder.ExtraCost;
                _windowTitle = "✏ Редактирование записи";
            }
            else
            {
                CurrentOrder = new CarWashOrder
                {
                    Id = 0,
                    CarModel = "",
                    CarNumber = "",
                    CarBodyType = "Седан",
                    BodyTypeCategory = 1,
                    Time = DateTime.Now,
                    BoxNumber = 1,
                    ServiceIds = new List<int>(),
                    ExtraCost = 0,
                    ExtraCostReason = "",
                    Status = "Выполняется",
                    PaymentMethod = "Наличные",
                    IsAppointment = false,
                    ClientId = null,
                    Notes = ""
                };
                _windowTitle = "➕ Добавление заказа";
                ExtraCost = 0;
            }
        }

        private void LoadWashers()
        {
            var allUsers = _dataService.GetAllUsers();
            Washers = allUsers;
        }

        // === ЗАГРУЗКА УСЛУГ (с пересчётом цен) ===
        public void LoadServices()
        {
            var allServices = _dataService.GetAllServices();
            var selectedIds = CurrentOrder?.ServiceIds?.ToList() ?? new List<int>();

            // Отписываемся от старых событий, чтобы не было утечек
            if (Services != null)
            {
                foreach (var s in Services)
                {
                    s.SelectionChanged -= Service_SelectionChanged;
                }
            }

            if (Services == null)
            {
                Services = new ObservableCollection<ServiceViewModel>(
                    allServices.Select(s => new ServiceViewModel
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Price = s.GetPrice(SelectedBodyTypeCategory),
                        IsSelected = selectedIds.Contains(s.Id)
                    })
                );
            }
            else
            {
                // Обновляем СУЩЕСТВУЮЩИЕ объекты
                foreach (var serviceVM in Services.ToList())
                {
                    var updatedService = allServices.FirstOrDefault(s => s.Id == serviceVM.Id);
                    if (updatedService != null)
                    {
                        // Принудительно обновляем цену — это вызовет PropertyChanged
                        serviceVM.Price = updatedService.GetPrice(SelectedBodyTypeCategory);
                        serviceVM.Name = updatedService.Name;
                        serviceVM.IsSelected = selectedIds.Contains(serviceVM.Id);
                    }
                }

                // Добавляем новые услуги
                var existingIds = new HashSet<int>(Services.Select(s => s.Id));
                foreach (var s in allServices)
                {
                    if (!existingIds.Contains(s.Id))
                    {
                        Services.Add(new ServiceViewModel
                        {
                            Id = s.Id,
                            Name = s.Name,
                            Price = s.GetPrice(SelectedBodyTypeCategory),
                            IsSelected = selectedIds.Contains(s.Id)
                        });
                    }
                }
                foreach (var service in Services)
                {
                    service.SelectionChanged += Service_SelectionChanged;
                }

                // Удаляем удалённые услуги
                var serviceIds = new HashSet<int>(allServices.Select(s => s.Id));
                var toRemove = Services.Where(s => !serviceIds.Contains(s.Id)).ToList();
                foreach (var s in toRemove) Services.Remove(s);
            }

            // Принудительное уведомление + пересчёт
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Services)));
            CalculateTotal(); // Пересчитываем при загрузке
        }

        private void Service_SelectionChanged(object sender, EventArgs e)
        {
            CalculateTotal(); // ← Пересчитываем итоговую сумму!
        }

        private void UpdateServicePrices()
        {
            if (Services == null) return;

            var allServices = _dataService.GetAllServices();
            foreach (var serviceVM in Services)
            {
                var originalService = allServices.FirstOrDefault(s => s.Id == serviceVM.Id);
                if (originalService != null)
                {
                    serviceVM.Price = originalService.GetPrice(SelectedBodyTypeCategory);
                }
            }
        }

        public bool Validate()
        {
            if (string.IsNullOrWhiteSpace(CurrentOrder.CarModel))
            {
                MessageBox.Show("Введите марку и модель автомобиля", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(CurrentOrder.CarNumber))
            {
                MessageBox.Show("Введите государственный номер", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            var selectedServices = Services?.Where(s => s.IsSelected).ToList();
            if (selectedServices == null || !selectedServices.Any())
            {
                MessageBox.Show("Выберите хотя бы одну услугу", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (ExtraCost > 0 && string.IsNullOrWhiteSpace(CurrentOrder.ExtraCostReason))
            {
                MessageBox.Show("Укажите причину дополнительной стоимости", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        public void SaveOrder(out bool success, out string message)
        {
            success = false;
            message = "";

            try
            {
                if (!Validate()) return;

                var selectedServices = Services.Where(s => s.IsSelected).ToList();
                var serviceIds = selectedServices.Select(s => s.Id).ToList();

                CurrentOrder.ServiceIds = serviceIds;
                CurrentOrder.TotalPrice = ServicesTotal;
                CurrentOrder.BodyTypeCategory = SelectedBodyTypeCategory;
                CurrentOrder.ExtraCost = ExtraCost;
                CurrentOrder.OriginalTotalPrice = ServicesTotal;
                CurrentOrder.DiscountPercent = DiscountPercent;
                CurrentOrder.DiscountAmount = DiscountAmount;

                if (!IsEditMode && !IsAppointment)
                {
                    if (CurrentOrder.Id == 0)
                    {
                        CurrentOrder.ShiftId = _currentShift?.Id ?? 0;
                        _dataService.AddOrder(CurrentOrder, serviceIds);
                        message = "Заказ добавлен!";
                        success = true;
                    }
                    else
                    {
                        message = "Ошибка: ID заказа уже существует";
                    }
                }
                else if (IsAppointment)
                {
                    var appointment = _dataService.GetAppointmentById(CurrentOrder.AppointmentId.Value);
                    if (appointment != null)
                    {
                        appointment.CarModel = CurrentOrder.CarModel;
                        appointment.CarNumber = CurrentOrder.CarNumber;
                        appointment.CarBodyType = CurrentOrder.CarBodyType;
                        appointment.BodyTypeCategory = SelectedBodyTypeCategory;
                        appointment.AppointmentDate = CurrentOrder.Time;
                        appointment.BoxNumber = CurrentOrder.BoxNumber;
                        appointment.ServiceIds = serviceIds;
                        appointment.ExtraCost = ExtraCost;
                        appointment.ExtraCostReason = CurrentOrder.ExtraCostReason;

                        _dataService.UpdateAppointment(appointment);
                        message = "Запись обновлена!";
                        success = true;
                    }
                    else
                    {
                        message = "Запись не найдена";
                    }
                }
                else
                {
                    _dataService.UpdateOrder(CurrentOrder);
                    message = $"Заказ обновлен!\n\n🚗 {CurrentOrder.CarModel} ({CurrentOrder.CarNumber})\n💰 Итого: {CurrentOrder.FinalPrice:N0} ₽";
                    if (ExtraCost > 0) message += $"\n➕ Дополнительно: {ExtraCost:N0} ₽";
                    success = true;
                }
            }
            catch (Exception ex)
            {
                message = $"Ошибка при сохранении: {ex.Message}";
            }
        }

        public void CalculateTotal()
        {
            ServicesTotal = Services?.Where(s => s.IsSelected).Sum(s => s.Price) ?? 0;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ServicesTotal)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FinalTotal)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WasherEarningsDisplay)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CompanyEarningsDisplay)));
        }

        public decimal FinalTotal
        {
            get
            {
                decimal actualDiscount = DiscountPercent > 0
                    ? ServicesTotal * (DiscountPercent / 100m)
                    : DiscountAmount;
                return ServicesTotal - actualDiscount + ExtraCost;
            }
        }

        public decimal WasherEarningsDisplay
        {
            get => ServicesTotal * 0.35m;
        }

        public decimal CompanyEarningsDisplay
        {
            get
            {
                decimal actualDiscount = DiscountPercent > 0
                    ? ServicesTotal * (DiscountPercent / 100m)
                    : DiscountAmount;
                return (ServicesTotal - actualDiscount + ExtraCost) * 0.65m;
            }
        }

        // Очистка подписки при удалении ViewModel
        public void Cleanup()
        {
            if (_isSubscribedToDataChanged)
            {
                DataService.DataChanged -= OnDataChanged;
                _isSubscribedToDataChanged = false;
            }

            // ← Отписываемся от событий услуг
            if (Services != null)
            {
                foreach (var s in Services)
                {
                    s.SelectionChanged -= Service_SelectionChanged;
                }
            }
        }
    }
}
