using System;
using System.Collections.Generic;

namespace MyPanelCarWashing.Models
{
    public class CarWashOrder
    {
        public int Id { get; set; }
        public string CarNumber { get; set; }
        public string CarModel { get; set; }
        public string CarBodyType { get; set; } // Тип кузова
        public DateTime Time { get; set; }
        public decimal TotalPrice { get; set; }
        public int ShiftId { get; set; }
        public string Notes { get; set; }
        public List<int> ServiceIds { get; set; } = new List<int>();

        // Новые поля
        public int BoxNumber { get; set; } // Номер бокса (1, 2 или 3)
        public int WasherId { get; set; } // ID мойщика (сотрудника)

        public string ServicesCount => ServiceIds.Count > 0 ? $"{ServiceIds.Count} услуг" : "нет услуг";
        public string BoxName => $"Бокс {BoxNumber}";

        // Заработок - только эти свойства, без дубликатов!
        public decimal WasherEarnings => TotalPrice * 0.35m; // 35% мойщику
        public decimal CompanyEarnings => TotalPrice * 0.65m; // 65% компании
    }
}