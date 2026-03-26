using MyPanelCarWashing.Models;
using System;
using System.Linq;
using System.Windows;

namespace MyPanelCarWashing
{
    public partial class AddEditServiceWindow : Window
    {
        public Service CurrentService { get; set; }
        public string WindowTitle { get; set; }

        public AddEditServiceWindow(Service service)
        {
            InitializeComponent();

            if (service == null)
            {
                CurrentService = new Service
                {
                    Id = 0,
                    Name = "",
                    Price = 0,
                    DurationMinutes = 30,
                    Description = "",
                    IsActive = true
                };
                WindowTitle = "➕ Добавление услуги";
            }
            else
            {
                CurrentService = new Service
                {
                    Id = service.Id,
                    Name = service.Name,
                    Price = service.Price,
                    DurationMinutes = service.DurationMinutes,
                    Description = service.Description,
                    IsActive = service.IsActive
                };
                WindowTitle = "✏ Редактирование услуги";
            }

            DataContext = this;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка обязательных полей
                if (string.IsNullOrWhiteSpace(CurrentService.Name))
                {
                    MessageBox.Show("Введите название услуги", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (CurrentService.Price <= 0)
                {
                    MessageBox.Show("Введите корректную цену (больше 0)", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (CurrentService.DurationMinutes <= 0)
                {
                    MessageBox.Show("Введите корректную длительность (больше 0)", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Загружаем данные
                var appData = FileDataService.LoadData();

                if (CurrentService.Id == 0)
                {
                    // Новая услуга
                    CurrentService.Id = appData.GetNextServiceId();
                    appData.Services.Add(CurrentService);
                }
                else
                {
                    // Обновляем существующую
                    var existing = appData.Services.FirstOrDefault(s => s.Id == CurrentService.Id);
                    if (existing != null)
                    {
                        existing.Name = CurrentService.Name;
                        existing.Price = CurrentService.Price;
                        existing.DurationMinutes = CurrentService.DurationMinutes;
                        existing.Description = CurrentService.Description;
                        existing.IsActive = CurrentService.IsActive;
                    }
                }

                // Сохраняем
                FileDataService.SaveData(appData);
                Core.RefreshData();

                DialogResult = true;
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