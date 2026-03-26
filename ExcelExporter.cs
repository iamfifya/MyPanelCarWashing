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
                    worksheet.Range(1, 1, 1, 4).Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

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

                    // Заголовок таблицы сотрудников
                    worksheet.Cell(row, 1).Value = "Сотрудник";
                    worksheet.Cell(row, 2).Value = "Машин";
                    worksheet.Cell(row, 3).Value = "Выручка";
                    worksheet.Cell(row, 4).Value = "Заработок (35%)";
                    worksheet.Range(row, 1, row, 4).Style.Font.Bold = true;
                    worksheet.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.LightGray;
                    row++;

                    // Данные сотрудников
                    foreach (var emp in report.EmployeesWork)
                    {
                        worksheet.Cell(row, 1).Value = emp.EmployeeName;
                        worksheet.Cell(row, 2).Value = emp.CarsWashed;
                        worksheet.Cell(row, 3).Value = emp.TotalAmount;
                        worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00 ₽";
                        worksheet.Cell(row, 4).Value = emp.Earnings;
                        worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00 ₽";
                        row++;
                    }

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
                    var worksheet = workbook.Worksheets.Add($"Отчет за {report.MonthName}");

                    // Заголовок
                    worksheet.Cell(1, 1).Value = $"Месячный отчет за {report.MonthName}";
                    worksheet.Cell(1, 1).Style.Font.Bold = true;
                    worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                    worksheet.Range(1, 1, 1, 5).Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Итоги
                    int row = 3;
                    worksheet.Cell(row, 1).Value = "Всего машин за месяц:";
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

                    // Заголовок таблицы по дням
                    worksheet.Cell(row, 1).Value = "Дата";
                    worksheet.Cell(row, 2).Value = "Машин";
                    worksheet.Cell(row, 3).Value = "Выручка";
                    worksheet.Cell(row, 4).Value = "Мойщикам";
                    worksheet.Cell(row, 5).Value = "Компании";
                    worksheet.Range(row, 1, row, 5).Style.Font.Bold = true;
                    worksheet.Range(row, 1, row, 5).Style.Fill.BackgroundColor = XLColor.LightGray;
                    row++;

                    // Данные по дням
                    foreach (var day in report.DailyReports)
                    {
                        worksheet.Cell(row, 1).Value = day.Date.ToString("dd.MM.yyyy");
                        worksheet.Cell(row, 2).Value = day.Cars;
                        worksheet.Cell(row, 3).Value = day.Revenue;
                        worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00 ₽";
                        worksheet.Cell(row, 4).Value = day.WasherEarnings;
                        worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00 ₽";
                        worksheet.Cell(row, 5).Value = day.CompanyEarnings;
                        worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00 ₽";
                        row++;
                    }

                    row += 2;

                    // Таблица по сотрудникам
                    worksheet.Cell(row, 1).Value = "ДЕТАЛЬНАЯ СТАТИСТИКА СОТРУДНИКОВ";
                    worksheet.Range(row, 1, row, 4).Merge().Style.Font.Bold = true;
                    worksheet.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.LightGray;
                    row++;

                    worksheet.Cell(row, 1).Value = "Сотрудник";
                    worksheet.Cell(row, 2).Value = "Всего машин";
                    worksheet.Cell(row, 3).Value = "Общая выручка";
                    worksheet.Cell(row, 4).Value = "Заработок (35%)";
                    worksheet.Range(row, 1, row, 4).Style.Font.Bold = true;
                    row++;

                    foreach (var emp in report.EmployeesReport)
                    {
                        worksheet.Cell(row, 1).Value = emp.EmployeeName;
                        worksheet.Cell(row, 2).Value = emp.CarsWashed;
                        worksheet.Cell(row, 3).Value = emp.TotalAmount;
                        worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00 ₽";
                        worksheet.Cell(row, 4).Value = emp.Earnings;
                        worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00 ₽";
                        row++;
                    }

                    row++;
                    worksheet.Cell(row, 1).Value = "ИТОГО:";
                    worksheet.Cell(row, 2).Value = report.TotalCars;
                    worksheet.Cell(row, 3).Value = report.TotalRevenue;
                    worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00 ₽";
                    worksheet.Cell(row, 4).Value = report.TotalWasherEarnings;
                    worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00 ₽";
                    worksheet.Range(row, 1, row, 4).Style.Font.Bold = true;

                    // Детальная таблица по дням для каждого сотрудника
                    row += 2;
                    worksheet.Cell(row, 1).Value = "ПОДРОБНО ПО ДНЯМ";
                    worksheet.Range(row, 1, row, 4).Merge().Style.Font.Bold = true;
                    worksheet.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.LightGray;
                    row++;

                    foreach (var emp in report.EmployeesReport)
                    {
                        row++;
                        worksheet.Cell(row, 1).Value = emp.EmployeeName;
                        worksheet.Range(row, 1, row, 1).Style.Font.Bold = true;
                        row++;

                        worksheet.Cell(row, 1).Value = "Дата";
                        worksheet.Cell(row, 2).Value = "Машин";
                        worksheet.Cell(row, 3).Value = "Выручка";
                        worksheet.Cell(row, 4).Value = "Заработок";
                        worksheet.Range(row, 1, row, 4).Style.Font.Bold = true;
                        worksheet.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.LightGray;
                        row++;

                        foreach (var day in emp.DailyWork.OrderBy(d => d.Date))
                        {
                            worksheet.Cell(row, 1).Value = day.Date.ToString("dd.MM.yyyy");
                            worksheet.Cell(row, 2).Value = day.CarsWashed;
                            worksheet.Cell(row, 3).Value = day.TotalAmount;
                            worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00 ₽";
                            worksheet.Cell(row, 4).Value = day.Earnings;
                            worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00 ₽";
                            row++;
                        }

                        row++;
                        worksheet.Cell(row, 1).Value = "Итого:";
                        worksheet.Cell(row, 2).Value = emp.CarsWashed;
                        worksheet.Cell(row, 3).Value = emp.TotalAmount;
                        worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00 ₽";
                        worksheet.Cell(row, 4).Value = emp.Earnings;
                        worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00 ₽";
                        worksheet.Range(row, 1, row, 4).Style.Font.Bold = true;
                        row += 2;
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
    }
}