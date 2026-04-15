using MyPanelCarWashing.Models;
using MyPanelCarWashing.Services;
using NLog.Config;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace MyPanelCarWashing.Controls
{
    public partial class CashboxOverlay : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private SqliteDataService _db;
        private Shift _currentShift;

        // Списки и суммы для привязки (Binding)
        public ObservableCollection<Transaction> Transactions { get; set; } = new ObservableCollection<Transaction>();
        
        private decimal _cashInHand;
        public decimal CashInHand 
        { 
            get => _cashInHand; 
            set { _cashInHand = value; OnPropertyChanged(nameof(CashInHand)); } 
        }

        private decimal _totalExpenses;
        public decimal TotalExpenses 
        { 
            get => _totalExpenses; 
            set { _totalExpenses = value; OnPropertyChanged(nameof(TotalExpenses)); } 
        }

        private decimal _netCashProfit;
        public decimal NetCashProfit 
        { 
            get => _netCashProfit; 
            set { _netCashProfit = value; OnPropertyChanged(nameof(NetCashProfit)); }
        }

        public CashboxOverlay()
        {
            InitializeComponent();
            DataContext = this;
        }

        // Вызывается из MainWindow при нажатии кнопки "Касса"
        public void Show(Shift shift, SqliteDataService db)
        {
            if (shift == null)
            {
                MessageBox.Show("Смена не начата! Сначала откройте смену.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _db = db;
            _currentShift = shift;

            RefreshData();

            this.Visibility = Visibility.Visible;
            OverlayBackground.Visibility = Visibility.Visible;
            PopupPanel.Visibility = Visibility.Visible;

            // Запускаем красивую анимацию появления
            var sb = (Storyboard)FindResource("ShowAnimation");
            sb.Begin();
        }

        // ГЛАВНЫЙ МОЗГ: Расчет всей математики кассы
        private void RefreshData()
        {
            if (_currentShift == null || _db == null) return;

            // Загружаем сотрудников смены
            ActiveEmployees.Clear();
            var allUsers = _db.GetAllUsers();
            foreach (var empId in _currentShift.EmployeeIds)
            {
                var user = allUsers.FirstOrDefault(u => u.Id == empId);
                if (user != null) ActiveEmployees.Add(user);
            }

            // Заставляем UI проверить, нужно ли показывать второй комбобокс
            OnPropertyChanged(nameof(EmployeeComboVisibility));

            // 1. Грузим транзакции из базы
            var list = _db.GetTransactionsByShiftId(_currentShift.Id);
            Transactions.Clear();
            foreach (var t in list) Transactions.Add(t);

            // 2. Получаем наличную выручку (только выполненные заказы за нал)
            decimal cashRevenue = _currentShift.Orders
                .Where(o => o.Status == "Выполнен" && o.PaymentMethod == "Наличные")
                .Sum(o => o.FinalPrice);

            // 3. Считаем движения по кассе (Транзакции)
            decimal deposits = list.Where(t => t.Type == "Приход" || t.Type == "Размен").Sum(t => t.Amount);
            decimal advances = list.Where(t => t.Type == "Аванс мойщику").Sum(t => t.Amount);
            decimal expenses = list.Where(t => t.Type == "Расход").Sum(t => t.Amount);
            decimal withdrawals = list.Where(t => t.Type == "Инкассация").Sum(t => t.Amount);

            // ФОРМУЛА: Сколько сейчас денег в ящике
            CashInHand = cashRevenue + deposits - (advances + expenses + withdrawals);

            // ФОРМУЛА: Всего ушло денег (расходы + авансы)
            TotalExpenses = expenses + advances;

            // ФОРМУЛА: Наличная прибыль компании (65% от нала - расходы)
            // Доплата до минималки тоже вычитается из прибыли компании
            var allServices = _db.GetAllServices();
            decimal totalTopUp = 0;
            
            // Считаем, сколько компания "доплатила" до минималки в этой смене
            var groupedByWasher = _currentShift.Orders.Where(o => o.Status == "Выполнен").GroupBy(o => o.WasherId);
            foreach(var group in groupedByWasher)
            {
                var stats = OrderMath.CalculateShiftStats(group, allServices);
                totalTopUp += stats.MinWageTopUp;
            }

            NetCashProfit = (cashRevenue * 0.65m) - expenses - totalTopUp;
        }

        // Обработчик кнопки "Провести операцию"
        private void AddTransaction_Click(object sender, RoutedEventArgs e)
        {
            if (_currentShift == null) return;

            if (!decimal.TryParse(AmountText.Text.Replace(".", ","), out decimal amt) || amt <= 0)
            {
                MessageBox.Show("Введите корректную сумму больше нуля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string type = SelectedOperationType;

            if (string.IsNullOrEmpty(type))
            {
                MessageBox.Show("Выберите тип операции", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string comment = string.IsNullOrWhiteSpace(CommentText.Text) ? "Без комментария" : CommentText.Text;
            int? empId = null;

            // Если это аванс, проверяем, выбрали ли сотрудника
            if (type == "Аванс мойщику")
            {
                if (SelectedEmployee == null)
                {
                    MessageBox.Show("Пожалуйста, выберите сотрудника, которому выдается аванс.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                empId = SelectedEmployee.Id;
                comment = $"Аванс: {SelectedEmployee.FullName}. {comment}";
            }

            var newTransaction = new Transaction
            {
                ShiftId = _currentShift.Id,
                EmployeeId = empId, // Сохраняем ID сотрудника!
                Amount = amt,
                Type = type,
                Comment = comment,
                DateTime = DateTime.Now
            };

            try
            {
                _db.AddTransaction(newTransaction);

                MessageBox.Show("Операция успешно проведена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                Logger.Info($"Касса: проведена операция '{type}' на сумму {amt}₽", "CASHBOX");

                // ✅ Отладка ДО сброса
                Debug.WriteLine($"[AddTransaction] Type={type}, Amount={amt}");

                var justSaved = _db.GetTransactionsByShiftId(_currentShift.Id).FirstOrDefault();
                Debug.WriteLine($"[DB Verify] Saved: Type='{justSaved?.Type}', Amount={justSaved?.Amount}");

                AmountText.Clear();
                CommentText.Clear();
                SelectedOperationType = null; // Сброс ПОСЛЕ сохранения и логирования
                SelectedEmployee = null; // Сброс ПОСЛЕ сохранения и логирования

                RefreshData();

            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка при сохранении транзакции", ex, "CASHBOX");
                MessageBox.Show("Не удалось сохранить операцию: " + ex.Message);
            }
        }


        private string _selectedOperationType;
        public string SelectedOperationType
        {
            get => _selectedOperationType;
            set
            {
                _selectedOperationType = value;
                OnPropertyChanged(nameof(SelectedOperationType));
                // ДОБАВИТЬ: Обновляем видимость поля выбора сотрудника
                OnPropertyChanged(nameof(EmployeeComboVisibility));
            }
        }

        // Список сотрудников, которые работают в текущей смене
        public ObservableCollection<User> ActiveEmployees { get; set; } = new ObservableCollection<User>();

        private User _selectedEmployee;
        public User SelectedEmployee
        {
            get => _selectedEmployee;
            set
            {
                _selectedEmployee = value;
                OnPropertyChanged(nameof(SelectedEmployee));
            }
        }

        // Управляет видимостью второго комбобокса
        public Visibility EmployeeComboVisibility =>
            SelectedOperationType == "Аванс мойщику" ? Visibility.Visible : Visibility.Collapsed;

        // Также добавьте список доступных типов операций:
        public List<KeyValuePair<string, string>> OperationTypes { get; } =
            new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("Приход", "Приход"),
                new KeyValuePair<string, string>("Расход", "Расход"),
                new KeyValuePair<string, string>("Аванс мойщику", "Аванс мойщику"),
                new KeyValuePair<string, string>("Размен", "Размен"),
                new KeyValuePair<string, string>("Инкассация", "Инкассация")
            };

        private async void Close_Click(object sender, RoutedEventArgs e)
        {
            // Здесь можно добавить анимацию закрытия (HideAnimation), если она есть
            this.Visibility = Visibility.Collapsed;
        }

        protected void OnPropertyChanged(string name)
        {
            Debug.WriteLine($"[PropertyChanged] {name}");
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
