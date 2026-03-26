using System;
using System.Collections.Generic;

namespace MyPanelCarWashing.Models
{
    public class CarWashOrder
    {
        public int Id { get; set; }
        public string CarNumber { get; set; }
        public string CarModel { get; set; }
        public string CarBodyType { get; set; }
        public DateTime Time { get; set; }
        public decimal TotalPrice { get; set; }
        public int ShiftId { get; set; }
        public string Notes { get; set; }
        public List<int> ServiceIds { get; set; } = new List<int>();
        public int BoxNumber { get; set; }
        public int WasherId { get; set; }

        // Новые поля
        public decimal ExtraCost { get; set; } // Дополнительная стоимость
        public string ExtraCostReason { get; set; } // Причина дополнительной стоимости

        public decimal BasePrice => TotalPrice; // Стоимость услуг
        public decimal FinalPrice => TotalPrice + ExtraCost; // Итоговая сумма с доп. стоимостью

        public decimal WasherEarnings => FinalPrice * 0.35m; // 35% от итоговой суммы
        public decimal CompanyEarnings => FinalPrice * 0.65m; // 65% от итоговой суммы

        public string ServicesCount => ServiceIds.Count > 0 ? $"{ServiceIds.Count} услуг" : "нет услуг";
        public string BoxName => $"Бокс {BoxNumber}";
    }
}