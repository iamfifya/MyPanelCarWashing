using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MyPanelCarWashing.Models;

namespace MyPanelCarWashing.Services
{
    public class DataService
    {
        private AppData _data;

        public DataService()
        {
            LoadData();
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
            var shift = _data.Shifts.FirstOrDefault(s => s.Date.Date == date.Date && !s.IsClosed);

            if (shift == null)
            {
                shift = new Shift
                {
                    Id = _data.GetNextShiftId(),
                    Date = date,
                    StartTime = DateTime.Now,
                    IsClosed = false,
                    EmployeeIds = new List<int>(),
                    Orders = new List<CarWashOrder>()
                };
                _data.Shifts.Add(shift);
                SaveData();
            }

            return shift;
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
            order.Id = _data.GetNextOrderId();
            order.ServiceIds = serviceIds;
            order.TotalPrice = _data.Services
                .Where(s => serviceIds.Contains(s.Id))
                .Sum(s => s.Price);

            var shift = _data.Shifts.FirstOrDefault(s => s.Id == order.ShiftId);
            if (shift != null)
            {
                shift.Orders.Add(order);
                SaveData();
            }
        }

        public CarWashOrder GetOrderById(int orderId)
        {
            return _data.Shifts
                .SelectMany(s => s.Orders)
                .FirstOrDefault(o => o.Id == orderId);
        }

        public List<int> GetOrderServiceIds(int orderId)
        {
            var order = GetOrderById(orderId);
            return order?.ServiceIds ?? new List<int>();
        }

        public void UpdateOrderServices(int orderId, List<int> serviceIds)
        {
            var order = GetOrderById(orderId);
            if (order != null)
            {
                order.ServiceIds = serviceIds;
                order.TotalPrice = _data.Services
                    .Where(s => serviceIds.Contains(s.Id))
                    .Sum(s => s.Price);
                SaveData();
            }
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
    }

    public class MonthlyReport
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int TotalCars { get; set; }
        public decimal TotalRevenue { get; set; }
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
    }
}