using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;

namespace MyPanelCarWashing.Models
{
    public class Service : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public int Id { get; set; }
        public string Name { get; set; }
        public int DurationMinutes { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; } = true;

        // Новая структура цен по категориям кузова
        public Dictionary<int, decimal> PriceByBodyType { get; set; } = new Dictionary<int, decimal>();

        // Метод для получения цены по категории кузова
        public decimal GetPrice(int bodyTypeCategory)
        {
            if (PriceByBodyType.TryGetValue(bodyTypeCategory, out var price))
                return price;

            // Если нет цены для этой категории, берем цену для категории 1
            if (PriceByBodyType.TryGetValue(1, out var defaultPrice))
                return defaultPrice;

            // Если нет вообще, возвращаем 0
            return 0;
        }
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }
    }
}
