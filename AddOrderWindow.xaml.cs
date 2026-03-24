using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
using MyPanelCarWashing.ViewModels;

namespace MyPanelCarWashing
{
    public partial class AddOrderWindow : Window
    {
        private int _shiftId;
        private DataService _dataService;
        private List<ServiceViewModel> _services;

        public AddOrderWindow(int shiftId, DataService dataService)
        {
            InitializeComponent();
            _shiftId = shiftId;
            _dataService = dataService;
            DatePicker.SelectedDate = DateTime.Now;
            TimeBox.Text = DateTime.Now.ToString("HH:mm");
            LoadServices();
        }

        private void LoadServices()
        {
            var services = _dataService.GetAllServices();
            _services = services.Select(s => new ServiceViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Price = s.Price,
                IsSelected = false
            }).ToList();

            ServicesListBox.ItemsSource = _services;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CarNumberBox.Text))
            {
                MessageBox.Show("Введите номер автомобиля", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedServices = _services.Where(s => s.IsSelected).ToList();
            if (!selectedServices.Any())
            {
                MessageBox.Show("Выберите хотя бы одну услугу", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime orderTime;
            try
            {
                var timeString = TimeBox.Text;
                var date = DatePicker.SelectedDate ?? DateTime.Now;
                var timeParts = timeString.Split(':');
                if (timeParts.Length == 2)
                {
                    orderTime = new DateTime(date.Year, date.Month, date.Day,
                        int.Parse(timeParts[0]), int.Parse(timeParts[1]), 0);
                }
                else
                {
                    orderTime = DateTime.Now;
                }
            }
            catch
            {
                orderTime = DateTime.Now;
            }

            var order = new CarWashOrder
            {
                CarNumber = CarNumberBox.Text,
                CarModel = CarModelBox.Text,
                Time = orderTime,
                ShiftId = _shiftId,
                Notes = NotesBox.Text
            };

            var serviceIds = selectedServices.Select(s => s.Id).ToList();
            _dataService.AddOrder(order, serviceIds);

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