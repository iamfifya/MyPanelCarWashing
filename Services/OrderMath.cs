using MyPanelCarWashing.Models;
using System.Collections.Generic;
using System.Linq;

namespace MyPanelCarWashing.Services
{
    /// <summary>
    /// ЕДИНСТВЕННЫЙ источник истины для расчётов заказов.
    /// Все формулы, скидки и ЗП считаются здесь.
    /// </summary>
    public static class OrderMath
    {
        // === НАСТРОЙКИ (меняй только здесь) ===
        public const decimal WASHER_PERCENT = 0.35m;          // 35% мойщику
        public const decimal MIN_WASHER_PAY_PER_SHIFT = 1000m; // Мин. ЗП за смену

        /// <summary>
        /// Рассчитывает ВСЕ значения для одного заказа.
        /// </summary>
        public static OrderCalculation Calculate(CarWashOrder order, List<Service> allServices)
        {
            // 1. Сумма услуг по актуальным ценам
            decimal servicesTotal = (order.ServiceIds ?? new List<int>())
                .Sum(sid => allServices?.FirstOrDefault(s => s.Id == sid)?.GetPrice(order.BodyTypeCategory) ?? 0);

            // 2. База: услуги + доп. расходы
            decimal baseAmount = servicesTotal + order.ExtraCost;

            // 3. Скидка (применяется ко ВСЕЙ базе)
            decimal actualDiscount = order.DiscountPercent > 0
                ? baseAmount * (order.DiscountPercent / 100m)
                : order.DiscountAmount;

            // 4. Результаты
            return new OrderCalculation
            {
                ServicesTotal = servicesTotal,
                BaseAmount = baseAmount,
                ActualDiscount = actualDiscount,
                FinalPrice = baseAmount - actualDiscount,
                WasherEarnings = baseAmount * WASHER_PERCENT, // Скидка НЕ влияет
                CompanyEarnings = (baseAmount - actualDiscount) - (baseAmount * WASHER_PERCENT)
            };
        }

        /// <summary>
        /// Рассчитывает ЗП мойщика за смену с гарантией минимума.
        /// </summary>
        public static decimal CalculateWasherShiftPay(IEnumerable<CarWashOrder> completedOrders, List<Service> allServices)
        {
            decimal basePay = completedOrders.Sum(o => Calculate(o, allServices).WasherEarnings);
            return System.Math.Max(basePay, MIN_WASHER_PAY_PER_SHIFT);
        }
    }

    /// <summary>
    /// Готовый результат расчёта. Не меняй этот класс.
    /// </summary>
    public class OrderCalculation
    {
        public decimal ServicesTotal { get; set; }
        public decimal BaseAmount { get; set; }
        public decimal ActualDiscount { get; set; }
        public decimal FinalPrice { get; set; }
        public decimal WasherEarnings { get; set; }
        public decimal CompanyEarnings { get; set; }
    }
}
