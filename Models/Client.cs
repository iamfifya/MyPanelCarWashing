// Models/Client.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace MyPanelCarWashing.Models
{
    public class Client : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private int _id;
        private string _fullName;
        private string _phone;
        private string _carModel;
        private string _carNumber;
        private DateTime _registrationDate;
        private DateTime? _lastVisitDate;
        private decimal _totalSpent;
        private int _visitsCount;
        private string _notes;

        public int Id
        {
            get => _id;
            set { _id = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Id))); }
        }

        public string FullName
        {
            get => _fullName;
            set { _fullName = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FullName))); }
        }

        public string Phone
        {
            get => _phone;
            set { _phone = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Phone))); }
        }

        public string CarModel
        {
            get => _carModel;
            set { _carModel = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CarModel))); }
        }

        public string CarNumber
        {
            get => _carNumber;
            set { _carNumber = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CarNumber))); }
        }

        public DateTime RegistrationDate
        {
            get => _registrationDate;
            set { _registrationDate = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RegistrationDate))); }
        }

        public DateTime? LastVisitDate
        {
            get => _lastVisitDate;
            set { _lastVisitDate = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastVisitDate))); }
        }

        public decimal TotalSpent
        {
            get => _totalSpent;
            set { _totalSpent = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalSpent))); }
        }

        public int VisitsCount
        {
            get => _visitsCount;
            set { _visitsCount = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VisitsCount))); }
        }

        public string Notes
        {
            get => _notes;
            set { _notes = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Notes))); }
        }

        // Вычисляемое свойство для отображения
        public string DisplayInfo => $"{FullName} | {Phone} | {CarNumber}";

        public string AverageCheck => VisitsCount > 0 ? (TotalSpent / VisitsCount).ToString("N0") : "0";
    }
}
