using System;

namespace MyPanelCarWashing.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public int? ShiftId { get; set; }

        public int? EmployeeId { get; set; } // Добавили поле для привязки к сотруднику

        public decimal Amount { get; set; }
        public string Type { get; set; }
        public string Comment { get; set; }
        public DateTime DateTime { get; set; }

        // --- Помощники для визуального интерфейса (C# 7.3 compatible) ---
        public string FormattedAmount
        {
            get { return (Type == "Приход" || Type == "Размен") ? $"+{Amount:N0} ₽" : $"-{Amount:N0} ₽"; }
        }

        public string ColorHex
        {
            get { return (Type == "Приход" || Type == "Размен") ? "#27AE60" : "#E74C3C"; }
        }

        public string Icon
        {
            get
            {
                switch (Type)
                {
                    case "Приход": return "💵";
                    case "Расход": return "🛒";
                    case "Аванс мойщику": return "👤"; // Исправлено на "Аванс мойщику"
                    case "Инкассация": return "🏦";
                    case "Размен": return "🪙";
                    default: return "📄";
                }
            }
        }
    }
}
