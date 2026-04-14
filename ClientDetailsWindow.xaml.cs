using LiveCharts;
using LiveCharts.Wpf;
using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace MyPanelCarWashing.Controls
{
    public partial class ClientDetailsOverlay : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private Client _selectedClient;
        public Client SelectedClient
        {
            get => _selectedClient;
            set { _selectedClient = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedClient))); }
        }

        public string CarInfo => $"🚗 {SelectedClient?.CarModel} ({SelectedClient?.CarNumber})";
        public SeriesCollection ChartSeries { get; set; }
        public string[] ChartLabels { get; set; }
        public Func<double, string> YFormatter { get; set; }
        public List<TopServiceItem> TopServices { get; set; }

        public ClientDetailsOverlay()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        public void ShowClient(Client client, SqliteDataService db)
        {
            SelectedClient = client;
            LoadAnalytics(db);

            // Включаем видимость перед анимацией
            this.Visibility = Visibility.Visible;
            OverlayBackground.Visibility = Visibility.Visible;
            PopupPanel.Visibility = Visibility.Visible;

            // Запуск анимации
            var sb = (Storyboard)FindResource("ShowAnimation");
            sb.Begin();

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CarInfo)));
        }

        private void LoadAnalytics(SqliteDataService db)
        {
            var orders = db.GetOrdersByClientId(SelectedClient.Id).Where(o => o.Status == "Выполнен").ToList();
            var allServices = db.GetAllServices();

            var serviceCounts = new Dictionary<int, int>();
            foreach (var o in orders)
            {
                foreach (var sid in o.ServiceIds)
                {
                    if (!serviceCounts.ContainsKey(sid)) serviceCounts[sid] = 0;
                    serviceCounts[sid]++;
                }
            }

            TopServices = serviceCounts.OrderByDescending(kv => kv.Value).Take(3)
                .Select(kv => new TopServiceItem
                {
                    ServiceName = allServices.FirstOrDefault(s => s.Id == kv.Key)?.Name ?? "Услуга",
                    Count = kv.Value
                }).ToList();

            var months = new List<string>();
            var values = new ChartValues<double>();
            for (int i = 5; i >= 0; i--)
            {
                var date = DateTime.Now.AddMonths(-i);
                months.Add(date.ToString("MMM yy"));
                var sum = orders.Where(o => o.Time.Year == date.Year && o.Time.Month == date.Month).Sum(o => o.FinalPrice);
                values.Add((double)sum);
            }

            ChartLabels = months.ToArray();
            ChartSeries = new SeriesCollection { new ColumnSeries { Values = values, Fill = System.Windows.Media.Brushes.DodgerBlue } };
            YFormatter = v => v.ToString("N0") + " ₽";

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TopServices)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ChartSeries)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ChartLabels)));
        }

        private async void Close_Click(object sender, RoutedEventArgs e)
        {
            // Запуск анимации скрытия
            var sb = (Storyboard)FindResource("HideAnimation");
            sb.Begin();

            // Ждем завершения анимации перед тем, как скрыть элементы полностью
            await Task.Delay(200);

            this.Visibility = Visibility.Collapsed;
            OverlayBackground.Visibility = Visibility.Collapsed;
            PopupPanel.Visibility = Visibility.Collapsed;
        }
    }

    public class TopServiceItem
    {
        public string ServiceName { get; set; }
        public int Count { get; set; }
    }
}
