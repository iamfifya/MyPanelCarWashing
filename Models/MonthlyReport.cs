using System;
using System.Collections.Generic;

namespace MyPanelCarWashing.Models
{
    public class MonthlyReport
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int TotalCars { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalWasherEarnings { get; set; }
        public decimal TotalCompanyEarnings { get; set; }

        // Способы оплаты
        public int TotalCashCount { get; set; }
        public decimal TotalCashAmount { get; set; }
        public int TotalCardCount { get; set; }
        public decimal TotalCardAmount { get; set; }
        public int TotalTransferCount { get; set; }
        public decimal TotalTransferAmount { get; set; }
        public int TotalQrCount { get; set; }
        public decimal TotalQrAmount { get; set; }

        public List<DailyReportSummary> DailyReports { get; set; } = new List<DailyReportSummary>();
        public List<EmployeeMonthlyReport> EmployeesReport { get; set; } = new List<EmployeeMonthlyReport>();

        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
    }

    public class DailyReportSummary
    {
        public DateTime Date { get; set; }
        public int Cars { get; set; }
        public decimal Revenue { get; set; }
        public decimal WasherEarnings { get; set; }
        public decimal CompanyEarnings { get; set; }

        // Способы оплаты
        public int CashCount { get; set; }
        public decimal CashAmount { get; set; }
        public int CardCount { get; set; }
        public decimal CardAmount { get; set; }
        public int TransferCount { get; set; }
        public decimal TransferAmount { get; set; }
        public int QrCount { get; set; }
        public decimal QrAmount { get; set; }
    }

    public class EmployeeMonthlyReport
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public int CarsWashed { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Earnings { get; set; }
        public List<DailyEmployeeReport> DailyWork { get; set; } = new List<DailyEmployeeReport>();
    }

    public class DailyEmployeeReport
    {
        public DateTime Date { get; set; }
        public int CarsWashed { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Earnings { get; set; }
    }
}
