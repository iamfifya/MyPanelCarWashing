using Newtonsoft.Json;
using System.ComponentModel;

namespace MyPanelCarWashing.Models
{
    public class User : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public int Id { get; set; }
        public string FullName { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsActive { get; set; } = true;

        // === Номер телефона ===
        private string _phone;
        public string Phone
        {
            get => _phone;
            set
            {
                if (_phone != value)
                {
                    _phone = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Phone)));
                }
            }
        }

        // Для отображения в списках (опционально)
        [JsonIgnore]
        public string DisplayString => $"{FullName} {(IsAdmin ? "👑" : "👤")} {(string.IsNullOrEmpty(Phone) ? "" : $"| {Phone}")}";
    }
}
