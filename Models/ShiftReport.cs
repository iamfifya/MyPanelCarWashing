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
        public decimal TotalWasherEarnings { get; set; }
        public decimal TotalCompanyEarnings { get; set; }
        public string Notes { get; set; }

        // Существующие способы оплаты
        public int CashCount { get; set; }
        public decimal CashAmount { get; set; }
        public int CardCount { get; set; }
        public decimal CardAmount { get; set; }
        public int TransferCount { get; set; }
        public decimal TransferAmount { get; set; }
        public int QrCount { get; set; }
        public decimal QrAmount { get; set; }
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
