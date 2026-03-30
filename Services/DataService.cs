using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MyPanelCarWashing.Models;
using System.Threading.Tasks;

namespace MyPanelCarWashing.Services
{
    public class DataService
    {
        private AppData _data;

        public DataService()
        {
            LoadData();
            CleanupDuplicateServices(); // Очищаем дубликаты при загрузке
            CleanupInvalidData(); // Добавляем очистку некорректных данных
            _data.UpdateIds();
        }

        private void LoadData()
        {
            _data = FileDataService.LoadData();
            _data.UpdateIds();
        }

        public void SaveData()
        {
            FileDataService.SaveData(_data);
        }

        // Services/DataService.cs - добавьте эти методы

        public void AddOrderTransactional(CarWashOrder order, List<int> serviceIds)
        {
            TransactionService.ExecuteInTransaction(appData =>
            {
                var shift = appData.Shifts.FirstOrDefault(s => s.Id == order.ShiftId);
                if (shift == null)
                    throw new Exception("Смена не найдена");

                order.Id = GetNextOrderId(appData);
                order.ServiceIds = serviceIds;
                order.TotalPrice = appData.Services
                    .Where(s => serviceIds.Contains(s.Id))
                    .Sum(s => s.GetPrice(order.BodyTypeCategory));

                if (shift.Orders == null) shift.Orders = new List<CarWashOrder>();
                shift.Orders.Add(order);

                // Обновляем статистику клиента
                if (order.ClientId.HasValue)
                {
                    var client = appData.Clients.FirstOrDefault(c => c.Id == order.ClientId.Value);
                    if (client != null)
                    {
                        client.VisitsCount++;
                        client.TotalSpent += order.FinalPrice;
                        client.LastVisitDate = DateTime.Now;
                    }
                }

                return true;
            });
        }

        public CarWashOrder ConvertAppointmentToOrderTransactional(Appointment appointment, int shiftId, int washerId)
        {
            return TransactionService.ExecuteInTransaction(appData =>
            {
                var shift = appData.Shifts.FirstOrDefault(s => s.Id == shiftId);
                if (shift == null)
                    throw new Exception("Смена не найдена");

                var app = appData.Appointments.FirstOrDefault(a => a.Id == appointment.Id);
                if (app == null)
                    throw new Exception("Запись не найдена");

                var selectedServices = appData.Services.Where(s => app.ServiceIds.Contains(s.Id)).ToList();
                decimal totalPrice = selectedServices.Sum(s => s.GetPrice(app.BodyTypeCategory));

                var order = new CarWashOrder
                {
                    Id = GetNextOrderId(appData),
                    CarNumber = app.CarNumber,
                    CarModel = app.CarModel,
                    CarBodyType = app.CarBodyType,
                    BodyTypeCategory = app.BodyTypeCategory,
                    Time = app.AppointmentDate,
                    ShiftId = shiftId,
                    BoxNumber = app.BoxNumber,
                    WasherId = washerId,
                    ServiceIds = app.ServiceIds.ToList(),
                    ExtraCost = app.ExtraCost,
                    ExtraCostReason = app.ExtraCostReason,
                    Notes = app.Notes,
                    Status = "В ожидании",
                    TotalPrice = totalPrice,
                    IsAppointment = true,
                    AppointmentId = app.Id
                };

                shift.Orders.Add(order);
                app.IsCompleted = true;
                app.OrderId = order.Id;

                // Обновляем статистику клиента
                if (order.ClientId.HasValue)
                {
                    var client = appData.Clients.FirstOrDefault(c => c.Id == order.ClientId.Value);
                    if (client != null)
                    {
                        client.VisitsCount++;
                        client.TotalSpent += order.FinalPrice;
                        client.LastVisitDate = DateTime.Now;
                    }
                }

                return order;
            });
        }

        public static event Action DataChanged;

        public static void NotifyDataChanged()
        {
            DataChanged?.Invoke();
        }

        // Пользователи
        public User AuthenticateUser(string login, string password)
        {
            return _data.Users.FirstOrDefault(u => u.Login == login && u.Password == password);
        }

