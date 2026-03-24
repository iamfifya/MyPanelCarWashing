using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
using MyPanelCarWashing.ViewModels;

namespace MyPanelCarWashing
{
    public partial class EditOrderServicesWindow : Window
    {
        private int _orderId;
        private DataService _dataService;
        private List<ServiceViewModel> _services;

        public EditOrderServicesWindow(int orderId, DataService dataService)
        {
            InitializeComponent();
            _orderId = orderId;
            _dataService = dataService;
            LoadData();
        }

        private void LoadData()
        {
            var allServices = _dataService.GetAllServices();
            var orderServiceIds = _dataService.GetOrderServiceIds(_orderId);

            _services = allServices.Select(s => new ServiceViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Price = s.Price,
                IsSelected = orderServiceIds.Contains(s.Id)
            }).ToList();

            ServicesListBox.ItemsSource = _services;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedServiceIds = _services.Where(s => s.IsSelected).Select(s => s.Id).ToList();
            _dataService.UpdateOrderServices(_orderId, selectedServiceIds);

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}