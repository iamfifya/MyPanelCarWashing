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
        public string Notes { get; set; }
    }

    public class EmployeeWorkReport
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public int CarsWashed { get; set; }
        public decimal TotalAmount { get; set; }
    }
}