        public List<User> GetAllUsers()
        {
            return _data.Users.ToList();
        }

        public void AddUser(User user)
        {
            user.Id = _data.GetNextUserId();
            _data.Users.Add(user);
            SaveData();
        }

        // Услуги
        public List<Service> GetAllServices()
        {
            return _data.Services.Where(s => s.IsActive).ToList();
        }

        // Смены
        public Shift GetShiftByDate(DateTime date)
        {
            // Используем только дату, игнорируя время
            var targetDate = date.Date;

            System.Diagnostics.Debug.WriteLine($"GetShiftByDate ищет смену на {targetDate:dd.MM.yyyy}");

            // Выводим все смены для отладки
            foreach (var s in _data.Shifts)
            {
                System.Diagnostics.Debug.WriteLine($"  Смена: ID={s.Id}, Дата={s.Date:dd.MM.yyyy}, IsClosed={s.IsClosed}");
            }

            var openShift = _data.Shifts.FirstOrDefault(s => s.Date.Date == targetDate && !s.IsClosed);

            System.Diagnostics.Debug.WriteLine($"Результат: {(openShift != null ? $"найдена ID={openShift.Id}" : "не найдена")}");

            return openShift;
        }

        public List<Shift> GetAllShifts()
        {
            return _data.Shifts.OrderByDescending(s => s.Date).ToList();
        }

        public void CloseShift(int shiftId, string notes)
        {
            var shift = _data.Shifts.FirstOrDefault(s => s.Id == shiftId);
            if (shift != null && !shift.IsClosed)
            {
                shift.EndTime = DateTime.Now;
                shift.IsClosed = true;
                shift.Notes = notes;
                SaveData();
            }
        }

        // Заказы
        public void AddOrder(CarWashOrder order, List<int> serviceIds)
        {
            var appData = FileDataService.LoadData();
            var shift = appData.Shifts.FirstOrDefault(s => s.Id == order.ShiftId);

            if (shift != null)
            {
                if (shift.Orders == null) shift.Orders = new List<CarWashOrder>();

                // Устанавливаем ID заказа
                order.Id = GetNextOrderId(appData);
                order.ServiceIds = serviceIds;

                // Рассчитываем сумму с учетом категории кузова (сохраняем существующую логику)
                order.TotalPrice = appData.Services
                    .Where(s => serviceIds.Contains(s.Id))
                    .Sum(s => s.GetPrice(order.BodyTypeCategory));

                shift.Orders.Add(order);

                // Обновляем статистику клиента (если клиент выбран)
                if (order.ClientId.HasValue)
                {
                    UpdateClientStats(order.ClientId.Value, order.FinalPrice);
                }

                FileDataService.SaveData(appData);
            }
        }

        public void UpdateOrderServices(int orderId, List<int> serviceIds)
        {
            var order = GetOrderById(orderId);
            if (order != null)
            {
                order.ServiceIds = serviceIds;
                order.TotalPrice = _data.Services
                    .Where(s => serviceIds.Contains(s.Id))
                    .Sum(s => s.GetPrice(order.BodyTypeCategory)); // Используем GetPrice
                SaveData();
            }
        }

