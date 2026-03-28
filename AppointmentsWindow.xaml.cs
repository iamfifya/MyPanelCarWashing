using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MyPanelCarWashing
{
    public partial class AppointmentsWindow : Window
    {
        private readonly DataService _dataService;
        private List<Appointment> _allAppointments;

        public AppointmentsWindow(DataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
            FilterDatePicker.SelectedDate = DateTime.Now;
            LoadAppointments();
        }

        private void LoadAppointments()
        {
            DateTime? filterDate = FilterDatePicker.SelectedDate;

            // Получаем все записи
            var allAppointments = _dataService.GetAllAppointments();

            System.Diagnostics.Debug.WriteLine($"=== Загрузка записей ===");
            System.Diagnostics.Debug.WriteLine($"Всего записей: {allAppointments.Count}");
            foreach (var a in allAppointments)
            {
                System.Diagnostics.Debug.WriteLine($"  {a.AppointmentDate:HH:mm} - {a.EndTime:HH:mm} | {a.CarModel}");
            }

            List<Appointment> appointments;
            if (filterDate.HasValue)
            {
                appointments = allAppointments.Where(a => a.AppointmentDate.Date == filterDate.Value.Date).ToList();
            }
            else
            {
                appointments = allAppointments;
            }

            var allServices = _dataService.GetAllServices();

            var displayAppointments = appointments.Select(a => new
            {
                a.Id,
                a.AppointmentDate,
                a.EndTime,
                a.CarModel,
                a.CarNumber,
                a.BoxNumber,
                ServicesList = string.Join(", ", a.ServiceIds.Select(id => allServices.FirstOrDefault(s => s.Id == id)?.Name ?? "Unknown")),
                TotalPrice = a.ServiceIds.Sum(id => allServices.FirstOrDefault(s => s.Id == id)?.Price ?? 0) + a.ExtraCost,
                a.IsCompleted,
                Status = a.IsCompleted ? "✓ Выполнена" : (a.AppointmentDate <= DateTime.Now ? "⚠️ Просрочена" : "⏳ Ожидает")
            }).ToList();

            AppointmentsListView.ItemsSource = displayAppointments;
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            LoadAppointments();
        }

        private void NewAppointmentButton_Click(object sender, RoutedEventArgs e)
        {
            var appointmentWin = new AppointmentWindow(_dataService);
            if (appointmentWin.ShowDialog() == true)
            {
                LoadAppointments();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = AppointmentsListView.SelectedItem;
            if (selected == null)
            {
                MessageBox.Show("Выберите запись для удаления", "Внимание");
                return;
            }

            // Получаем ID через рефлексию
            var idProperty = selected.GetType().GetProperty("Id");
            if (idProperty != null)
            {
                int id = (int)idProperty.GetValue(selected);

                if (MessageBox.Show("Удалить выбранную запись?", "Подтверждение",
                    MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _dataService.DeleteAppointment(id);
                    LoadAppointments();
                    MessageBox.Show("Запись удалена", "Успешно");
                }
            }
        }
        private void ShowAllButton_Click(object sender, RoutedEventArgs e)
        {
            var allAppointments = _dataService.GetAllAppointments();
            string message = $"Всего записей: {allAppointments.Count}\n\n";
            foreach (var a in allAppointments.OrderBy(a => a.AppointmentDate))
            {
                message += $"{a.AppointmentDate:dd.MM.yyyy HH:mm} - {a.EndTime:HH:mm} | {a.CarModel}\n";
            }
            MessageBox.Show(message, "Все записи");
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
