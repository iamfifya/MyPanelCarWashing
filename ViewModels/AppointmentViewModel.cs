using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
using MyPanelCarWashing.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace MyPanelCarWashing.ViewModels
{
    public class AppointmentViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly DataService _dataService;
        private List<ServiceViewModel> _services;
        private int _selectedBodyTypeCategory = 1;
        private decimal _extraCost;
        private decimal _servicesTotal;

        public List<ServiceViewModel> Services
        {
            get => _services;
            set
            {
                _services = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Services)));
            }
        }

        public int SelectedBodyTypeCategory
        {
            get => _selectedBodyTypeCategory;
            set
            {
                _selectedBodyTypeCategory = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedBodyTypeCategory)));
                UpdateServicePrices();
                CalculateTotal();
            }
        }

        public decimal ExtraCost
        {
            get => _extraCost;
            set
            {
                _extraCost = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExtraCost)));
                CalculateTotal();
            }
        }

        public decimal ServicesTotal
        {
            get => _servicesTotal;
            set
            {
                _servicesTotal = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ServicesTotal)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FinalTotal)));
            }
        }

        public decimal FinalTotal => ServicesTotal + ExtraCost;

        public AppointmentViewModel(DataService dataService)
        {
            _dataService = dataService;
            LoadServices();
        }

        private void LoadServices()
        {
            var allServices = _dataService.GetAllServices();
            Services = allServices.Select(s => new ServiceViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Price = s.GetPrice(SelectedBodyTypeCategory),
                IsSelected = false
            }).ToList();
        }

        private void UpdateServicePrices()
        {
            if (Services != null)
            {
                var allServices = _dataService.GetAllServices();
                foreach (var serviceVM in Services)
                {
                    var originalService = allServices.FirstOrDefault(s => s.Id == serviceVM.Id);
                    if (originalService != null)
                    {
                        serviceVM.Price = originalService.GetPrice(SelectedBodyTypeCategory);
                    }
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Services)));
            }
        }

        public void CalculateTotal()
        {
            ServicesTotal = Services?.Where(s => s.IsSelected).Sum(s => s.Price) ?? 0;
        }

        public List<int> GetSelectedServiceIds()
        {
            return Services?.Where(s => s.IsSelected).Select(s => s.Id).ToList() ?? new List<int>();
        }
    }
}