        // DataService.cs
        public CarWashOrder GetOrderById(int orderId)
        {
            try
            {
                var appData = FileDataService.LoadData();
                foreach (var shift in appData.Shifts)
                {
                    var order = shift.Orders?.FirstOrDefault(o => o.Id == orderId);
                    if (order != null)
                        return order;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public List<int> GetOrderServiceIds(int orderId)
        {
            var order = GetOrderById(orderId);
            return order?.ServiceIds ?? new List<int>();
        }

        // DataService.cs - добавьте эти методы

        public void UpdateAppointment(Appointment appointment)
        {
            try
            {
                var appData = FileDataService.LoadData();
                var existing = appData.Appointments.FirstOrDefault(a => a.Id == appointment.Id);

                if (existing != null)
                {
                    existing.CarModel = appointment.CarModel;
                    existing.CarNumber = appointment.CarNumber;
                    existing.CarBodyType = appointment.CarBodyType;
                    existing.AppointmentDate = appointment.AppointmentDate;
                    existing.DurationMinutes = appointment.DurationMinutes;
                    existing.BoxNumber = appointment.BoxNumber;
                    existing.ServiceIds = appointment.ServiceIds;
                    existing.ExtraCost = appointment.ExtraCost;
                    existing.ExtraCostReason = appointment.ExtraCostReason;
                    existing.Notes = appointment.Notes;
                    existing.IsCompleted = appointment.IsCompleted;
                }

                FileDataService.SaveData(appData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка UpdateAppointment: {ex.Message}");
                throw;
            }
        }

        public void UpdateOrder(CarWashOrder order)
        {
            try
            {
                var appData = FileDataService.LoadData();
                var shift = appData.Shifts.FirstOrDefault(s => s.Id == order.ShiftId);

                if (shift != null)
                {
                    var existingOrder = shift.Orders.FirstOrDefault(o => o.Id == order.Id);
                    if (existingOrder != null)
                    {
                        existingOrder.CarModel = order.CarModel;
                        existingOrder.CarNumber = order.CarNumber;
                        existingOrder.CarBodyType = order.CarBodyType;
                        existingOrder.BodyTypeCategory = order.BodyTypeCategory;
                        existingOrder.Time = order.Time;
                        existingOrder.BoxNumber = order.BoxNumber;
                        existingOrder.WasherId = order.WasherId;
                        existingOrder.ServiceIds = order.ServiceIds;
                        existingOrder.ExtraCost = order.ExtraCost;
                        existingOrder.ExtraCostReason = order.ExtraCostReason;
                        existingOrder.TotalPrice = order.TotalPrice;
                        existingOrder.Status = order.Status;
                        existingOrder.PaymentMethod = order.PaymentMethod;
                        existingOrder.Notes = order.Notes;
                    }
                }

                FileDataService.SaveData(appData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка UpdateOrder: {ex.Message}");
                throw;
            }
        }

        private void MigrateOldServices()
        {
            bool needSave = false;

            foreach (var service in _data.Services)
            {
                // Если у услуги есть старый Price и нет PriceByBodyType
                if (service.PriceByBodyType == null || !service.PriceByBodyType.Any())
                {
                    service.PriceByBodyType = new Dictionary<int, decimal>();

                    // Если есть старая цена, используем её для всех категорий
                    // (через рефлексию или временное поле)
                    // В вашем случае у Service уже есть Price? Проверьте.
                }
            }

            if (needSave)
                SaveData();
        }

        public void AddEmployeeToShift(int shiftId, int userId)
        {
            var shift = _data.Shifts.FirstOrDefault(s => s.Id == shiftId);
            if (shift != null && !shift.EmployeeIds.Contains(userId))
            {
                shift.EmployeeIds.Add(userId);
                SaveData();
            }
        }

        // Отчеты
        public List<MonthlyReport> GetMonthlyReport(int year, int month)
        {
            var shifts = _data.Shifts
                .Where(s => s.Date.Year == year && s.Date.Month == month && s.IsClosed)
                .ToList();

            return new List<MonthlyReport>
            {
                new MonthlyReport
                {
                    Year = year,
                    Month = month,
                    TotalCars = shifts.Sum(s => s.TotalCars),
                    TotalRevenue = shifts.Sum(s => s.TotalRevenue)
                }
            };
        }
        // Добавить в класс DataService
        public bool IsShiftStarted(DateTime date)
        {
            return _data.Shifts.Any(s => s.Date.Date == date.Date && !s.IsClosed);
        }

        public Shift GetActiveShift()
        {
            return _data.Shifts.FirstOrDefault(s => !s.IsClosed);
        }

        public void StartShift(DateTime date, List<int> employeeIds)
        {
            System.Diagnostics.Debug.WriteLine($"StartShift вызван для даты {date:dd.MM.yyyy}");

            // Удаляем существующую открытую смену на эту дату
            var existingShift = _data.Shifts.FirstOrDefault(s => s.Date.Date == date.Date && !s.IsClosed);
            if (existingShift != null)
            {
                System.Diagnostics.Debug.WriteLine($"  Удаляем существующую смену ID={existingShift.Id}");
                _data.Shifts.Remove(existingShift);
            }

            // Создаем новую смену
            var shift = new Shift
            {
                Id = _data.GetNextShiftId(),
                Date = date,
                StartTime = DateTime.Now,
                IsClosed = false,
                EmployeeIds = employeeIds,
                Orders = new List<CarWashOrder>()
            };

            System.Diagnostics.Debug.WriteLine($"  Создаем новую смену ID={shift.Id}, Дата={shift.Date:dd.MM.yyyy}");
            _data.Shifts.Add(shift);
            SaveData();

            // Проверяем, что сохранилось
            var saved = _data.Shifts.FirstOrDefault(s => s.Id == shift.Id);
            System.Diagnostics.Debug.WriteLine($"  После сохранения: {(saved != null ? $"OK, ID={saved.Id}, Дата={saved.Date:dd.MM.yyyy}" : "НЕ СОХРАНИЛОСЬ!")}");
        }
        public Shift GetShiftById(int shiftId)
        {
            return _data.Shifts.FirstOrDefault(s => s.Id == shiftId);
        }
        public async Task<List<Service>> GetAllServicesAsync()
        {
            return await Task.Run(() => _data.Services.Where(s => s.IsActive).ToList());
        }

        public async Task<Shift> GetShiftByDateAsync(DateTime date)
        {
            return await Task.Run(() => _data.Shifts.FirstOrDefault(s => s.Date.Date == date.Date && !s.IsClosed));
        }

        public async Task AddOrderAsync(CarWashOrder order, List<int> serviceIds)
        {
            await Task.Run(() => AddOrder(order, serviceIds));
        }
        // Получить все записи
        public List<Appointment> GetAllAppointments()
        {
            return _data.Appointments.OrderBy(a => a.AppointmentDate).ToList();
        }

        // Получить записи на конкретную дату
        public List<Appointment> GetAppointmentsByDate(DateTime date)
        {
            return _data.Appointments
                .Where(a => a.AppointmentDate.Date == date.Date && !a.IsCompleted)
                .OrderBy(a => a.AppointmentDate)
                .ToList();
        }

        public bool IsBoxAvailable(int boxNumber, DateTime startTime, int durationMinutes)
        {
            var endTime = startTime.AddMinutes(durationMinutes);

            // Получаем ВСЕ записи на эту дату (независимо от бокса, так как мойка одна)
            var appointments = _data.Appointments
                .Where(a => !a.IsCompleted && a.AppointmentDate.Date == startTime.Date)
                .ToList();

            // Проверяем пересечения
            foreach (var a in appointments)
            {
                var aStart = a.AppointmentDate;
                var aEnd = a.EndTime;

                // Если интервалы пересекаются
                if (endTime > aStart && startTime < aEnd)
                {
                    return false;
                }
            }

            return true;
        }

        public void ClearAllData()
        {
            _data.Shifts.Clear();
            _data.Appointments.Clear();
            SaveData();
            System.Diagnostics.Debug.WriteLine("Все данные очищены");
        }

        // DataService.cs - добавьте эти методы

        public List<EmployeeSchedule> GetSchedule(int year, int month)
        {
            var appData = FileDataService.LoadData();
            var schedule = appData.Schedules?.FirstOrDefault(s => s.Year == year && s.Month == month);

            if (schedule != null)
            {
                System.Diagnostics.Debug.WriteLine($"Найден график: {schedule.Year}.{schedule.Month}, сотрудников: {schedule.EmployeeSchedules.Count}");
                foreach (var emp in schedule.EmployeeSchedules)
                {
                    System.Diagnostics.Debug.WriteLine($"  {emp.EmployeeName} - {emp.Position}, дней: {emp.Days.Count}");
                }
                return schedule.EmployeeSchedules;
            }

            System.Diagnostics.Debug.WriteLine($"График на {year}.{month} не найден");
            return new List<EmployeeSchedule>();
        }

        public void SaveSchedule(int year, int month, List<EmployeeSchedule> scheduleData)
        {
            var appData = FileDataService.LoadData();

            // Инициализируем список, если null
            if (appData.Schedules == null)
                appData.Schedules = new List<Schedule>();

            // Ищем существующий график
            var existingSchedule = appData.Schedules.FirstOrDefault(s => s.Year == year && s.Month == month);

            if (existingSchedule != null)
            {
                // Обновляем существующий
                existingSchedule.EmployeeSchedules = scheduleData;
                System.Diagnostics.Debug.WriteLine($"Обновлен график на {year}.{month}");
            }
            else
            {
                // Создаем новый
                int newId = appData.Schedules.Any() ? appData.Schedules.Max(s => s.Id) + 1 : 1;
                appData.Schedules.Add(new Schedule
                {
                    Id = newId,
                    Year = year,
                    Month = month,
                    EmployeeSchedules = scheduleData
                });
                System.Diagnostics.Debug.WriteLine($"Создан новый график на {year}.{month}, ID: {newId}");
            }

            FileDataService.SaveData(appData);
        }

        private void CleanupInvalidData()
        {
            // Удаляем заказы, у которых IsAppointment = true, но нет соответствующей записи
            var invalidOrders = _data.Shifts
                .SelectMany(s => s.Orders)
                .Where(o => o.IsAppointment && !_data.Appointments.Any(a => a.Id == o.AppointmentId))
                .ToList();

            foreach (var order in invalidOrders)
            {
                var shift = _data.Shifts.FirstOrDefault(s => s.Orders.Contains(order));
                if (shift != null)
                {
                    shift.Orders.Remove(order);
                    System.Diagnostics.Debug.WriteLine($"Удален некорректный заказ ID={order.Id}");
                }
            }

            // Удаляем пустые смены
            _data.Shifts.RemoveAll(s => s.Orders.Count == 0 && s.IsClosed);

            if (invalidOrders.Any())
            {
                SaveData();
            }
        }


        // Добавить запись
        public void AddAppointment(Appointment appointment)
        {
            // Проверяем доступность перед добавлением
            if (!IsBoxAvailable(appointment.BoxNumber, appointment.AppointmentDate, appointment.DurationMinutes))
            {
                throw new Exception("Выбранное время уже занято!");
            }

            appointment.Id = _data.GetNextAppointmentId();
            _data.Appointments.Add(appointment);
            SaveData();

            System.Diagnostics.Debug.WriteLine($"=== ЗАПИСЬ ДОБАВЛЕНА ===");
            System.Diagnostics.Debug.WriteLine($"ID: {appointment.Id}");
            System.Diagnostics.Debug.WriteLine($"Время: {appointment.AppointmentDate:HH:mm} - {appointment.EndTime:HH:mm}");
        }

        // Удалить запись
        public void DeleteAppointment(int appointmentId)
        {
            var appointment = _data.Appointments.FirstOrDefault(a => a.Id == appointmentId);
            if (appointment != null)
            {
                _data.Appointments.Remove(appointment);
                SaveData();
            }
        }

        // Преобразовать запись в заказ при наступлении смены
        public CarWashOrder ConvertAppointmentToOrder(Appointment appointment, int shiftId, int washerId)
        {
            var selectedServices = _data.Services.Where(s => appointment.ServiceIds.Contains(s.Id)).ToList();
            // Исправлено: используем GetPrice с категорией по умолчанию 1
            decimal totalPrice = selectedServices.Sum(s => s.GetPrice(1));

            var order = new CarWashOrder
            {
                Id = _data.GetNextOrderId(),
                CarNumber = appointment.CarNumber,
                CarModel = appointment.CarModel,
                CarBodyType = appointment.CarBodyType,
                Time = appointment.AppointmentDate,
                ShiftId = shiftId,
                BoxNumber = appointment.BoxNumber,
                WasherId = washerId,
                ServiceIds = appointment.ServiceIds.ToList(),
                ExtraCost = appointment.ExtraCost,
                ExtraCostReason = appointment.ExtraCostReason,
                Notes = appointment.Notes,
                Status = "В ожидании",
                TotalPrice = totalPrice,
                IsAppointment = true,
                AppointmentId = appointment.Id
            };

            appointment.IsCompleted = true;
            appointment.OrderId = order.Id;
            SaveData();

            return order;
        }
        public Appointment GetAppointmentById(int appointmentId)
        {
            return _data.Appointments.FirstOrDefault(a => a.Id == appointmentId);
        }
        public void UpdateOrderStatus(int orderId, string status)
        {
            var order = GetOrderById(orderId);
            if (order != null)
            {
                order.Status = status;
                SaveData();
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
        public Shift GetCurrentOpenShift()
        {
            try
            {
                var appData = FileDataService.LoadData();
                return appData.Shifts?.FirstOrDefault(s => !s.IsClosed && s.Date.Date == DateTime.Now.Date);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetCurrentOpenShift error: {ex.Message}");
                return null;
            }
        }

        private void CleanupDuplicateServices()
        {
            // Находим дубликаты по ID
            var duplicateIds = _data.Services
                .GroupBy(s => s.Id)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateIds.Any())
            {
                System.Diagnostics.Debug.WriteLine($"Найдены дубликаты ID: {string.Join(", ", duplicateIds)}");

                // Группируем по ID и оставляем только первую запись
                var cleanedServices = _data.Services
                    .GroupBy(s => s.Id)
                    .Select(g => g.First())
                    .OrderBy(s => s.Id)
                    .ToList();

                _data.Services = cleanedServices;

                // Находим дубликаты по названию
                var duplicateNames = _data.Services
                    .GroupBy(s => s.Name)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicateNames.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"Найдены дубликаты названий: {string.Join(", ", duplicateNames)}");

                    // Оставляем только одну услугу с каждым названием
                    cleanedServices = _data.Services
                        .GroupBy(s => s.Name)
                        .Select(g => g.First())
                        .OrderBy(s => s.Id)
                        .ToList();

                    _data.Services = cleanedServices;
                }

                // Перенумеровываем ID
                int newId = 1;
                foreach (var service in _data.Services.OrderBy(s => s.Id))
                {
                    service.Id = newId++;
                }

                SaveData();
                System.Diagnostics.Debug.WriteLine($"Дубликаты удалены. Осталось услуг: {_data.Services.Count}");
            }
        }
        public void AddService(Service service)
        {
            service.Id = _data.GetNextServiceId();
            _data.Services.Add(service);
            SaveData();

            // Исправлено: выводим информацию о ценах
            var prices = string.Join(", ", service.PriceByBodyType.Select(p => $"Кат.{p.Key}: {p.Value}"));
            System.Diagnostics.Debug.WriteLine($"Добавлена услуга: ID={service.Id}, Name={service.Name}, Цены: {prices}");
        }
        public List<Client> GetAllClients()
        {
            var appData = FileDataService.LoadData();
            return appData.Clients;
        }

        public Client GetClientById(int id)
        {
            var appData = FileDataService.LoadData();
            return appData.Clients.FirstOrDefault(c => c.Id == id);
        }

        public List<Client> FindClients(string searchText)
        {
            var appData = FileDataService.LoadData();
            if (string.IsNullOrWhiteSpace(searchText))
                return appData.Clients;

            return appData.Clients.Where(c =>
                c.FullName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                c.Phone.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                c.CarNumber.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0
            ).ToList();
        }

        public void AddClient(Client client)
        {
            var appData = FileDataService.LoadData();
            client.Id = appData.GetNextClientId();
            client.RegistrationDate = DateTime.Now;
            appData.Clients.Add(client);
            FileDataService.SaveData(appData);
        }

        public void UpdateClient(Client client)
        {
            var appData = FileDataService.LoadData();
            var existing = appData.Clients.FirstOrDefault(c => c.Id == client.Id);
            if (existing != null)
            {
                existing.FullName = client.FullName;
                existing.Phone = client.Phone;
                existing.CarModel = client.CarModel;
                existing.CarNumber = client.CarNumber;
                existing.Notes = client.Notes;
                FileDataService.SaveData(appData);
            }
        }

        public void DeleteClient(int clientId)
        {
            var appData = FileDataService.LoadData();
            var client = appData.Clients.FirstOrDefault(c => c.Id == clientId);
            if (client != null)
            {
                appData.Clients.Remove(client);
                FileDataService.SaveData(appData);
            }
        }

        // Обновляем статистику клиента при добавлении заказа
        public void UpdateClientStats(int clientId, decimal orderAmount)
        {
            var appData = FileDataService.LoadData();
            var client = appData.Clients.FirstOrDefault(c => c.Id == clientId);
            if (client != null)
            {
                client.VisitsCount++;
                client.TotalSpent += orderAmount;
                client.LastVisitDate = DateTime.Now;
                FileDataService.SaveData(appData);
            }
        }
        public List<CarWashOrder> GetOrdersByClientId(int clientId)
        {
            var appData = FileDataService.LoadData();
            var orders = new List<CarWashOrder>();

            foreach (var shift in appData.Shifts)
            {
                var clientOrders = shift.Orders?.Where(o => o.ClientId == clientId).ToList();
                if (clientOrders != null && clientOrders.Any())
                {
                    orders.AddRange(clientOrders);
                }
            }

            return orders;
        }
        /// <summary>
        /// Обновляет статистику клиента при изменении статуса заказа
        /// </summary>
        /// <param name="orderId">ID заказа</param>
        /// <param name="oldStatus">Старый статус</param>
        /// <param name="newStatus">Новый статус</param>
        public void UpdateClientStatsOnStatusChange(int orderId, string oldStatus, string newStatus)
        {
            var appData = FileDataService.LoadData();

            // Находим заказ
            CarWashOrder order = null;
            Shift shift = null;

            foreach (var s in appData.Shifts)
            {
                var foundOrder = s.Orders?.FirstOrDefault(o => o.Id == orderId);
                if (foundOrder != null)
                {
                    order = foundOrder;
                    shift = s;
                    break;
                }
            }

            if (order == null || !order.ClientId.HasValue) return;

            var client = appData.Clients.FirstOrDefault(c => c.Id == order.ClientId.Value);
            if (client == null) return;

            bool wasCompleted = oldStatus == "Выполнен";
            bool willBeCompleted = newStatus == "Выполнен";

            // Если статус не менялся или оба не "Выполнен" - ничего не делаем
            if (wasCompleted == willBeCompleted) return;

            if (willBeCompleted && !wasCompleted)
            {
                // Заказ стал выполненным - добавляем статистику
                client.VisitsCount++;
                client.TotalSpent += order.FinalPrice;
                client.LastVisitDate = DateTime.Now;

                System.Diagnostics.Debug.WriteLine($"Статистика клиента {client.FullName} увеличена: +{order.FinalPrice:N0} ₽, визитов: {client.VisitsCount}");
            }
            else if (!willBeCompleted && wasCompleted)
            {
                // Заказ был выполнен, но стал отменен/в ожидании - вычитаем статистику
                client.VisitsCount--;
                client.TotalSpent -= order.FinalPrice;

                // Обновляем дату последнего визита на предыдущий выполненный заказ
                var lastCompletedOrder = shift.Orders
                    .Where(o => o.ClientId == client.Id && o.Id != orderId && o.Status == "Выполнен")
                    .OrderByDescending(o => o.Time)
                    .FirstOrDefault();

                client.LastVisitDate = lastCompletedOrder?.Time ?? client.RegistrationDate;

                System.Diagnostics.Debug.WriteLine($"Статистика клиента {client.FullName} уменьшена: -{order.FinalPrice:N0} ₽, визитов: {client.VisitsCount}");
            }

            FileDataService.SaveData(appData);
        }
    }
}
