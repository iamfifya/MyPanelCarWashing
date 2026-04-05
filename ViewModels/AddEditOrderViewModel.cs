// ViewModels/AddEditOrderViewModel.cs
using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
using System;
using System.Collections.Generic;
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
        public AddEditOrderViewModel(DataService dataService)
        {
            _dataService = dataService;
        }
        public void Initialize(Shift currentShift, CarWashOrder order = null)
        {
            _currentShift = currentShift;
            _existingOrder = order;
            _isEditMode = order != null && order.Id > 0;
            _isAppointment = order != null && order.IsAppointment && order.Id == 0;

            InitializeOrder();
            LoadWashers();
            LoadServices();
        }

        private CarWashOrder _currentOrder;
        private List<ServiceViewModel> _services;
        private List<User> _washers;
        private decimal _servicesTotal;
        private decimal _extraCost;
        private int _selectedBodyTypeCategory = 1;
        private string _windowTitle;

        public CarWashOrder CurrentOrder
        {
            get => _currentOrder;
            set
            {
                _currentOrder = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentOrder)));
            }
        }

        public List<ServiceViewModel> Services
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

        public decimal FinalTotal => ServicesTotal + ExtraCost;
        public string WindowTitle => _windowTitle;
        public bool IsEditMode => _isEditMode;
        public bool IsAppointment => _isAppointment;

        private void InitializeOrder()
        {
            if (_existingOrder != null && _isEditMode)
            {
                // Редактирование существующего заказа
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
                    Notes = _existingOrder.Notes
                };
                SelectedBodyTypeCategory = CurrentOrder.BodyTypeCategory;
                ExtraCost = CurrentOrder.ExtraCost;
                _windowTitle = "✏ Редактирование заказа";
            }
            else if (_existingOrder != null && _isAppointment)
            {
                // Редактирование предварительной записи
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
                    Notes = _existingOrder.Notes
                };
                SelectedBodyTypeCategory = CurrentOrder.BodyTypeCategory;
                ExtraCost = CurrentOrder.ExtraCost;
                _windowTitle = "✏ Редактирование записи";
            }
            else
            {
                // НОВЫЙ ЗАКАЗ
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
                    Status = "Выполняется",  // ← ИСПРАВЛЕНО
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

            // Если есть активная смена, показываем сотрудников смены + предупреждение о других
            if (_currentShift != null && _currentShift.EmployeeIds != null && _currentShift.EmployeeIds.Any())
            {
                // Показываем всех сотрудников, но с маркировкой кто в смене
                Washers = allUsers;

                // Для отладки
                System.Diagnostics.Debug.WriteLine($"Загружено мойщиков: {Washers.Count}");
                foreach (var w in Washers)
                {
                    bool inShift = _currentShift.EmployeeIds.Contains(w.Id);
                    System.Diagnostics.Debug.WriteLine($"  {w.FullName} - {(inShift ? "в смене" : "не в смене")}");
                }
            }
            else
            {
                // Если нет активной смены, показываем всех
                Washers = allUsers;
                System.Diagnostics.Debug.WriteLine($"Нет активной смены, загружено всех мойщиков: {Washers.Count}");
            }
        }

        private void LoadServices()
        {
            var allServices = _dataService.GetAllServices();
            Services = allServices.Select(s => new ServiceViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Price = s.GetPrice(SelectedBodyTypeCategory),
                IsSelected = CurrentOrder.ServiceIds.Contains(s.Id)
            }).ToList();
        }

        private void UpdateServicePrices()
        {
            if (Services != null)
            {
                var allServices = _dataService.GetAllServices();
                foreach (var serviceVM in Services)
                {
                    var originalService = allServices.FirstOrDefault(s => s.Id == serviceVM.Id);
                    if (originalService != null)
                    {
                        serviceVM.Price = originalService.GetPrice(SelectedBodyTypeCategory);
                    }
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Services)));
            }
        }

        public void CalculateTotal()
        {
            ServicesTotal = Services?.Where(s => s.IsSelected).Sum(s => s.Price) ?? 0;
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

                if (!IsEditMode && !IsAppointment)
                {
                    // НОВЫЙ ЗАКАЗ - проверяем, что заказ еще не существует
                    if (CurrentOrder.Id == 0)
                    {
                        CurrentOrder.ShiftId = _currentShift?.Id ?? 0;
                        _dataService.AddOrder(CurrentOrder, serviceIds);
                        message = $"Заказ добавлен!";
                        success = true;
                    }
                    else
                    {
                        message = "Ошибка: ID заказа уже существует";
                    }
                }
                else if (IsAppointment)
                {
                    // РЕДАКТИРОВАНИЕ ЗАПИСИ
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

                        message = $"Запись обновлена!\n\n🚗 {CurrentOrder.CarModel} ({CurrentOrder.CarNumber})";
                        if (ExtraCost > 0)
                            message += $"\n➕ Дополнительно: {ExtraCost:N0} ₽";
                        success = true;
                    }
                    else
                    {
                        message = "Запись не найдена";
                    }
                }
                else
                {
                    // РЕДАКТИРОВАНИЕ СУЩЕСТВУЮЩЕГО ЗАКАЗА
                    _dataService.UpdateOrder(CurrentOrder);

                    message = $"Заказ обновлен!\n\n🚗 {CurrentOrder.CarModel} ({CurrentOrder.CarNumber})\n💰 Итого: {FinalTotal:N0} ₽";
                    if (ExtraCost > 0)
                        message += $"\n➕ Дополнительно: {ExtraCost:N0} ₽";
                    success = true;
                }
            }
            catch (Exception ex)
            {
                message = $"Ошибка при сохранении: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"SaveOrder error: {ex}");
            }
        }

        // Добавьте этот вспомогательный метод в класс AddEditOrderViewModel
        private void UpdateClientStatsForOrder(AppData appData, CarWashOrder order, string oldStatus, string newStatus)
        {
            var client = appData.Clients.FirstOrDefault(c => c.Id == order.ClientId.Value);
            if (client == null) return;

            bool wasCompleted = oldStatus == "Выполнен";
            bool willBeCompleted = newStatus == "Выполнен";

            if (wasCompleted == willBeCompleted) return;

            if (willBeCompleted && !wasCompleted)
            {
                client.VisitsCount++;
                client.TotalSpent += order.FinalPrice;
                client.LastVisitDate = DateTime.Now;
            }
            else if (!willBeCompleted && wasCompleted)
            {
                client.VisitsCount--;
                client.TotalSpent -= order.FinalPrice;

                var lastCompletedOrder = appData.Shifts
                    .SelectMany(s => s.Orders ?? new List<CarWashOrder>())
                    .Where(o => o.ClientId == client.Id && o.Id != order.Id && o.Status == "Выполнен")
                    .OrderByDescending(o => o.Time)
                    .FirstOrDefault();

                client.LastVisitDate = lastCompletedOrder?.Time ?? client.RegistrationDate;
            }
        }

        private int GetNextOrderId(AppData appData)
        {
            int maxId = 0;
            foreach (var shift in appData.Shifts)
            {
                if (shift.Orders != null && shift.Orders.Any())
                {
                    var maxInShift = shift.Orders.Max(o => o.Id);
                    if (maxInShift > maxId) maxId = maxInShift;
                }
            }
            return maxId + 1;
        }
    }
}
