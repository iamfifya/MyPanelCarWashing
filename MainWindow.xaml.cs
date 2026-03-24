using MyPanelCarWashing.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyPanelCarWashing
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private List<CarWashOrder> _ordersList;
        private Shift _currentShift;

        public List<CarWashOrder> OrdersList
        {
            get
            {
                var Result = _ordersList;

                if (SearchFilter != "")
                {
                    Result = Result.Where(o => o.CarNumber.IndexOf(SearchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                o.CarModel.IndexOf(SearchFilter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                }

                return Result.Skip((CurrentPage - 1) * 6).Take(6).ToList();
            }
            set
            {
                _ordersList = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("OrdersList"));
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            // Получаем или создаем смену на сегодня
            _currentShift = Core.DB.GetShiftByDate(DateTime.Now);
            LoadOrders();
        }

        private void LoadOrders()
        {
            OrdersList = _currentShift.Orders.ToList();

            // Обновляем CurrentPage для корректной пагинации
            _currentPage = 1;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentPage"));
        }

        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (value > 0 && _ordersList != null)
                {
                    int totalPages = (int)Math.Ceiling((double)_ordersList.Count() / 6);
                    if (value <= totalPages && totalPages > 0)
                    {
                        _currentPage = value;
                        Invalidate();
                    }
                    else if (value == 1 && totalPages == 0)
                    {
                        _currentPage = value;
                        Invalidate();
                    }
                }
            }
        }

        private string _searchFilter = "";
        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                _searchFilter = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("OrdersList"));
            }
        }

        private void Invalidate()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("OrdersList"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentPage"));
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var addWin = new AddOrderWindow(_currentShift);
            if (addWin.ShowDialog() == true)
            {
                LoadOrders();
            }
        }

        private void EditItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedOrder = OrdersListView.SelectedItem as CarWashOrder;
            if (selectedOrder != null)
            {
                var editWin = new EditOrderServicesWindow(selectedOrder);
                if (editWin.ShowDialog() == true)
                {
                    LoadOrders();
                }
            }
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedOrder = OrdersListView.SelectedItem as CarWashOrder;
            if (selectedOrder != null)
            {
                if (MessageBox.Show("Удалить заказ?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        _currentShift.Orders.Remove(selectedOrder);
                        Core.DB.SaveData();
                        LoadOrders();
                        MessageBox.Show("Заказ удален", "Успешно",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void EmployeesButton_Click(object sender, RoutedEventArgs e)
        {
            var empWin = new EmployeeCardWindow();
            empWin.ShowDialog();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void SearchFilterTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            SearchFilter = SearchFilterTextBox.Text;
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e) => CurrentPage--;
        private void NextPage_Click(object sender, RoutedEventArgs e) => CurrentPage++;
    }
}