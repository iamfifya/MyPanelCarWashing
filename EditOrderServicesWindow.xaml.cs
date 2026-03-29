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

        private DataService _dataService;
        private List<ServiceViewModel> _services;

        public CarWashOrder CurrentOrder { get; set; }

        public List<ServiceViewModel> Services
        {
            get => _services;
            set
            {
                _services = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Services)));
            }
        }

        public EditOrderServicesWindow(DataService dataService, CarWashOrder order)
        {
            InitializeComponent();
            _dataService = dataService;
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
                Price = s.GetPrice(CurrentOrder.BodyTypeCategory), // Используем GetPrice с категорией заказа
                IsSelected = orderServiceIds.Contains(s.Id)
            }).ToList();

            ServicesListView.ItemsSource = Services;

            for (int i = 0; i < Services.Count; i++)
            {
                if (Services[i].IsSelected)
                {
                    ServicesListView.SelectedItems.Add(Services[i]);
                }
            }
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
                MessageBox.Show($"Услуги обновлены\nНовая сумма: {CurrentOrder.FinalPrice:N0} ₽", "Успешно",
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
