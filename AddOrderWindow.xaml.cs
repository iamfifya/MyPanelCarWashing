using MyPanelCarWashing.Models;
using MyPanelCarWashing.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace MyPanelCarWashing
{
    public partial class AddOrderWindow : Window
    {
        public CarWashOrder NewOrder { get; set; }
        public List<ServiceViewModel> Services { get; set; }
        public string SelectedBodyType { get; set; }
        private Shift _currentShift;

        public AddOrderWindow(Shift currentShift)
        {
            InitializeComponent();
            _currentShift = currentShift;
            NewOrder = new CarWashOrder
            {
                Time = DateTime.Now,
                ShiftId = currentShift.Id
            };

            LoadServices();
            DataContext = this;
        }

        private void LoadServices()
        {
            var allServices = Core.DB.GetAllServices();
            Services = allServices.Select(s => new ServiceViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Price = s.Price,
                IsSelected = false
            }).ToList();
        }

        private void BodyType_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as System.Windows.Controls.Border;
            if (border?.Tag != null)
            {
                SelectedBodyType = border.Tag.ToString();
                NewOrder.Notes = $"Тип кузова: {SelectedBodyType}";
                // Обновляем привязку
                OnPropertyChanged(nameof(SelectedBodyType));
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка обязательных полей
                if (string.IsNullOrWhiteSpace(NewOrder.CarModel))
                {
                    MessageBox.Show("Введите марку и модель автомобиля", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(NewOrder.CarNumber))
                {
                    MessageBox.Show("Введите государственный номер", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var selectedServices = Services.Where(s => s.IsSelected).ToList();
                if (!selectedServices.Any())
                {
                    MessageBox.Show("Выберите хотя бы одну услугу", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Добавляем заказ
                var serviceIds = selectedServices.Select(s => s.Id).ToList();
                Core.DB.AddOrder(NewOrder, serviceIds);

                DialogResult = true;
                MessageBox.Show($"Заказ добавлен\nСумма: {NewOrder.TotalPrice:C}", "Успешно",
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

        private void OnPropertyChanged(string propertyName)
        {
            var binding = GetBindingExpression(DataContextProperty);
            binding?.UpdateTarget();
        }
    }
}