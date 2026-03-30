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
        private readonly Shift _currentShift;
        private readonly CarWashOrder _existingOrder;
        private readonly bool _isEditMode;
        private readonly bool _isAppointment;

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

        public AddEditOrderViewModel(DataService dataService, Shift currentShift, CarWashOrder order = null)
        {
            _dataService = dataService;
            _currentShift = currentShift;
            _existingOrder = order;
            _isEditMode = order != null && order.Id > 0;
            _isAppointment = order != null && order.IsAppointment && order.Id == 0;

            InitializeOrder();
            LoadWashers();
            LoadServices();
        }

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
                    ShiftId = _existingOrder.ShiftId
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
                    IsAppointment = true,
                    AppointmentId = _existingOrder.AppointmentId
                };
                SelectedBodyTypeCategory = CurrentOrder.BodyTypeCategory;
                ExtraCost = CurrentOrder.ExtraCost;
                _windowTitle = "✏ Редактирование записи";
            }
            else
            {
                // Новый заказ
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
                    Status = "В ожидании",
                    PaymentMethod = "Наличные",
                    IsAppointment = false
                };
                _windowTitle = "➕ Добавление заказа";
            }
        }

        private void LoadWashers()
        {
            if (_currentShift != null && _currentShift.EmployeeIds != null && _currentShift.EmployeeIds.Any())
            {
                var allUsers = _dataService.GetAllUsers();
                Washers = allUsers.Where(u => _currentShift.EmployeeIds.Contains(u.Id)).ToList();
            }
            else
            {
                Washers = new List<User>();
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

                var appData = FileDataService.LoadData();

                if (!IsEditMode && !IsAppointment)
                {
                    // НОВЫЙ ЗАКАЗ
                    CurrentOrder.Id = GetNextOrderId(appData);
                    CurrentOrder.ShiftId = _currentShift?.Id ?? 0;

                    var shift = appData.Shifts.FirstOrDefault(s => s.Id == CurrentOrder.ShiftId);
                    if (shift != null)
                    {
                        if (shift.Orders == null) shift.Orders = new List<CarWashOrder>();
                        shift.Orders.Add(CurrentOrder);

                        // Обновляем статистику клиента для нового заказа
                        if (CurrentOrder.ClientId.HasValue && CurrentOrder.Status == "Выполнен")
                        {
                            var client = appData.Clients.FirstOrDefault(c => c.Id == CurrentOrder.ClientId.Value);
                            if (client != null)
                            {
                                client.VisitsCount++;
                                client.TotalSpent += CurrentOrder.FinalPrice;
                                client.LastVisitDate = DateTime.Now;
                            }
                        }

                        FileDataService.SaveData(appData);

                        message = $"Заказ добавлен!\n\n🚗 {CurrentOrder.CarModel} ({CurrentOrder.CarNumber})\n💰 Итого: {FinalTotal:N0} ₽";
                        success = true;
                    }
                    else
                    {
                        message = "Смена не найдена";
                    }
                }
                else if (IsAppointment)
                {
                    // РЕДАКТИРОВАНИЕ ЗАПИСИ
                    var appointment = appData.Appointments.FirstOrDefault(a => a.Id == CurrentOrder.AppointmentId.Value);
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

                        FileDataService.SaveData(appData);

                        message = $"Запись обновлена!\n\n🚗 {CurrentOrder.CarModel} ({CurrentOrder.CarNumber})";
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
                    var shift = appData.Shifts.FirstOrDefault(s => s.Id == CurrentOrder.ShiftId);
                    if (shift != null)
                    {
                        var existingOrder = shift.Orders.FirstOrDefault(o => o.Id == CurrentOrder.Id);
                        if (existingOrder != null)
                        {
                            // Сохраняем старый статус для обновления статистики
                            string oldStatus = existingOrder.Status;
                            string newStatus = CurrentOrder.Status;

                            // Обновляем поля заказа
                            existingOrder.CarModel = CurrentOrder.CarModel;
                            existingOrder.CarNumber = CurrentOrder.CarNumber;
                            existingOrder.CarBodyType = CurrentOrder.CarBodyType;
                            existingOrder.BodyTypeCategory = CurrentOrder.BodyTypeCategory;
                            existingOrder.Time = CurrentOrder.Time;
                            existingOrder.BoxNumber = CurrentOrder.BoxNumber;
                            existingOrder.WasherId = CurrentOrder.WasherId;
                            existingOrder.ServiceIds = serviceIds;
                            existingOrder.ExtraCost = ExtraCost;
                            existingOrder.ExtraCostReason = CurrentOrder.ExtraCostReason;
                            existingOrder.Status = newStatus;
                            existingOrder.PaymentMethod = CurrentOrder.PaymentMethod;
                            existingOrder.TotalPrice = ServicesTotal;

                            // Обновляем статистику клиента только если изменился статус
                            if (oldStatus != newStatus && existingOrder.ClientId.HasValue)
                            {
                                UpdateClientStatsForOrder(appData, existingOrder, oldStatus, newStatus);
                            }

                            FileDataService.SaveData(appData);

                            message = $"Заказ обновлен!\n\n🚗 {CurrentOrder.CarModel} ({CurrentOrder.CarNumber})\n💰 Итого: {FinalTotal:N0} ₽";
                            success = true;
                        }
                        else
                        {
                            message = "Заказ не найден";
                        }
                    }
                    else
                    {
                        message = "Смена не найдена";
                    }
                }
            }
            catch (Exception ex)
            {
                message = $"Ошибка при сохранении: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"SaveOrder error: {ex}");
            }
        }

        /// <summary>
        /// Вспомогательный метод для обновления статистики клиента при изменении статуса
        /// </summary>
        private void UpdateClientStatsForOrder(AppData appData, CarWashOrder order, string oldStatus, string newStatus)
        {
            var client = appData.Clients.FirstOrDefault(c => c.Id == order.ClientId.Value);
            if (client == null) return;

            bool wasCompleted = oldStatus == "Выполнен";
            bool willBeCompleted = newStatus == "Выполнен";

            // Если статус не менялся на "Выполнен" или с "Выполнен" - ничего не делаем
            if (wasCompleted == willBeCompleted) return;

            if (willBeCompleted && !wasCompleted)
            {
                // Заказ стал выполненным - добавляем статистику
                client.VisitsCount++;
                client.TotalSpent += order.FinalPrice;
                client.LastVisitDate = DateTime.Now;

                System.Diagnostics.Debug.WriteLine($"Статистика увеличена: клиент {client.FullName}, +{order.FinalPrice:N0} ₽");
            }
            else if (!willBeCompleted && wasCompleted)
            {
                // Заказ был выполнен, но стал отменен/в ожидании - вычитаем статистику
                client.VisitsCount--;
                client.TotalSpent -= order.FinalPrice;

                // Обновляем дату последнего визита
                var lastCompletedOrder = appData.Shifts
                    .SelectMany(s => s.Orders ?? new List<CarWashOrder>())
                    .Where(o => o.ClientId == client.Id && o.Id != order.Id && o.Status == "Выполнен")
                    .OrderByDescending(o => o.Time)
                    .FirstOrDefault();

                client.LastVisitDate = lastCompletedOrder?.Time ?? client.RegistrationDate;

                System.Diagnostics.Debug.WriteLine($"Статистика уменьшена: клиент {client.FullName}, -{order.FinalPrice:N0} ₽");
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
