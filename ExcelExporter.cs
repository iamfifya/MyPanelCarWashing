using ClosedXML.Excel;
using MyPanelCarWashing.Models;
using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace MyPanelCarWashing
{
    public static class ExcelExporter
    {
        public static void ExportShiftReport(ShiftReport report, string filePath)
        {
            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Отчет о смене");

                    // Заголовок
                    worksheet.Cell(1, 1).Value = $"Отчет о смене от {report.Date:dd.MM.yyyy}";
                    worksheet.Cell(1, 1).Style.Font.Bold = true;
                    worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                    worksheet.Range(1, 1, 1, 6).Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Основная информация
                    int row = 3;
                    worksheet.Cell(row, 1).Value = "Дата:";
                    worksheet.Cell(row, 2).Value = report.Date.ToString("dd.MM.yyyy");
                    row++;

                    worksheet.Cell(row, 1).Value = "Время начала:";
                    worksheet.Cell(row, 2).Value = report.StartTime.ToString("HH:mm:ss");
                    row++;

                    worksheet.Cell(row, 1).Value = "Время окончания:";
                    worksheet.Cell(row, 2).Value = report.EndTime.ToString("HH:mm:ss");
                    row++;

                    worksheet.Cell(row, 1).Value = "Всего машин:";
                    worksheet.Cell(row, 2).Value = report.TotalCars;
                    row++;

                    worksheet.Cell(row, 1).Value = "Общая выручка:";
                    worksheet.Cell(row, 2).Value = report.TotalRevenue;
                    worksheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00 ₽";
                    row++;

                    worksheet.Cell(row, 1).Value = "Выплаты мойщикам (35%):";
                    worksheet.Cell(row, 2).Value = report.TotalWasherEarnings;
                    worksheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00 ₽";
                    row++;

                    worksheet.Cell(row, 1).Value = "Доход компании (65%):";
                    worksheet.Cell(row, 2).Value = report.TotalCompanyEarnings;
                    worksheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00 ₽";
                    row += 2;

                    // Статистика по способам оплаты
                    worksheet.Cell(row, 1).Value = "СТАТИСТИКА ПО СПОСОБАМ ОПЛАТЫ";
                    worksheet.Range(row, 1, row, 4).Merge().Style.Font.Bold = true;
                    worksheet.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.LightGray;
                    row++;

                    worksheet.Cell(row, 1).Value = "Способ оплаты";
                    worksheet.Cell(row, 2).Value = "Количество";
                    worksheet.Cell(row, 3).Value = "Сумма";
                    worksheet.Range(row, 1, row, 3).Style.Font.Bold = true;
                    row++;

                    worksheet.Cell(row, 1).Value = "💵 Наличные";
                    worksheet.Cell(row, 2).Value = report.CashCount;
                    worksheet.Cell(row, 3).Value = report.CashAmount;
                    worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00 ₽";
                    row++;

                    worksheet.Cell(row, 1).Value = "💳 Карта";
                    worksheet.Cell(row, 2).Value = report.CardCount;
                    worksheet.Cell(row, 3).Value = report.CardAmount;
                    worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00 ₽";
                    row++;

                    worksheet.Cell(row, 1).Value = "📱 Перевод";
                    worksheet.Cell(row, 2).Value = report.TransferCount;
                    worksheet.Cell(row, 3).Value = report.TransferAmount;
                    worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00 ₽";
                    row++;

                    worksheet.Cell(row, 1).Value = "📱 QR-код";
                    worksheet.Cell(row, 2).Value = report.QrCount;
                    worksheet.Cell(row, 3).Value = report.QrAmount;
                    worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00 ₽";
                    row++;

                    worksheet.Cell(row, 1).Value = "ИТОГО:";
                    worksheet.Cell(row, 2).Value = report.CashCount + report.CardCount + report.TransferCount;
                    worksheet.Cell(row, 3).Value = report.CashAmount + report.CardAmount + report.TransferAmount;
                    worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00 ₽";
                    worksheet.Range(row, 1, row, 3).Style.Font.Bold = true;
                    row += 2;

                    // Заголовок таблицы сотрудников
                    worksheet.Cell(row, 1).Value = "Сотрудник";
                    worksheet.Cell(row, 2).Value = "Машин";
                    worksheet.Cell(row, 3).Value = "Выручка";
                    worksheet.Cell(row, 4).Value = "Заработок (35%)";
                    worksheet.Range(row, 1, row, 4).Style.Font.Bold = true;
                    worksheet.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.LightGray;
                    row++;

                    // Данные сотрудников
                    foreach (var emp in report.EmployeesWork.OrderByDescending(e => e.CarsWashed))
                    {
                        worksheet.Cell(row, 1).Value = emp.EmployeeName;
                        worksheet.Cell(row, 2).Value = emp.CarsWashed;
                        worksheet.Cell(row, 3).Value = emp.TotalAmount;
                        worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00 ₽";
                        worksheet.Cell(row, 4).Value = emp.Earnings;
                        worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00 ₽";
                        row++;
                    }

                    // Итоговая строка
                    worksheet.Cell(row, 1).Value = "ИТОГО:";
                    worksheet.Cell(row, 2).Value = report.EmployeesWork.Sum(e => e.CarsWashed);
                    worksheet.Cell(row, 3).Value = report.EmployeesWork.Sum(e => e.TotalAmount);
                    worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00 ₽";
                    worksheet.Cell(row, 4).Value = report.EmployeesWork.Sum(e => e.Earnings);
                    worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00 ₽";
                    worksheet.Range(row, 1, row, 4).Style.Font.Bold = true;
                    row++;

                    // Примечание
                    if (!string.IsNullOrEmpty(report.Notes))
                    {
                        row++;
                        worksheet.Cell(row, 1).Value = "Примечание:";
                        worksheet.Cell(row, 2).Value = report.Notes;
                    }

                    // Автоширина колонок
                    worksheet.Columns().AdjustToContents();

                    workbook.SaveAs(filePath);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка экспорта в Excel: {ex.Message}");
            }
        }

        public static void ExportMonthlyReport(MonthlyReport report, string filePath)
        {
            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var ws = workbook.Worksheets.Add("Месячный отчет");

                    // Шапка
                    ws.Cell(1, 1).Value = $"Отчет за {report.MonthName} {report.Year}";
                    ws.Cell(1, 1).Style.Font.Bold = true;
                    ws.Cell(1, 1).Style.Font.FontSize = 16;
                    ws.Range(1, 1, 1, 4).Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Общие показатели
                    ws.Cell(3, 1).Value = "Общие показатели";
                    ws.Cell(3, 1).Style.Font.Bold = true;

                    ws.Cell(4, 1).Value = "Всего машин:"; ws.Cell(4, 2).Value = report.TotalCars;
                    ws.Cell(5, 1).Value = "Общая выручка:"; ws.Cell(5, 2).Value = report.TotalRevenue; ws.Cell(5, 2).Style.NumberFormat.Format = "#,##0.00 ₽";
                    ws.Cell(6, 1).Value = "ЗП Мойщикам:"; ws.Cell(6, 2).Value = report.TotalWasherEarnings; ws.Cell(6, 2).Style.NumberFormat.Format = "#,##0.00 ₽";
                    ws.Cell(7, 1).Value = "Доход компании:"; ws.Cell(7, 2).Value = report.TotalCompanyEarnings; ws.Cell(7, 2).Style.NumberFormat.Format = "#,##0.00 ₽";

                    // Сводка по сотрудникам
                    int row = 9;
                    ws.Cell(row, 1).Value = "СВОДКА ПО СОТРУДНИКАМ";
                    ws.Cell(row, 1).Style.Font.Bold = true;
                    row++;

                    ws.Cell(row, 1).Value = "Сотрудник";
                    ws.Cell(row, 2).Value = "Машин";
                    ws.Cell(row, 3).Value = "Выручка";
                    ws.Cell(row, 4).Value = "Заработок";
                    ws.Range(row, 1, row, 4).Style.Font.Bold = true;
                    ws.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.LightGray;
                    row++;

                    foreach (var emp in report.EmployeesReport.OrderByDescending(e => e.Earnings))
                    {
                        ws.Cell(row, 1).Value = emp.EmployeeName;
                        ws.Cell(row, 2).Value = emp.CarsWashed;
                        ws.Cell(row, 3).Value = emp.TotalAmount; ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00 ₽";
                        ws.Cell(row, 4).Value = emp.Earnings; ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00 ₽";
                        row++;
                    }

                    // Автоширина
                    ws.Columns().AdjustToContents();

                    workbook.SaveAs(filePath);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при создании Excel файла: {ex.Message}", ex);
            }
        }

        public static void ExportCustomPeriodReport(CustomPeriodReport report, string filePath)
        {
            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var ws = workbook.Worksheets.Add("Выборочный отчет");

                    // Шапка
                    ws.Cell(1, 1).Value = $"Отчет за период с {report.StartDate:dd.MM.yyyy} по {report.EndDate:dd.MM.yyyy}";
                    ws.Cell(1, 1).Style.Font.Bold = true;
                    ws.Cell(1, 1).Style.Font.FontSize = 16;
                    ws.Range(1, 1, 1, 5).Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Общие показатели
                    ws.Cell(3, 1).Value = "ОБЩИЕ ПОКАЗАТЕЛИ";
                    ws.Cell(3, 1).Style.Font.Bold = true;
                    ws.Cell(3, 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                    ws.Range(3, 1, 3, 2).Merge();

                    ws.Cell(4, 1).Value = "Всего машин:"; ws.Cell(4, 2).Value = report.TotalCars;
                    ws.Cell(5, 1).Value = "Общая выручка:"; ws.Cell(5, 2).Value = report.TotalRevenue; ws.Cell(5, 2).Style.NumberFormat.Format = "#,##0.00 ₽";
                    ws.Cell(6, 1).Value = "ЗП Мойщикам:"; ws.Cell(6, 2).Value = report.TotalWasherEarnings; ws.Cell(6, 2).Style.NumberFormat.Format = "#,##0.00 ₽";
                    ws.Cell(7, 1).Value = "Доход компании:"; ws.Cell(7, 2).Value = report.TotalCompanyEarnings; ws.Cell(7, 2).Style.NumberFormat.Format = "#,##0.00 ₽";

                    // Способы оплаты 
                    ws.Cell(9, 1).Value = "СПОСОБЫ ОПЛАТЫ";
                    ws.Cell(9, 1).Style.Font.Bold = true;
                    ws.Cell(9, 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                    ws.Range(9, 1, 9, 3).Merge();

                    ws.Cell(10, 1).Value = "Наличные:"; ws.Cell(10, 2).Value = $"{report.TotalCashCount} шт."; ws.Cell(10, 3).Value = report.TotalCashAmount; ws.Cell(10, 3).Style.NumberFormat.Format = "#,##0.00 ₽";
                    ws.Cell(11, 1).Value = "Карта:"; ws.Cell(11, 2).Value = $"{report.TotalCardCount} шт."; ws.Cell(11, 3).Value = report.TotalCardAmount; ws.Cell(11, 3).Style.NumberFormat.Format = "#,##0.00 ₽";
                    ws.Cell(12, 1).Value = "Перевод:"; ws.Cell(12, 2).Value = $"{report.TotalTransferCount} шт."; ws.Cell(12, 3).Value = report.TotalTransferAmount; ws.Cell(12, 3).Style.NumberFormat.Format = "#,##0.00 ₽";
                    ws.Cell(13, 1).Value = "QR-код:"; ws.Cell(13, 2).Value = $"{report.TotalQrCount} шт."; ws.Cell(13, 3).Value = report.TotalQrAmount; ws.Cell(13, 3).Style.NumberFormat.Format = "#,##0.00 ₽";

                    // Детализация по дням
                    int row = 15;
                    ws.Cell(row, 1).Value = "ДЕТАЛИЗАЦИЯ ПО ДНЯМ";
                    ws.Cell(row, 1).Style.Font.Bold = true;
                    ws.Range(row, 1, row, 5).Merge();
                    row++;

                    ws.Cell(row, 1).Value = "Дата";
                    ws.Cell(row, 2).Value = "Машин";
                    ws.Cell(row, 3).Value = "Выручка";
                    ws.Cell(row, 4).Value = "ЗП Мойщикам";
                    ws.Cell(row, 5).Value = "Доход компании";
                    ws.Range(row, 1, row, 5).Style.Font.Bold = true;
                    ws.Range(row, 1, row, 5).Style.Fill.BackgroundColor = XLColor.LightBlue;
                    row++;

                    // Сортируем дни по порядку
                    foreach (var day in report.DailyReports.OrderBy(d => d.Date))
                    {
                        ws.Cell(row, 1).Value = day.Date.ToString("dd.MM.yyyy");

                        // Используем правильные свойства из класса DailyReportSummary
                        ws.Cell(row, 2).Value = day.Cars;

                        ws.Cell(row, 3).Value = day.Revenue;
                        ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00 ₽";

                        ws.Cell(row, 4).Value = day.WasherEarnings;
                        ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00 ₽";

                        ws.Cell(row, 5).Value = day.CompanyEarnings;
                        ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00 ₽";

                        row++;
                    }

                    // Наводим красоту: автоширина колонок
                    ws.Columns().AdjustToContents();

                    workbook.SaveAs(filePath);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при создании Excel файла: {ex.Message}", ex);
            }
        }
    }
}
