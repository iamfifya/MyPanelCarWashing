using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
using System;
using System.Linq;
using System.Windows;

namespace MyPanelCarWashing
{
    public partial class AddEditServiceWindow : Window
    {
        private readonly SqliteDataService _SqliteDataService;
        public Service CurrentService { get; set; }
        public string WindowTitle { get; set; }

        public AddEditServiceWindow(SqliteDataService SqliteDataService, Service service)
        {
            InitializeComponent();
            _SqliteDataService = SqliteDataService;

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

                // Сохраняем цены для каждой категории
                CurrentService.PriceByBodyType.Clear();

                if (decimal.TryParse(PriceCategory1TextBox.Text, out var p1) && p1 >= 0)
                    CurrentService.PriceByBodyType[1] = p1;
                if (decimal.TryParse(PriceCategory2TextBox.Text, out var p2) && p2 >= 0)
                    CurrentService.PriceByBodyType[2] = p2;
                if (decimal.TryParse(PriceCategory3TextBox.Text, out var p3) && p3 >= 0)
                    CurrentService.PriceByBodyType[3] = p3;
                if (decimal.TryParse(PriceCategory4TextBox.Text, out var p4) && p4 >= 0)
                    CurrentService.PriceByBodyType[4] = p4;

                // Если нет цен — ставим 0 для всех категорий (чтобы не было ошибок)
                for (int cat = 1; cat <= 4; cat++)
                {
                    if (!CurrentService.PriceByBodyType.ContainsKey(cat))
                        CurrentService.PriceByBodyType[cat] = 0;
                }

                if (CurrentService.Id == 0)
                {
                    // === НОВАЯ УСЛУГА: используем SqliteDataService.AddService() для правильного ID ===
                    _SqliteDataService.AddService(CurrentService);
                    System.Diagnostics.Debug.WriteLine($"Добавлена новая услуга: ID={CurrentService.Id}, Name={CurrentService.Name}");
                }
                else
                {
                    // === ОБНОВЛЕНИЕ: используем SqliteDataService.UpdateService() ===
                    _SqliteDataService.UpdateService(CurrentService);
                    System.Diagnostics.Debug.WriteLine($"Обновлена услуга: ID={CurrentService.Id}, Name={CurrentService.Name}");
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при сохранении услуги: {ex}");
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
