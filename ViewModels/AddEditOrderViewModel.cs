using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace MyPanelCarWashing.ViewModels
{
    public class AddEditOrderViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly SqliteDataService _sqliteDataService;  // ← ИСПРАВЛЕНО: _sqliteDataService
        private Shift _currentShift;
        private CarWashOrder _existingOrder;
        private bool _isEditMode;
        private bool _isAppointment;
        private bool _isSubscribedToDataChanged = false;
        private OrderCalculation _currentCalc;

        public AddEditOrderViewModel(SqliteDataService dataService)
        {
            _sqliteDataService = dataService;  // ← ИСПРАВЛЕНО
            if (!_isSubscribedToDataChanged)
            {
                SqliteDataService.DataChanged += OnDataChanged;
                _isSubscribedToDataChanged = true;
            }
        }

        public void Initialize(Shift currentShift, CarWashOrder order = null)
        {
            _currentShift = currentShift;
            _existingOrder = order;
            _isEditMode = order != null && order.Id > 0;
            _isAppointment = order != null && order.IsAppointment && order.Id == 0;
            _currentCalc = null;

            InitializeOrder();
            LoadWashers();
            LoadServices();
        }

        public void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private CarWashOrder _currentOrder;
        private ObservableCollection<ServiceViewModel> _services;
        private List<User> _washers;
        private int _selectedBodyTypeCategory = 1;
        private string _windowTitle;
        private decimal _discountPercent;
        private decimal _discountAmount;

        public CarWashOrder CurrentOrder
        {
            get => _currentOrder;
            set { _currentOrder = value; _currentCalc = null; OnPropertyChanged(nameof(CurrentOrder)); }
        }

        public ObservableCollection<ServiceViewModel> Services
        {
            get => _services;
            set { _services = value; OnPropertyChanged(nameof(Services)); }
        }

        public List<User> Washers
        {
            get => _washers;
            set { _washers = value; OnPropertyChanged(nameof(Washers)); }
        }

        public int SelectedBodyTypeCategory
        {
            get => _selectedBodyTypeCategory;
            set
            {
                if (_selectedBodyTypeCategory != value)
                {
                    _selectedBodyTypeCategory = value;
                    OnPropertyChanged(nameof(SelectedBodyTypeCategory));
                    UpdateServicePrices();
                }
            }
        }

        // === ОБЁРТКИ НАД OrderMath (чтобы XAML не ломался) ===
        public decimal ServicesTotal => CurrentCalculation.ServicesTotal;
        public decimal ExtraCost
        {
            get => CurrentOrder.ExtraCost;
            set
            {
                if (CurrentOrder.ExtraCost != value)
                {
                    CurrentOrder.ExtraCost = value;
                    OnPropertyChanged(nameof(ExtraCost));
                    Recalculate();
                }
            }
        }
        public decimal FinalTotal => CurrentCalculation.FinalPrice;
        public decimal WasherEarningsDisplay => CurrentCalculation.WasherEarnings;
        public decimal CompanyEarningsDisplay => CurrentCalculation.CompanyEarnings;

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
                    CurrentOrder.DiscountPercent = value;
                    if (value > 0) { _discountAmount = 0; CurrentOrder.DiscountAmount = 0; }
                    OnPropertyChanged(nameof(DiscountPercent));
                    Recalculate();
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
                    CurrentOrder.DiscountAmount = value;
                    if (value > 0) { _discountPercent = 0; CurrentOrder.DiscountPercent = 0; }
                    OnPropertyChanged(nameof(DiscountAmount));
                    Recalculate();
                }
            }
        }

        // === ГЛАВНЫЙ МЕТОД РАСЧЁТА ===
        private OrderCalculation CurrentCalculation
        {
            get
            {
                if (_currentCalc == null)
                {
                    var services = _sqliteDataService.GetAllServices();
                    _currentCalc = OrderMath.Calculate(CurrentOrder, services);
                }
                return _currentCalc;
            }
        }

        public void Recalculate()
        {
            _currentCalc = null;

            var selectedCount = Services?.Count(s => s.IsSelected) ?? 0;
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Recalculate: {selectedCount} услуг выбрано");
            if (Services != null)
            {
                foreach (var s in Services.Where(s => s.IsSelected))
                {
                    System.Diagnostics.Debug.WriteLine($"  - {s.Name}: {s.Price} ₽");
                }
            }

            System.Diagnostics.Debug.WriteLine($"  ServicesTotal: {ServicesTotal}");
            System.Diagnostics.Debug.WriteLine($"  FinalTotal: {FinalTotal}");
            System.Diagnostics.Debug.WriteLine($"  WasherEarnings: {WasherEarningsDisplay}");

            OnPropertyChanged(nameof(FinalTotal));
            OnPropertyChanged(nameof(WasherEarningsDisplay));
            OnPropertyChanged(nameof(CompanyEarningsDisplay));
            OnPropertyChanged(nameof(ServicesTotal));
        }

        // === Просто синхронизирует выбранные услуги с заказом ===
        public void SyncServiceIds()
        {
            if (CurrentOrder != null && Services != null)
                CurrentOrder.ServiceIds = Services.Where(s => s.IsSelected).Select(s => s.Id).ToList();
        }

        private void OnDataChanged()
        {
            LoadServices();
            Recalculate();
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
            }
        }

        private void LoadWashers() => Washers = _sqliteDataService.GetAllUsers();

        public void LoadServices()
        {
            var allServices = _sqliteDataService.GetAllServices();
            var selectedIds = CurrentOrder?.ServiceIds?.ToList() ?? new List<int>();

            if (Services == null)
            {
                Services = new ObservableCollection<ServiceViewModel>(
                    allServices.Select(s => new ServiceViewModel
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Price = s.GetPrice(SelectedBodyTypeCategory),
                        IsSelected = selectedIds.Contains(s.Id)
                    }));
            }
            else
            {
                foreach (var vm in Services.ToList())
                {
                    var updated = allServices.FirstOrDefault(s => s.Id == vm.Id);
                    if (updated != null)
                    {
                        vm.Price = updated.GetPrice(SelectedBodyTypeCategory);
                        vm.Name = updated.Name;
                        vm.IsSelected = selectedIds.Contains(vm.Id);
                    }
                }
                foreach (var s in allServices)
                {
                    if (!Services.Any(x => x.Id == s.Id))
                        Services.Add(new ServiceViewModel
                        {
                            Id = s.Id,
                            Name = s.Name,
                            Price = s.GetPrice(SelectedBodyTypeCategory),
                            IsSelected = selectedIds.Contains(s.Id)
                        });
                }
                var existingIds = new HashSet<int>(allServices.Select(s => s.Id));
                foreach (var s in Services.Where(s => !existingIds.Contains(s.Id)).ToList())
                    Services.Remove(s);
            }

            System.Diagnostics.Debug.WriteLine($"LoadServices: загружено {Services?.Count} услуг");
            foreach (var s in Services)
            {
                System.Diagnostics.Debug.WriteLine($"  Услуга: Id={s.Id}, Name={s.Name}, Price={s.Price}, IsSelected={s.IsSelected}");
            }

            Recalculate();
        }

        public void UpdateServicePrices()
        {
            if (Services != null)
            {
                var allServices = _sqliteDataService.GetAllServices();
                foreach (var vm in Services)
                {
                    var service = allServices.FirstOrDefault(s => s.Id == vm.Id);
                    if (service != null)
                    {
                        vm.Price = service.GetPrice(SelectedBodyTypeCategory);
                    }
                }
                Recalculate();
            }
        }

        public bool Validate()
        {
            if (string.IsNullOrWhiteSpace(CurrentOrder.CarModel)) { MessageBox.Show("Введите марку и модель автомобиля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return false; }
            if (string.IsNullOrWhiteSpace(CurrentOrder.CarNumber)) { MessageBox.Show("Введите государственный номер", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return false; }
            if (Services?.Any(s => s.IsSelected) != true) { MessageBox.Show("Выберите хотя бы одну услугу", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return false; }
            if (ExtraCost > 0 && string.IsNullOrWhiteSpace(CurrentOrder.ExtraCostReason)) { MessageBox.Show("Укажите причину дополнительной стоимости", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return false; }
            return true;
        }

        public void SaveOrder(out bool success, out string message)
        {
            success = false; message = "";
            if (!Validate()) return;

            var selectedServices = Services.Where(s => s.IsSelected).ToList();
            var serviceIds = selectedServices.Select(s => s.Id).ToList();

            CurrentOrder.ServiceIds = serviceIds;
            CurrentOrder.TotalPrice = ServicesTotal;
            CurrentOrder.BodyTypeCategory = SelectedBodyTypeCategory;
            CurrentOrder.OriginalTotalPrice = ServicesTotal;
            CurrentOrder.DiscountPercent = DiscountPercent;
            CurrentOrder.DiscountAmount = DiscountAmount;

            if (!IsEditMode && !IsAppointment)
            {
                if (CurrentOrder.Id == 0)
                {
                    CurrentOrder.ShiftId = _currentShift?.Id ?? 0;
                    _sqliteDataService.AddOrder(CurrentOrder, serviceIds);
                    message = "Заказ добавлен!"; success = true;
                }
                else message = "Ошибка: ID заказа уже существует";
            }
            else if (IsAppointment)
            {
                var apt = _sqliteDataService.GetAppointmentById(CurrentOrder.AppointmentId.Value);
                if (apt != null)
                {
                    apt.CarModel = CurrentOrder.CarModel; apt.CarNumber = CurrentOrder.CarNumber; apt.CarBodyType = CurrentOrder.CarBodyType;
                    apt.BodyTypeCategory = SelectedBodyTypeCategory; apt.AppointmentDate = CurrentOrder.Time; apt.BoxNumber = CurrentOrder.BoxNumber;
                    apt.ServiceIds = serviceIds; apt.ExtraCost = ExtraCost; apt.ExtraCostReason = CurrentOrder.ExtraCostReason;
                    _sqliteDataService.UpdateAppointment(apt);
                    message = "Запись обновлена!"; success = true;
                }
                else message = "Запись не найдена";
            }
            else
            {
                _sqliteDataService.UpdateOrder(CurrentOrder);
                message = $"Заказ обновлен!\n\n🚗 {CurrentOrder.CarModel} ({CurrentOrder.CarNumber})\n💰 Итого: {FinalTotal:N0} ₽";
                if (ExtraCost > 0) message += $"\n➕ Дополнительно: {ExtraCost:N0} ₽";
                success = true;
            }
        }

        public void Cleanup()
        {
            if (_isSubscribedToDataChanged) { SqliteDataService.DataChanged -= OnDataChanged; _isSubscribedToDataChanged = false; }
        }
    }
}
