using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MyPanelCarWashing.Models
{
    // --- 1. Базовый класс для любого отчета (смена, месяц, период) ---
    public class BaseReport
    {
        public int TotalCars { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalWasherEarnings { get; set; }
        public decimal TotalCompanyEarnings { get; set; }

        // --- ИСПРАВЛЕННАЯ СТАТИСТИКА ПО ОПЛАТАМ ---
        [JsonProperty("TotalCashCount")]
        public int CashCount { get; set; }

        [JsonProperty("TotalCashAmount")]
        public decimal CashAmount { get; set; }

        [JsonProperty("TotalCardCount")]
        public int CardCount { get; set; }

        [JsonProperty("TotalCardAmount")]
        public decimal CardAmount { get; set; }

        [JsonProperty("TotalTransferCount")]
        public int TransferCount { get; set; }

        [JsonProperty("TotalTransferAmount")]
        public decimal TransferAmount { get; set; }

        [JsonProperty("TotalQrCount")]
        public int QrCount { get; set; }

        [JsonProperty("TotalQrAmount")]
        public decimal QrAmount { get; set; }

        // НОВЫЕ ПОЛЯ АНАЛИТИКИ КЛИЕНТОВ (их можно оставить без атрибутов)
        public int UniqueClientsCount { get; set; }
        public int NewClientsCount { get; set; }

        public List<ServiceAnalytics> TopServices { get; set; } = new List<ServiceAnalytics>();
        public List<EmployeeReport> EmployeesWork { get; set; } = new List<EmployeeReport>();
    }

    // --- 2. Модель для Месячного отчета ---
    public class MonthlyReport : BaseReport
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
        public List<DailyReportSummary> DailyReports { get; set; } = new List<DailyReportSummary>();
    }

    // --- 3. Модель для Выборочного (Интервального) отчета ---
    public class CustomPeriodReport : BaseReport
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<DailyReportSummary> DailyReports { get; set; } = new List<DailyReportSummary>();
    }

    // --- 4. Вспомогательные классы (унифицированные имена) ---

    public class DailyReportSummary : BaseReport
    {
        public DateTime Date { get; set; }
    }

    public class EmployeeReport
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

    // Класс для аналитики услуг (Топ любимых услуг)
    public class ServiceAnalytics
    {
        public string ServiceName { get; set; }
        public int Count { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
