using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
using MyPanelCarWashing.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace MyPanelCarWashing
{
    public partial class EditOrderServicesWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private CarWashOrder _order;
        private DataService _dataService;
        private List<ServiceViewModel> _services;

        // Публичное свойство для доступа из XAML
        public CarWashOrder CurrentOrder
        {
            get => _order;
            set
            {
                _order = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentOrder"));
            }
        }

        public List<ServiceViewModel> Services
        {
            get => _services;
            set
            {
                _services = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Services"));
            }
        }

        public EditOrderServicesWindow(CarWashOrder order)
        {
            InitializeComponent();
            _dataService = Core.DB;
            CurrentOrder = order;
            DataContext = this;
            LoadServices();
        }

        private void LoadServices()
        {
            var allServices = _dataService.GetAllServices();
            var orderServiceIds = CurrentOrder.ServiceIds ?? new List<int>();

            Services = allServices.Select(s => new ServiceViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Price = s.Price,
                IsSelected = orderServiceIds.Contains(s.Id)
            }).ToList();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedServices = Services.Where(s => s.IsSelected).ToList();

                if (!selectedServices.Any())
                {
                    MessageBox.Show("Выберите хотя бы одну услугу", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var serviceIds = selectedServices.Select(s => s.Id).ToList();
                _dataService.UpdateOrderServices(CurrentOrder.Id, serviceIds);

                DialogResult = true;
                MessageBox.Show($"Услуги обновлены\nНовая сумма: {CurrentOrder.TotalPrice:C}", "Успешно",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}