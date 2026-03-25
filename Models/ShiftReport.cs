using System;
using System.Collections.Generic;

namespace MyPanelCarWashing.Models
{
    public class ShiftReport
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<EmployeeWorkReport> EmployeesWork { get; set; } = new List<EmployeeWorkReport>();
        public int TotalCars { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalWasherEarnings { get; set; } // Заработок всех мойщиков
        public decimal TotalCompanyEarnings { get; set; } // Заработок компании
        public string Notes { get; set; }
    }

    public class EmployeeWorkReport
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public int CarsWashed { get; set; }
        public decimal TotalAmount { get; set; } // Общая сумма заказов сотрудника
        public decimal Earnings { get; set; } // 35% от TotalAmount
    }
}