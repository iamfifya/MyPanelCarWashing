using System.Collections.Generic;
using System.Linq;

namespace MyPanelCarWashing.Models
{
    public class AppData
    {
        public List<User> Users { get; set; } = new List<User>();
        public List<Service> Services { get; set; } = new List<Service>();
        public List<Shift> Shifts { get; set; } = new List<Shift>();
        private int _nextUserId = 1;
        private int _nextServiceId = 1;
        private int _nextShiftId = 1;
        private int _nextOrderId = 1;

        public int GetNextUserId() => _nextUserId++;
        public int GetNextServiceId() => _nextServiceId++;
        public int GetNextShiftId() => _nextShiftId++;
        public int GetNextOrderId() => _nextOrderId++;

        public void UpdateIds()
        {
            if (Users.Count > 0) _nextUserId = Users.Max(u => u.Id) + 1;
            if (Services.Count > 0) _nextServiceId = Services.Max(s => s.Id) + 1;
            if (Shifts.Count > 0) _nextShiftId = Shifts.Max(s => s.Id) + 1;
            if (Shifts.SelectMany(s => s.Orders).Any())
                _nextOrderId = Shifts.SelectMany(s => s.Orders).Max(o => o.Id) + 1;
        }
    }
}