using System;
using System.Collections.Generic;

namespace MyPanelCarWashing.Models
{
    public class CarWashOrder
    {
        public int Id { get; set; }
        public string CarNumber { get; set; }
        public string CarModel { get; set; }
        public DateTime Time { get; set; }
        public decimal TotalPrice { get; set; }
        public int ShiftId { get; set; }
        public string Notes { get; set; }
        public List<int> ServiceIds { get; set; } = new List<int>();
        public int? EmployeeId { get; set; } // Кто выполнил заказ

        public string ServicesCount => ServiceIds.Count > 0 ? $"{ServiceIds.Count} услуг" : "нет услуг";
    }
}