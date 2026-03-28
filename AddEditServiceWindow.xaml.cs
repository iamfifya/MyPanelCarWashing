using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
using System;
using System.Linq;
using System.Windows;

namespace MyPanelCarWashing
{
    public partial class AddEditServiceWindow : Window
    {
        private readonly DataService _dataService;
        public Service CurrentService { get; set; }
        public string WindowTitle { get; set; }

        public AddEditServiceWindow(DataService dataService, Service service)
        {
            InitializeComponent();
            _dataService = dataService;

            if (service == null)
            {
                CurrentService = new Service
                {
                    Id = 0,
                    Name = "",
                    DurationMinutes = 30,
                    Description = "",
                    IsActive = true,
                    PriceByBodyType = new System.Collections.Generic.Dictionary<int, decimal>()
                };
                WindowTitle = "➕ Добавление услуги";
            }
            else
            {
                CurrentService = new Service
                {
                    Id = service.Id,
                    Name = service.Name,
                    DurationMinutes = service.DurationMinutes,
                    Description = service.Description,
                    IsActive = service.IsActive,
                    PriceByBodyType = new System.Collections.Generic.Dictionary<int, decimal>(service.PriceByBodyType)
                };
                WindowTitle = "✏ Редактирование услуги";
            }

            DataContext = this;
            LoadPricesToUI();
        }

        private void LoadPricesToUI()
        {
            if (CurrentService.PriceByBodyType.TryGetValue(1, out var p1))
                PriceCategory1TextBox.Text = p1.ToString();
            if (CurrentService.PriceByBodyType.TryGetValue(2, out var p2))
                PriceCategory2TextBox.Text = p2.ToString();
            if (CurrentService.PriceByBodyType.TryGetValue(3, out var p3))
                PriceCategory3TextBox.Text = p3.ToString();
            if (CurrentService.PriceByBodyType.TryGetValue(4, out var p4))
                PriceCategory4TextBox.Text = p4.ToString();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(CurrentService.Name))
                {
                    MessageBox.Show("Введите название услуги", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (CurrentService.DurationMinutes <= 0)
                {
                    MessageBox.Show("Введите корректную длительность (больше 0)", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var appData = FileDataService.LoadData();

                // Проверка на дубликат названия
                var existingByName = appData.Services.FirstOrDefault(s =>
                    s.Name.Equals(CurrentService.Name, StringComparison.OrdinalIgnoreCase));

                if (existingByName != null && (CurrentService.Id == 0 || existingByName.Id != CurrentService.Id))
                {
                    MessageBox.Show($"Услуга с названием \"{CurrentService.Name}\" уже существует!\n\n" +
                        "Пожалуйста, используйте другое название.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Сохраняем цены для каждой категории
                CurrentService.PriceByBodyType.Clear();

                if (decimal.TryParse(PriceCategory1TextBox.Text, out var p1) && p1 > 0)
                    CurrentService.PriceByBodyType[1] = p1;
                if (decimal.TryParse(PriceCategory2TextBox.Text, out var p2) && p2 > 0)
                    CurrentService.PriceByBodyType[2] = p2;
                if (decimal.TryParse(PriceCategory3TextBox.Text, out var p3) && p3 > 0)
                    CurrentService.PriceByBodyType[3] = p3;
                if (decimal.TryParse(PriceCategory4TextBox.Text, out var p4) && p4 > 0)
                    CurrentService.PriceByBodyType[4] = p4;

                // Если нет цен для всех категорий, используем категорию 1 как базовую
                if (!CurrentService.PriceByBodyType.ContainsKey(1) && CurrentService.PriceByBodyType.Any())
                {
                    var firstPrice = CurrentService.PriceByBodyType.First().Value;
                    CurrentService.PriceByBodyType[1] = firstPrice;
                }

                if (CurrentService.Id == 0)
                {
                    // Новая услуга - получаем следующий ID
                    int newId = appData.Services.Any() ? appData.Services.Max(s => s.Id) + 1 : 1;
                    CurrentService.Id = newId;
                    appData.Services.Add(CurrentService);
                    System.Diagnostics.Debug.WriteLine($"Добавлена новая услуга: ID={CurrentService.Id}, Name={CurrentService.Name}");
                }
                else
                {
                    // Обновляем существующую
                    var existing = appData.Services.FirstOrDefault(s => s.Id == CurrentService.Id);
                    if (existing != null)
                    {
                        existing.Name = CurrentService.Name;
                        existing.DurationMinutes = CurrentService.DurationMinutes;
                        existing.Description = CurrentService.Description;
                        existing.IsActive = CurrentService.IsActive;
                        existing.PriceByBodyType = CurrentService.PriceByBodyType;
                        System.Diagnostics.Debug.WriteLine($"Обновлена услуга: ID={CurrentService.Id}, Name={CurrentService.Name}");
                    }
                }

                FileDataService.SaveData(appData);

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
