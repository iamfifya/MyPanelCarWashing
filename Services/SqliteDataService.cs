using Microsoft.Data.Sqlite;
using MyPanelCarWashing.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MyPanelCarWashing.Services
{
    public class SqliteDataService
    {
        private readonly string _connectionString;

        public SqliteDataService()
        {
            string dbFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyCarWashing");
            if (!Directory.Exists(dbFolder))
                Directory.CreateDirectory(dbFolder);
            string dbPath = Path.Combine(dbFolder, "data.db");
            _connectionString = $"Data Source={dbPath}";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            string createTables = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    FullName TEXT NOT NULL,
                    Login TEXT UNIQUE NOT NULL,
                    Password TEXT NOT NULL,
                    IsAdmin INTEGER NOT NULL DEFAULT 0,
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    Phone TEXT
                );
                CREATE TABLE IF NOT EXISTS Services (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    DurationMinutes INTEGER NOT NULL,
                    Description TEXT,
                    IsActive INTEGER NOT NULL DEFAULT 1
                );
                CREATE TABLE IF NOT EXISTS ServicePrices (
                    ServiceId INTEGER NOT NULL,
                    BodyTypeCategory INTEGER NOT NULL,
                    Price REAL NOT NULL,
                    PRIMARY KEY (ServiceId, BodyTypeCategory),
                    FOREIGN KEY (ServiceId) REFERENCES Services(Id) ON DELETE CASCADE
                );
                CREATE TABLE IF NOT EXISTS Clients (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    FullName TEXT NOT NULL,
                    Phone TEXT,
                    CarModel TEXT,
                    CarNumber TEXT,
                    DefaultDiscountPercent REAL DEFAULT 0,
                    RegistrationDate TEXT NOT NULL,
                    LastVisitDate TEXT,
                    TotalSpent REAL DEFAULT 0,
                    VisitsCount INTEGER DEFAULT 0,
                    Notes TEXT
                );
                CREATE TABLE IF NOT EXISTS Shifts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Date TEXT NOT NULL,
                    StartTime TEXT NOT NULL,
                    EndTime TEXT,
                    IsClosed INTEGER NOT NULL DEFAULT 0,
                    Notes TEXT
                );
                CREATE TABLE IF NOT EXISTS ShiftEmployees (
                    ShiftId INTEGER NOT NULL,
                    EmployeeId INTEGER NOT NULL,
                    PRIMARY KEY (ShiftId, EmployeeId),
                    FOREIGN KEY (ShiftId) REFERENCES Shifts(Id) ON DELETE CASCADE,
                    FOREIGN KEY (EmployeeId) REFERENCES Users(Id) ON DELETE CASCADE
                );
                CREATE TABLE IF NOT EXISTS Orders (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ShiftId INTEGER NOT NULL,
                    ClientId INTEGER,
                    CarModel TEXT NOT NULL,
                    CarNumber TEXT NOT NULL,
                    BodyTypeCategory INTEGER NOT NULL,
                    Time TEXT NOT NULL,
                    BoxNumber INTEGER NOT NULL,
                    WasherId INTEGER NOT NULL,
                    PaymentMethod TEXT NOT NULL,
                    FinalPrice REAL NOT NULL,
                    BasePrice REAL NOT NULL,
                    OriginalTotalPrice REAL NOT NULL,
                    ExtraCost REAL DEFAULT 0,
                    ExtraCostReason TEXT,
                    DiscountPercent REAL DEFAULT 0,
                    DiscountAmount REAL DEFAULT 0,
                    Notes TEXT,
                    Status TEXT NOT NULL DEFAULT 'В ожидании',
                    FOREIGN KEY (ShiftId) REFERENCES Shifts(Id) ON DELETE CASCADE,
                    FOREIGN KEY (ClientId) REFERENCES Clients(Id) ON DELETE SET NULL,
                    FOREIGN KEY (WasherId) REFERENCES Users(Id)
                );
                CREATE TABLE IF NOT EXISTS OrderServices (
                    OrderId INTEGER NOT NULL,
                    ServiceId INTEGER NOT NULL,
                    PRIMARY KEY (OrderId, ServiceId),
                    FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE,
                    FOREIGN KEY (ServiceId) REFERENCES Services(Id)
                );
                CREATE TABLE IF NOT EXISTS Appointments (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ClientId INTEGER,
                    CarNumber TEXT NOT NULL,
                    CarModel TEXT NOT NULL,
                    CarBodyType TEXT,
                    BodyTypeCategory INTEGER NOT NULL,
                    AppointmentDate TEXT NOT NULL,
                    DurationMinutes INTEGER NOT NULL,
                    EndTime TEXT NOT NULL,
                    BoxNumber INTEGER NOT NULL,
                    ExtraCost REAL DEFAULT 0,
                    ExtraCostReason TEXT,
                    Notes TEXT,
                    IsCompleted INTEGER NOT NULL DEFAULT 0,
                    OrderId INTEGER,
                    FOREIGN KEY (ClientId) REFERENCES Clients(Id) ON DELETE SET NULL,
                    FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE SET NULL
                );
                CREATE TABLE IF NOT EXISTS AppointmentServices (
                    AppointmentId INTEGER NOT NULL,
                    ServiceId INTEGER NOT NULL,
                    PRIMARY KEY (AppointmentId, ServiceId),
                    FOREIGN KEY (AppointmentId) REFERENCES Appointments(Id) ON DELETE CASCADE,
                    FOREIGN KEY (ServiceId) REFERENCES Services(Id)
                );
                CREATE TABLE IF NOT EXISTS EmployeeSchedules (
                    EmployeeId INTEGER NOT NULL,
                    Year INTEGER NOT NULL,
                    Month INTEGER NOT NULL,
                    Day INTEGER NOT NULL,
                    Status TEXT NOT NULL,
                    PRIMARY KEY (EmployeeId, Year, Month, Day),
                    FOREIGN KEY (EmployeeId) REFERENCES Users(Id) ON DELETE CASCADE
                );
            ";

            using var command = new SqliteCommand(createTables, connection);
            command.ExecuteNonQuery();

            // Проверяем, есть ли данные, если нет – добавляем начальные услуги и админов
            if (!TableHasData(connection, "Services"))
                SeedDefaultData(connection);
        }

        private bool TableHasData(SqliteConnection connection, string tableName)
        {
            using var cmd = new SqliteCommand($"SELECT COUNT(*) FROM {tableName}", connection);
            return (long)cmd.ExecuteScalar() > 0;
        }

        private void SeedDefaultData(SqliteConnection connection)
        {
            // Добавляем администраторов
            using var cmd = new SqliteCommand(@"
                INSERT INTO Users (FullName, Login, Password, IsAdmin, IsActive, Phone)
                VALUES ('Анна', '1', '1', 1, 1, NULL);
                INSERT INTO Users (FullName, Login, Password, IsAdmin, IsActive, Phone)
                VALUES ('Анастасия', '1', '1', 1, 1, NULL);
                INSERT INTO Users (FullName, Login, Password, IsAdmin, IsActive, Phone)
                VALUES ('переименуй меня', '1', '1', 0, 1, NULL);
            ", connection);
            cmd.ExecuteNonQuery();

            // Добавляем услуги из CSV-логики (здесь можно упрощённо, либо вызвать старый метод)
            // Для краткости – добавим пару услуг вручную, но лучше перенести логику из FileDataService.LoadServicesFromCsv
            var services = new List<Service>();
            // ... тут можно заполнить services из существующего метода (скопируйте логику)
            // Но для экономии места – просто вставка через отдельный метод.
            InsertDefaultServices(connection);
        }

        private void InsertDefaultServices(SqliteConnection connection)
        {
            // Вставка услуг и их цен (аналогично FileDataService.LoadServicesFromCsv)
            // Здесь вы можете скопировать ту логику, но переписать на SQL-вставки.
            // Пример:
            var servicesData = new (string name, int duration, string desc, (int cat, decimal price)[] prices)[]
            {
                ("Техническая мойка", 30, "Двухфазная мойка без сушки, коврики",
                    new[] { (1,700m), (2,750m), (3,850m), (4,900m) }),
                ("Профессиональная мойка кузова", 45, "Двухфазная мойка, воск, турбосушка, коврики, педальный блок",
                    new[] { (1,1000m), (2,1200m), (3,1400m), (4,1700m) }),
                ("Комплекс \"ИЗИ\"", 60, "Трехфазная мойка, влажная уборка, пылесос, стекла, турбосушка, коврики, педальный блок, чернение",
                    new[] { (1,1900m), (2,2100m), (3,2400m), (4,2700m) }),
                ("Комплекс \"Глянец\"", 90, "Двухфазная мойка, кварц, влажная уборка, пылесос, стекла, багажник, полироль, турбосушка, коврики, педальный блок, чернение",
                    new[] { (1,2900m), (2,3100m), (3,3400m), (4,3700m) })
            };
            foreach (var s in servicesData)
            {
                using var cmd = new SqliteCommand(@"
                    INSERT INTO Services (Name, DurationMinutes, Description, IsActive)
                    VALUES (@name, @dur, @desc, 1);
                    SELECT last_insert_rowid();
                ", connection);
                cmd.Parameters.AddWithValue("@name", s.name);
                cmd.Parameters.AddWithValue("@dur", s.duration);
                cmd.Parameters.AddWithValue("@desc", s.desc);
                long serviceId = (long)cmd.ExecuteScalar();

                foreach (var price in s.prices)
                {
                    using var priceCmd = new SqliteCommand(@"
                        INSERT INTO ServicePrices (ServiceId, BodyTypeCategory, Price)
                        VALUES (@sid, @cat, @price);
                    ", connection);
                    priceCmd.Parameters.AddWithValue("@sid", serviceId);
                    priceCmd.Parameters.AddWithValue("@cat", price.cat);
                    priceCmd.Parameters.AddWithValue("@price", price.price);
                    priceCmd.ExecuteNonQuery();
                }
            }
        }

        // Далее – методы CRUD, которые заменят методы FileDataService и часть DataService.
        // Они будут возвращать AppData? Или лучше переписать всё на прямые запросы? 
        // Для постепенной замены я предлагаю создать прослойку: сохранить интерфейс IDataService,
        // а затем два реализации: JsonDataService (старый) и SqliteDataService (новый).
        // Но можно сразу переписывать всё.

        // Пример метода получения всех активных услуг:
        public List<Service> GetAllServices()
        {
            var services = new List<Service>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, DurationMinutes, Description, IsActive FROM Services WHERE IsActive = 1";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var service = new Service
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    DurationMinutes = reader.GetInt32(2),
                    Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                    IsActive = reader.GetBoolean(4),
                    PriceByBodyType = new Dictionary<int, decimal>()
                };
                services.Add(service);
            }

            // Загружаем цены
            foreach (var s in services)
            {
                var priceCmd = connection.CreateCommand();
                priceCmd.CommandText = "SELECT BodyTypeCategory, Price FROM ServicePrices WHERE ServiceId = @id";
                priceCmd.Parameters.AddWithValue("@id", s.Id);
                using var priceReader = priceCmd.ExecuteReader();
                while (priceReader.Read())
                {
                    s.PriceByBodyType[priceReader.GetInt32(0)] = priceReader.GetDecimal(1);
                }
            }
            return services;
        }

        // Метод для получения активной смены (текущей открытой)
        public Shift GetCurrentOpenShift()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Date, StartTime, EndTime, IsClosed, Notes 
                FROM Shifts 
                WHERE IsClosed = 0 AND Date = date('now')
                LIMIT 1";
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            var shift = new Shift
            {
                Id = reader.GetInt32(0),
                Date = DateTime.Parse(reader.GetString(1)),
                StartTime = DateTime.Parse(reader.GetString(2)),
                EndTime = reader.IsDBNull(3) ? (DateTime?)null : DateTime.Parse(reader.GetString(3)),
                IsClosed = reader.GetBoolean(4),
                Notes = reader.IsDBNull(5) ? null : reader.GetString(5),
                Orders = new List<CarWashOrder>(),
                EmployeeIds = new List<int>()
            };

            // Загружаем сотрудников смены
            var empCmd = connection.CreateCommand();
            empCmd.CommandText = "SELECT EmployeeId FROM ShiftEmployees WHERE ShiftId = @id";
            empCmd.Parameters.AddWithValue("@id", shift.Id);
            using var empReader = empCmd.ExecuteReader();
            while (empReader.Read())
                shift.EmployeeIds.Add(empReader.GetInt32(0));

            // Загружаем заказы смены
            var ordersCmd = connection.CreateCommand();
            ordersCmd.CommandText = @"
                SELECT Id, ClientId, CarModel, CarNumber, BodyTypeCategory, Time, BoxNumber, WasherId,
                       PaymentMethod, FinalPrice, BasePrice, OriginalTotalPrice, ExtraCost, ExtraCostReason,
                       DiscountPercent, DiscountAmount, Notes, Status
                FROM Orders WHERE ShiftId = @id";
            ordersCmd.Parameters.AddWithValue("@id", shift.Id);
            using var orderReader = ordersCmd.ExecuteReader();
            while (orderReader.Read())
            {
                var order = new CarWashOrder
                {
                    Id = orderReader.GetInt32(0),
                    ClientId = orderReader.IsDBNull(1) ? (int?)null : orderReader.GetInt32(1),
                    CarModel = orderReader.GetString(2),
                    CarNumber = orderReader.GetString(3),
                    BodyTypeCategory = orderReader.GetInt32(4),
                    Time = DateTime.Parse(orderReader.GetString(5)),
                    BoxNumber = orderReader.GetInt32(6),
                    WasherId = orderReader.GetInt32(7),
                    PaymentMethod = orderReader.GetString(8),
                    FinalPrice = (decimal)orderReader.GetDouble(9),
                    BasePrice = (decimal)orderReader.GetDouble(10),
                    OriginalTotalPrice = (decimal)orderReader.GetDouble(11),
                    ExtraCost = (decimal)orderReader.GetDouble(12),
                    ExtraCostReason = orderReader.IsDBNull(13) ? null : orderReader.GetString(13),
                    DiscountPercent = (decimal)orderReader.GetDouble(14),
                    DiscountAmount = (decimal)orderReader.GetDouble(15),
                    Notes = orderReader.IsDBNull(16) ? null : orderReader.GetString(16),
                    Status = orderReader.GetString(17),
                    ServiceIds = new List<int>()
                };
                // Загружаем услуги заказа
                var svcCmd = connection.CreateCommand();
                svcCmd.CommandText = "SELECT ServiceId FROM OrderServices WHERE OrderId = @oid";
                svcCmd.Parameters.AddWithValue("@oid", order.Id);
                using var svcReader = svcCmd.ExecuteReader();
                while (svcReader.Read())
                    order.ServiceIds.Add(svcReader.GetInt32(0));

                shift.Orders.Add(order);
            }
            return shift;
        }

        // Метод для старта смены
        public void StartShift(Shift shift)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            var insertShift = connection.CreateCommand();
            insertShift.CommandText = @"
                INSERT INTO Shifts (Date, StartTime, IsClosed, Notes)
                VALUES (@date, @start, 0, @notes);
                SELECT last_insert_rowid();";
            insertShift.Parameters.AddWithValue("@date", shift.Date.ToString("yyyy-MM-dd"));
            insertShift.Parameters.AddWithValue("@start", shift.StartTime.Value.ToString("yyyy-MM-dd HH:mm:ss"));
            insertShift.Parameters.AddWithValue("@notes", shift.Notes ?? "");
            long shiftId = (long)insertShift.ExecuteScalar();
            shift.Id = (int)shiftId;

            foreach (var empId in shift.EmployeeIds)
            {
                var empCmd = connection.CreateCommand();
                empCmd.CommandText = "INSERT INTO ShiftEmployees (ShiftId, EmployeeId) VALUES (@sid, @eid)";
                empCmd.Parameters.AddWithValue("@sid", shiftId);
                empCmd.Parameters.AddWithValue("@eid", empId);
                empCmd.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        // Остальные методы (AddOrder, UpdateOrder, GetAppointments, etc.) реализуются аналогично.
        // Для полноценной замены потребуется реализовать весь интерфейс DataService, но постепенно.
    }
}
