using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using MyPanelCarWashing.Models;

namespace MyPanelCarWashing.Services
{
    public class SqliteDataService
    {
        private readonly string _connectionString;

        // Статический конструктор - вызывается один раз при первом обращении к классу
        

        public SqliteDataService()
        {
            string dbFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyCarWashing");
            if (!Directory.Exists(dbFolder))
                Directory.CreateDirectory(dbFolder);
            string dbPath = Path.Combine(dbFolder, "data.db");
            _connectionString = $"Data Source={dbPath}";
            InitializeDatabase();
            CheckServices();
        }



        private void InitializeDatabase()
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
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
                TotalPrice REAL NOT NULL,
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
                CarNumber TEXT NOT NULL,
                CarModel TEXT NOT NULL,
                CarBodyType TEXT,
                BodyTypeCategory INTEGER NOT NULL,
                AppointmentDate TEXT NOT NULL,
                DurationMinutes INTEGER NOT NULL,
                BoxNumber INTEGER NOT NULL,
                ExtraCost REAL DEFAULT 0,
                ExtraCostReason TEXT,
                Notes TEXT,
                IsCompleted INTEGER NOT NULL DEFAULT 0,
                OrderId INTEGER,
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
                using (var command = new SQLiteCommand(createTables, connection))
                    command.ExecuteNonQuery();

                System.Diagnostics.Debug.WriteLine("Таблицы созданы или уже существуют");

                // Добавляем начальные данные только если их нет
                AddInitialData(connection);
            }
        }

        private void AddInitialData(SQLiteConnection connection)
        {
            // === ДОБАВЛЯЕМ ПОЛЬЗОВАТЕЛЕЙ (только если нет ни одного) ===
            using (var checkCmd = new SQLiteCommand("SELECT COUNT(*) FROM Users", connection))
            {
                long count = (long)checkCmd.ExecuteScalar();
                System.Diagnostics.Debug.WriteLine($"Пользователей в БД: {count}");

                if (count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("Добавляем начальных пользователей...");

                    string insertUsers = @"
                INSERT OR IGNORE INTO Users (FullName, Login, Password, IsAdmin, IsActive, Phone)
                VALUES ('Анна', '1', '1', 1, 1, NULL);
                INSERT OR IGNORE INTO Users (FullName, Login, Password, IsAdmin, IsActive, Phone)
                VALUES ('Анастасия', '1', '1', 1, 1, NULL);
                INSERT OR IGNORE INTO Users (FullName, Login, Password, IsAdmin, IsActive, Phone)
                VALUES ('переименуй меня', '1', '1', 0, 1, NULL);
            ";
                    using (var cmd = new SQLiteCommand(insertUsers, connection))
                        cmd.ExecuteNonQuery();

                    System.Diagnostics.Debug.WriteLine("Пользователи добавлены");
                }
            }

            // === ДОБАВЛЯЕМ УСЛУГИ (только если нет ни одной) ===
            using (var checkCmd = new SQLiteCommand("SELECT COUNT(*) FROM Services", connection))
            {
                long count = (long)checkCmd.ExecuteScalar();
                System.Diagnostics.Debug.WriteLine($"Услуг в БД: {count}");

                if (count > 0)
                {
                    System.Diagnostics.Debug.WriteLine("Услуги уже есть, пропускаем добавление");
                    return;
                }
            }

            System.Diagnostics.Debug.WriteLine("=== Добавляем все базовые услуги ===");

            var servicesData = new List<ServiceSeed>();

            // Основные услуги (из Лист1)
            servicesData.Add(new ServiceSeed
            {
                Name = "Техническая мойка",
                Duration = 30,
                Description = "Двухфазная мойка без сушки, коврики",
                Prices = new List<PriceSeed> {
            new PriceSeed { Cat = 1, Price = 700m },
            new PriceSeed { Cat = 2, Price = 750m },
            new PriceSeed { Cat = 3, Price = 850m },
            new PriceSeed { Cat = 4, Price = 900m }
        }
            });
            servicesData.Add(new ServiceSeed
            {
                Name = "Профессиональная мойка кузова",
                Duration = 45,
                Description = "Двухфазная мойка, воск, турбосушка, коврики, педальный блок",
                Prices = new List<PriceSeed> {
            new PriceSeed { Cat = 1, Price = 1000m },
            new PriceSeed { Cat = 2, Price = 1200m },
            new PriceSeed { Cat = 3, Price = 1400m },
            new PriceSeed { Cat = 4, Price = 1700m }
        }
            });
            servicesData.Add(new ServiceSeed
            {
                Name = "Комплекс \"ИЗИ\"",
                Duration = 60,
                Description = "Трехфазная мойка, влажная уборка, пылесос, стекла, турбосушка, коврики, педальный блок, чернение",
                Prices = new List<PriceSeed> {
            new PriceSeed { Cat = 1, Price = 1900m },
            new PriceSeed { Cat = 2, Price = 2100m },
            new PriceSeed { Cat = 3, Price = 2400m },
            new PriceSeed { Cat = 4, Price = 2700m }
        }
            });
            servicesData.Add(new ServiceSeed
            {
                Name = "Комплекс \"Глянец\"",
                Duration = 90,
                Description = "Двухфазная мойка, кварц, влажная уборка, пылесос, стекла, багажник, полироль, турбосушка, коврики, педальный блок, чернение",
                Prices = new List<PriceSeed> {
            new PriceSeed { Cat = 1, Price = 2900m },
            new PriceSeed { Cat = 2, Price = 3100m },
            new PriceSeed { Cat = 3, Price = 3400m },
            new PriceSeed { Cat = 4, Price = 3700m }
        }
            });

            // Дополнительные услуги (из Лист2)
            servicesData.Add(new ServiceSeed
            {
                Name = "Багажник(цена от)",
                Duration = 30,
                Description = "",
                Prices = new List<PriceSeed> { new PriceSeed { Cat = 1, Price = 300m } }
            });
            servicesData.Add(new ServiceSeed
            {
                Name = "Пылесос(цена от)",
                Duration = 30,
                Description = "",
                Prices = new List<PriceSeed> { new PriceSeed { Cat = 1, Price = 300m } }
            });
            servicesData.Add(new ServiceSeed
            {
                Name = "Влажная уборка(цена от)",
                Duration = 30,
                Description = "",
                Prices = new List<PriceSeed> { new PriceSeed { Cat = 1, Price = 300m } }
            });
            servicesData.Add(new ServiceSeed
            {
                Name = "Стекла(цена от)",
                Duration = 30,
                Description = "",
                Prices = new List<PriceSeed> { new PriceSeed { Cat = 1, Price = 300m } }
            });
            servicesData.Add(new ServiceSeed
            {
                Name = "Кварцевое покрытие",
                Duration = 60,
                Description = "Защитное кварцевое покрытие кузова",
                Prices = new List<PriceSeed> {
            new PriceSeed { Cat = 1, Price = 850m },
            new PriceSeed { Cat = 2, Price = 950m },
            new PriceSeed { Cat = 3, Price = 1050m },
            new PriceSeed { Cat = 4, Price = 1150m }
        }
            });
            servicesData.Add(new ServiceSeed
            {
                Name = "Полироль пластика",
                Duration = 30,
                Description = "Полировка пластиковых элементов",
                Prices = new List<PriceSeed> { new PriceSeed { Cat = 1, Price = 300m } }
            });
            servicesData.Add(new ServiceSeed
            {
                Name = "Кондиционер кожи(цена от)",
                Duration = 45,
                Description = "Уход за кожаным салоном",
                Prices = new List<PriceSeed> { new PriceSeed { Cat = 1, Price = 1500m } }
            });
            servicesData.Add(new ServiceSeed
            {
                Name = "Чистка руля",
                Duration = 15,
                Description = "Химчистка рулевого колеса",
                Prices = new List<PriceSeed> { new PriceSeed { Cat = 1, Price = 500m } }
            });
            servicesData.Add(new ServiceSeed
            {
                Name = "Обработка силиконом",
                Duration = 20,
                Description = "Силиконовая обработка уплотнительных резинок",
                Prices = new List<PriceSeed> { new PriceSeed { Cat = 1, Price = 300m } }
            });
            servicesData.Add(new ServiceSeed
            {
                Name = "Удаление насекомых",
                Duration = 15,
                Description = "Удаление следов насекомых",
                Prices = new List<PriceSeed> { new PriceSeed { Cat = 1, Price = 250m } }
            });
            servicesData.Add(new ServiceSeed
            {
                Name = "Битум, металлические вкрапления(цена за элемент)",
                Duration = 15,
                Description = "Удаление битумных пятен",
                Prices = new List<PriceSeed> { new PriceSeed { Cat = 1, Price = 150m } }
            });
            servicesData.Add(new ServiceSeed
            {
                Name = "Антидождь быстрый",
                Duration = 15,
                Description = "Быстрое нанесение антидождя",
                Prices = new List<PriceSeed> { new PriceSeed { Cat = 1, Price = 150m } }
            });
            servicesData.Add(new ServiceSeed
            {
                Name = "Антидождь крайтека(передний контур)",
                Duration = 30,
                Description = "Качественный антидождь на переднюю часть",
                Prices = new List<PriceSeed> { new PriceSeed { Cat = 1, Price = 3500m } }
            });
            servicesData.Add(new ServiceSeed
            {
                Name = "Антидождь крайтека(вкруг)",
                Duration = 45,
                Description = "Качественный антидождь на весь автомобиль",
                Prices = new List<PriceSeed> { new PriceSeed { Cat = 1, Price = 6000m } }
            });
            servicesData.Add(new ServiceSeed
            {
                Name = "Очистка дисков",
                Duration = 20,
                Description = "Очистка колесных дисков",
                Prices = new List<PriceSeed> { new PriceSeed { Cat = 1, Price = 300m } }
            });
            servicesData.Add(new ServiceSeed
            {
                Name = "Мойка колес",
                Duration = 30,
                Description = "Полная мойка колес щеткой с шампунем",
                Prices = new List<PriceSeed> { new PriceSeed { Cat = 1, Price = 1200m } }
            });
            servicesData.Add(new ServiceSeed
            {
                Name = "Мойка двигателя(цена от)",
                Duration = 45,
                Description = "Мойка подкапотного пространства",
                Prices = new List<PriceSeed> { new PriceSeed { Cat = 1, Price = 1500m } }
            });

            foreach (var s in servicesData)
            {
                using (var ins = new SQLiteCommand(@"
            INSERT INTO Services (Name, DurationMinutes, Description, IsActive)
            VALUES (@name, @dur, @desc, 1);
            SELECT last_insert_rowid();
        ", connection))
                {
                    ins.Parameters.AddWithValue("@name", s.Name);
                    ins.Parameters.AddWithValue("@dur", s.Duration);
                    ins.Parameters.AddWithValue("@desc", s.Description ?? (object)DBNull.Value);
                    long serviceId = (long)ins.ExecuteScalar();
                    System.Diagnostics.Debug.WriteLine($"Добавлена услуга: ID={serviceId}, Name={s.Name}");

                    foreach (var p in s.Prices)
                    {
                        using (var priceCmd = new SQLiteCommand(@"
                    INSERT INTO ServicePrices (ServiceId, BodyTypeCategory, Price)
                    VALUES (@sid, @cat, @price);
                ", connection))
                        {
                            priceCmd.Parameters.AddWithValue("@sid", serviceId);
                            priceCmd.Parameters.AddWithValue("@cat", p.Cat);
                            priceCmd.Parameters.AddWithValue("@price", p.Price);
                            priceCmd.ExecuteNonQuery();
                        }
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"Всего добавлено услуг: {servicesData.Count}");
        }

        private class ServiceSeed { public string Name; public int Duration; public string Description; public List<PriceSeed> Prices; }
        private class PriceSeed { public int Cat; public decimal Price; }

        private bool TableHasData(SQLiteConnection connection, string tableName)
        {
            using (var cmd = new SQLiteCommand($"SELECT COUNT(*) FROM {tableName}", connection))
                return (long)cmd.ExecuteScalar() > 0;
        }

        private void SeedDefaultData(SQLiteConnection connection)
        {
            // Проверяем, есть ли уже пользователи
            using (var checkCmd = new SQLiteCommand("SELECT COUNT(*) FROM Users", connection))
            {
                long userCount = (long)checkCmd.ExecuteScalar();
                System.Diagnostics.Debug.WriteLine($"SeedDefaultData: найдено пользователей: {userCount}");

                if (userCount == 0)
                {
                    System.Diagnostics.Debug.WriteLine("Добавляем начальных пользователей...");
                    using (var cmd = new SQLiteCommand(@"
                INSERT INTO Users (FullName, Login, Password, IsAdmin, IsActive, Phone)
                VALUES ('Анна', '1', '1', 1, 1, NULL);
                INSERT INTO Users (FullName, Login, Password, IsAdmin, IsActive, Phone)
                VALUES ('Анастасия', '1', '1', 1, 1, NULL);
                INSERT INTO Users (FullName, Login, Password, IsAdmin, IsActive, Phone)
                VALUES ('переименуй меня', '1', '1', 0, 1, NULL);
            ", connection))
                        cmd.ExecuteNonQuery();
                }
            }

            // Проверяем, есть ли уже услуги
            using (var checkServices = new SQLiteCommand("SELECT COUNT(*) FROM Services", connection))
            {
                long servicesCount = (long)checkServices.ExecuteScalar();
                System.Diagnostics.Debug.WriteLine($"SeedDefaultData: найдено услуг: {servicesCount}");

                if (servicesCount > 0)
                {
                    System.Diagnostics.Debug.WriteLine("Услуги уже есть, пропускаем добавление");
                    return;
                }
            }

            System.Diagnostics.Debug.WriteLine("=== Добавляем начальные услуги ===");

            var servicesData = new List<ServiceSeed>();
            servicesData.Add(new ServiceSeed
            {
                Name = "Техническая мойка",
                Duration = 30,
                Description = "Двухфазная мойка без сушки, коврики",
                Prices = new List<PriceSeed> {
            new PriceSeed { Cat = 1, Price = 700m },
            new PriceSeed { Cat = 2, Price = 750m },
            new PriceSeed { Cat = 3, Price = 850m },
            new PriceSeed { Cat = 4, Price = 900m }
        }
            });
            servicesData.Add(new ServiceSeed
            {
                Name = "Профессиональная мойка кузова",
                Duration = 45,
                Description = "Двухфазная мойка, воск, турбосушка, коврики, педальный блок",
                Prices = new List<PriceSeed> {
            new PriceSeed { Cat = 1, Price = 1000m },
            new PriceSeed { Cat = 2, Price = 1200m },
            new PriceSeed { Cat = 3, Price = 1400m },
            new PriceSeed { Cat = 4, Price = 1700m }
        }
            });
            servicesData.Add(new ServiceSeed
            {
                Name = "Комплекс \"ИЗИ\"",
                Duration = 60,
                Description = "Трехфазная мойка, влажная уборка, пылесос, стекла, турбосушка, коврики, педальный блок, чернение",
                Prices = new List<PriceSeed> {
            new PriceSeed { Cat = 1, Price = 1900m },
            new PriceSeed { Cat = 2, Price = 2100m },
            new PriceSeed { Cat = 3, Price = 2400m },
            new PriceSeed { Cat = 4, Price = 2700m }
        }
            });
            servicesData.Add(new ServiceSeed
            {
                Name = "Комплекс \"Глянец\"",
                Duration = 90,
                Description = "Двухфазная мойка, кварц, влажная уборка, пылесос, стекла, багажник, полироль, турбосушка, коврики, педальный блок, чернение",
                Prices = new List<PriceSeed> {
            new PriceSeed { Cat = 1, Price = 2900m },
            new PriceSeed { Cat = 2, Price = 3100m },
            new PriceSeed { Cat = 3, Price = 3400m },
            new PriceSeed { Cat = 4, Price = 3700m }
        }
            });

            foreach (var s in servicesData)
            {
                using (var ins = new SQLiteCommand(@"
            INSERT INTO Services (Name, DurationMinutes, Description, IsActive)
            VALUES (@name, @dur, @desc, 1);
            SELECT last_insert_rowid();
        ", connection))
                {
                    ins.Parameters.AddWithValue("@name", s.Name);
                    ins.Parameters.AddWithValue("@dur", s.Duration);
                    ins.Parameters.AddWithValue("@desc", s.Description);
                    long serviceId = (long)ins.ExecuteScalar();
                    System.Diagnostics.Debug.WriteLine($"Добавлена услуга: ID={serviceId}, Name={s.Name}");

                    foreach (var p in s.Prices)
                    {
                        using (var priceCmd = new SQLiteCommand(@"
                    INSERT INTO ServicePrices (ServiceId, BodyTypeCategory, Price)
                    VALUES (@sid, @cat, @price);
                ", connection))
                        {
                            priceCmd.Parameters.AddWithValue("@sid", serviceId);
                            priceCmd.Parameters.AddWithValue("@cat", p.Cat);
                            priceCmd.Parameters.AddWithValue("@price", p.Price);
                            priceCmd.ExecuteNonQuery();
                            System.Diagnostics.Debug.WriteLine($"  Цена: Cat={p.Cat}, Price={p.Price}");
                        }
                    }
                }
            }
        }


        

        // ---- Users ----
        public List<User> GetAllUsers() => GetAllUsersIncludingInactive().Where(u => u.IsActive).ToList();
        public List<User> GetAllUsersIncludingInactive()
        {
            var users = new List<User>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT Id, FullName, Login, Password, IsAdmin, IsActive, Phone FROM Users";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new User
                        {
                            Id = reader.GetInt32(0),
                            FullName = reader.GetString(1),
                            Login = reader.GetString(2),
                            Password = reader.GetString(3),
                            IsAdmin = reader.GetBoolean(4),
                            IsActive = reader.GetBoolean(5),
                            Phone = reader.IsDBNull(6) ? null : reader.GetString(6)
                        });
                    }
                }
            }
            return users;
        }

        public User AuthenticateUser(string login, string password)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT Id, FullName, Login, Password, IsAdmin, IsActive, Phone FROM Users WHERE Login = @login AND Password = @password AND IsActive = 1";
                cmd.Parameters.AddWithValue("@login", login);
                cmd.Parameters.AddWithValue("@password", password);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new User
                        {
                            Id = reader.GetInt32(0),
                            FullName = reader.GetString(1),
                            Login = reader.GetString(2),
                            Password = reader.GetString(3),
                            IsAdmin = reader.GetBoolean(4),
                            IsActive = reader.GetBoolean(5),
                            Phone = reader.IsDBNull(6) ? null : reader.GetString(6)
                        };
                    }
                }
            }
            return null;
        }

        public void AddUser(User user)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Users (FullName, Login, Password, IsAdmin, IsActive, Phone)
                    VALUES (@fullname, @login, @pwd, @isAdmin, 1, @phone);
                    SELECT last_insert_rowid();
                ";
                cmd.Parameters.AddWithValue("@fullname", user.FullName);
                cmd.Parameters.AddWithValue("@login", user.Login);
                cmd.Parameters.AddWithValue("@pwd", user.Password);
                cmd.Parameters.AddWithValue("@isAdmin", user.IsAdmin ? 1 : 0);
                cmd.Parameters.AddWithValue("@phone", user.Phone ?? (object)DBNull.Value);
                user.Id = (int)(long)cmd.ExecuteScalar();
            }
        }

        public void UpdateUser(User user)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    UPDATE Users 
                    SET FullName = @fullname, Login = @login, Password = @pwd, IsAdmin = @isAdmin, IsActive = @isActive, Phone = @phone
                    WHERE Id = @id";
                cmd.Parameters.AddWithValue("@fullname", user.FullName);
                cmd.Parameters.AddWithValue("@login", user.Login);
                cmd.Parameters.AddWithValue("@pwd", user.Password);
                cmd.Parameters.AddWithValue("@isAdmin", user.IsAdmin ? 1 : 0);
                cmd.Parameters.AddWithValue("@isActive", user.IsActive ? 1 : 0);
                cmd.Parameters.AddWithValue("@phone", user.Phone ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@id", user.Id);
                cmd.ExecuteNonQuery();
            }
        }

        // ---- Services ----
        public List<Service> GetAllServices()
        {
            var services = new List<Service>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT Id, Name, DurationMinutes, Description, IsActive FROM Services WHERE IsActive = 1";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var s = new Service
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            DurationMinutes = reader.GetInt32(2),
                            Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                            IsActive = reader.GetBoolean(4),
                            PriceByBodyType = new Dictionary<int, decimal>()
                        };
                        services.Add(s);
                    }
                }
                foreach (var s in services)
                {
                    var priceCmd = connection.CreateCommand();
                    priceCmd.CommandText = "SELECT BodyTypeCategory, Price FROM ServicePrices WHERE ServiceId = @id";
                    priceCmd.Parameters.AddWithValue("@id", s.Id);
                    using (var pr = priceCmd.ExecuteReader())
                    {
                        while (pr.Read())
                            s.PriceByBodyType[pr.GetInt32(0)] = (decimal)pr.GetDouble(1);
                    }
                }
            }
            return services;
        }

        public void AddService(Service service)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var cmd = connection.CreateCommand();
                    cmd.CommandText = @"
                        INSERT INTO Services (Name, DurationMinutes, Description, IsActive)
                        VALUES (@name, @dur, @desc, 1);
                        SELECT last_insert_rowid();
                    ";
                    cmd.Parameters.AddWithValue("@name", service.Name);
                    cmd.Parameters.AddWithValue("@dur", service.DurationMinutes);
                    cmd.Parameters.AddWithValue("@desc", service.Description ?? (object)DBNull.Value);
                    service.Id = (int)(long)cmd.ExecuteScalar();
                    foreach (var kv in service.PriceByBodyType)
                    {
                        var priceCmd = connection.CreateCommand();
                        priceCmd.CommandText = "INSERT INTO ServicePrices (ServiceId, BodyTypeCategory, Price) VALUES (@sid, @cat, @price)";
                        priceCmd.Parameters.AddWithValue("@sid", service.Id);
                        priceCmd.Parameters.AddWithValue("@cat", kv.Key);
                        priceCmd.Parameters.AddWithValue("@price", kv.Value);
                        priceCmd.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }
            }
        }

        public void UpdateService(Service service)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var cmd = connection.CreateCommand();
                    cmd.CommandText = "UPDATE Services SET Name = @name, DurationMinutes = @dur, Description = @desc, IsActive = @active WHERE Id = @id";
                    cmd.Parameters.AddWithValue("@name", service.Name);
                    cmd.Parameters.AddWithValue("@dur", service.DurationMinutes);
                    cmd.Parameters.AddWithValue("@desc", service.Description ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@active", service.IsActive ? 1 : 0);
                    cmd.Parameters.AddWithValue("@id", service.Id);
                    cmd.ExecuteNonQuery();

                    var delCmd = connection.CreateCommand();
                    delCmd.CommandText = "DELETE FROM ServicePrices WHERE ServiceId = @id";
                    delCmd.Parameters.AddWithValue("@id", service.Id);
                    delCmd.ExecuteNonQuery();
                    foreach (var kv in service.PriceByBodyType)
                    {
                        var insCmd = connection.CreateCommand();
                        insCmd.CommandText = "INSERT INTO ServicePrices (ServiceId, BodyTypeCategory, Price) VALUES (@sid, @cat, @price)";
                        insCmd.Parameters.AddWithValue("@sid", service.Id);
                        insCmd.Parameters.AddWithValue("@cat", kv.Key);
                        insCmd.Parameters.AddWithValue("@price", kv.Value);
                        insCmd.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }
            }
        }

        public void DeleteService(int serviceId)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "DELETE FROM Services WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", serviceId);
                cmd.ExecuteNonQuery();
            }
        }

        // ---- Shifts ----
        public Shift GetCurrentOpenShift()
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                // Отладочный вывод
                var countCmd = connection.CreateCommand();
                countCmd.CommandText = "SELECT COUNT(*) FROM Shifts WHERE IsClosed = 0";
                long openShiftsCount = (long)countCmd.ExecuteScalar();
                System.Diagnostics.Debug.WriteLine($"GetCurrentOpenShift: найдено открытых смен всего: {openShiftsCount}");

                // Выведем все открытые смены для отладки
                var listCmd = connection.CreateCommand();
                listCmd.CommandText = "SELECT Id, Date FROM Shifts WHERE IsClosed = 0";
                using (var listReader = listCmd.ExecuteReader())
                {
                    while (listReader.Read())
                    {
                        System.Diagnostics.Debug.WriteLine($"  Открытая смена: ID={listReader.GetInt32(0)}, Date={listReader.GetString(1)}");
                    }
                }

                var cmd = connection.CreateCommand();
                // Используем DateTime.Now.Date.ToString() для сравнения
                cmd.CommandText = @"
            SELECT Id, Date, StartTime, EndTime, IsClosed, Notes 
            FROM Shifts 
            WHERE IsClosed = 0 AND Date = @today
            ORDER BY Id DESC
            LIMIT 1";
                cmd.Parameters.AddWithValue("@today", DateTime.Now.ToString("yyyy-MM-dd"));

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        System.Diagnostics.Debug.WriteLine("GetCurrentOpenShift: открытых смен на сегодня нет");
                        return null;
                    }

                    System.Diagnostics.Debug.WriteLine($"GetCurrentOpenShift: найдена смена ID={reader.GetInt32(0)}");

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
                    using (var empR = empCmd.ExecuteReader())
                    {
                        while (empR.Read())
                        {
                            shift.EmployeeIds.Add(empR.GetInt32(0));
                        }
                    }
                    System.Diagnostics.Debug.WriteLine($"GetCurrentOpenShift: загружено сотрудников: {shift.EmployeeIds.Count}");

                    // Загружаем заказы смены
                    var ordCmd = connection.CreateCommand();
                    ordCmd.CommandText = @"
                SELECT Id, ShiftId, ClientId, CarModel, CarNumber, BodyTypeCategory, Time, BoxNumber, WasherId,
                       PaymentMethod, TotalPrice, ExtraCost, ExtraCostReason,
                       DiscountPercent, DiscountAmount, Notes, Status
                FROM Orders WHERE ShiftId = @id";
                    ordCmd.Parameters.AddWithValue("@id", shift.Id);
                    using (var ordR = ordCmd.ExecuteReader())
                    {
                        while (ordR.Read())
                        {
                            var order = new CarWashOrder
                            {
                                Id = ordR.GetInt32(0),
                                ShiftId = ordR.GetInt32(1),
                                ClientId = ordR.IsDBNull(2) ? (int?)null : ordR.GetInt32(2),
                                CarModel = ordR.GetString(3),
                                CarNumber = ordR.GetString(4),
                                BodyTypeCategory = ordR.GetInt32(5),
                                Time = DateTime.Parse(ordR.GetString(6)),
                                BoxNumber = ordR.GetInt32(7),
                                WasherId = ordR.GetInt32(8),
                                PaymentMethod = ordR.GetString(9),
                                TotalPrice = (decimal)ordR.GetDouble(10),
                                ExtraCost = (decimal)ordR.GetDouble(11),
                                ExtraCostReason = ordR.IsDBNull(12) ? null : ordR.GetString(12),
                                DiscountPercent = (decimal)ordR.GetDouble(13),
                                DiscountAmount = (decimal)ordR.GetDouble(14),
                                Notes = ordR.IsDBNull(15) ? null : ordR.GetString(15),
                                Status = ordR.GetString(16),
                                ServiceIds = new List<int>()
                            };

                            // Загружаем услуги заказа
                            var svcCmd = connection.CreateCommand();
                            svcCmd.CommandText = "SELECT ServiceId FROM OrderServices WHERE OrderId = @oid";
                            svcCmd.Parameters.AddWithValue("@oid", order.Id);
                            using (var svcR = svcCmd.ExecuteReader())
                            {
                                while (svcR.Read())
                                {
                                    order.ServiceIds.Add(svcR.GetInt32(0));
                                }
                            }
                            shift.Orders.Add(order);
                        }
                    }
                    System.Diagnostics.Debug.WriteLine($"GetCurrentOpenShift: загружено заказов: {shift.Orders.Count}");

                    return shift;
                }
            }
        }

        public void StartShift(Shift shift)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var trans = connection.BeginTransaction())
                {
                    // 1. Проверяем, есть ли уже смена за сегодня
                    string todayStr = DateTime.Now.ToString("yyyy-MM-dd");
                    var checkCmd = connection.CreateCommand();
                    checkCmd.CommandText = "SELECT Id, IsClosed FROM Shifts WHERE date(Date) = @today LIMIT 1";
                    checkCmd.Parameters.AddWithValue("@today", todayStr);

                    int? existingShiftId = null;
                    bool isClosed = false;

                    using (var reader = checkCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            existingShiftId = reader.GetInt32(0);
                            isClosed = reader.GetInt32(1) == 1;
                        }
                    }

                    if (existingShiftId.HasValue)
                    {
                        // 2А. СМЕНА НАЙДЕНА: Возобновляем её
                        shift.Id = existingShiftId.Value;

                        if (isClosed)
                        {
                            var updateCmd = connection.CreateCommand();
                            updateCmd.CommandText = "UPDATE Shifts SET IsClosed = 0, EndTime = NULL, Notes = @notes WHERE Id = @id";
                            updateCmd.Parameters.AddWithValue("@id", existingShiftId.Value);
                            updateCmd.Parameters.AddWithValue("@notes", shift.Notes ?? "");
                            updateCmd.ExecuteNonQuery();
                        }

                        // Так как состав мойщиков мог поменяться, удаляем старые записи для этой смены
                        var delCmd = connection.CreateCommand();
                        delCmd.CommandText = "DELETE FROM ShiftEmployees WHERE ShiftId = @sid";
                        delCmd.Parameters.AddWithValue("@sid", shift.Id);
                        delCmd.ExecuteNonQuery();

                        System.Diagnostics.Debug.WriteLine($"StartShift: Возобновлена смена ID={shift.Id}, Date={todayStr}");
                    }
                    else
                    {
                        // 2Б. СМЕНЫ НЕТ: Создаем новую (твой оригинальный код)
                        var insertCmd = connection.CreateCommand();
                        insertCmd.CommandText = @"
                    INSERT INTO Shifts (Date, StartTime, IsClosed, Notes)
                    VALUES (@date, @start, 0, @notes);
                    SELECT last_insert_rowid();";
                        insertCmd.Parameters.AddWithValue("@date", todayStr);
                        insertCmd.Parameters.AddWithValue("@start", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        insertCmd.Parameters.AddWithValue("@notes", shift.Notes ?? "");
                        shift.Id = (int)(long)insertCmd.ExecuteScalar();

                        System.Diagnostics.Debug.WriteLine($"StartShift: Создана новая смена ID={shift.Id}, Date={todayStr}");
                    }

                    // 3. Записываем актуальных мойщиков (работает и для новой, и для возобновленной смены)
                    foreach (int eid in shift.EmployeeIds)
                    {
                        var eCmd = connection.CreateCommand();
                        eCmd.CommandText = "INSERT INTO ShiftEmployees (ShiftId, EmployeeId) VALUES (@sid, @eid)";
                        eCmd.Parameters.AddWithValue("@sid", shift.Id);
                        eCmd.Parameters.AddWithValue("@eid", eid);
                        eCmd.ExecuteNonQuery();
                    }

                    trans.Commit();
                }
            }

            // Уведомляем систему, что данные обновились
            NotifyDataChanged();
        }

        public void CloseShift(int shiftId, string notes)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
            UPDATE Shifts 
            SET EndTime = @end, IsClosed = 1, Notes = @notes 
            WHERE Id = @id";
                cmd.Parameters.AddWithValue("@end", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@notes", notes ?? "");
                cmd.Parameters.AddWithValue("@id", shiftId);

                int rowsAffected = cmd.ExecuteNonQuery();
                System.Diagnostics.Debug.WriteLine($"CloseShift: смена ID={shiftId}, rowsAffected={rowsAffected}");

                if (rowsAffected == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"CloseShift: ВНИМАНИЕ! Смена ID={shiftId} не найдена!");
                }
            }
        }

        public Shift GetShiftById(int shiftId)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT Id, Date, StartTime, EndTime, IsClosed, Notes FROM Shifts WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", shiftId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return null;
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
                    var empCmd = connection.CreateCommand();
                    empCmd.CommandText = "SELECT EmployeeId FROM ShiftEmployees WHERE ShiftId = @id";
                    empCmd.Parameters.AddWithValue("@id", shift.Id);
                    using (var empR = empCmd.ExecuteReader())
                    {
                        while (empR.Read()) shift.EmployeeIds.Add(empR.GetInt32(0));
                    }
                    return shift;
                }
            }
        }

        public List<Shift> GetAllShifts()
        {
            var shifts = new List<Shift>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT Id, Date, StartTime, EndTime, IsClosed, Notes FROM Shifts ORDER BY Date DESC";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
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
                        var empCmd = connection.CreateCommand();
                        empCmd.CommandText = "SELECT EmployeeId FROM ShiftEmployees WHERE ShiftId = @id";
                        empCmd.Parameters.AddWithValue("@id", shift.Id);
                        using (var empR = empCmd.ExecuteReader())
                        {
                            while (empR.Read()) shift.EmployeeIds.Add(empR.GetInt32(0));
                        }
                        shifts.Add(shift);
                    }
                }
            }
            return shifts;
        }

        // ---- Orders ----
        public void AddOrder(CarWashOrder order, List<int> serviceIds)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var trans = connection.BeginTransaction())
                {
                    var cmd = connection.CreateCommand();
                    cmd.CommandText = @"
                        INSERT INTO Orders (
                            ShiftId, ClientId, CarModel, CarNumber, BodyTypeCategory, Time, BoxNumber, WasherId,
                            PaymentMethod, TotalPrice, ExtraCost, ExtraCostReason,
                            DiscountPercent, DiscountAmount, Notes, Status
                        ) VALUES (
                            @sid, @cid, @carModel, @carNumber, @bodyCat, @time, @box, @washer,
                            @payMethod, @totalPrice, @extraCost, @extraReason,
                            @discPct, @discAmt, @notes, @status
                        );
                        SELECT last_insert_rowid();
                    ";
                    cmd.Parameters.AddWithValue("@sid", order.ShiftId);
                    cmd.Parameters.AddWithValue("@cid", order.ClientId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@carModel", order.CarModel);
                    cmd.Parameters.AddWithValue("@carNumber", order.CarNumber);
                    cmd.Parameters.AddWithValue("@bodyCat", order.BodyTypeCategory);
                    cmd.Parameters.AddWithValue("@time", order.Time.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@box", order.BoxNumber);
                    cmd.Parameters.AddWithValue("@washer", order.WasherId);
                    cmd.Parameters.AddWithValue("@payMethod", order.PaymentMethod);
                    cmd.Parameters.AddWithValue("@totalPrice", order.TotalPrice);
                    cmd.Parameters.AddWithValue("@extraCost", order.ExtraCost);
                    cmd.Parameters.AddWithValue("@extraReason", order.ExtraCostReason ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@discPct", order.DiscountPercent);
                    cmd.Parameters.AddWithValue("@discAmt", order.DiscountAmount);
                    cmd.Parameters.AddWithValue("@notes", order.Notes ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@status", order.Status);
                    order.Id = (int)(long)cmd.ExecuteScalar();

                    foreach (int sid in serviceIds)
                    {
                        var svcCmd = connection.CreateCommand();
                        svcCmd.CommandText = "INSERT INTO OrderServices (OrderId, ServiceId) VALUES (@oid, @sid)";
                        svcCmd.Parameters.AddWithValue("@oid", order.Id);
                        svcCmd.Parameters.AddWithValue("@sid", sid);
                        svcCmd.ExecuteNonQuery();
                    }
                    trans.Commit();
                }
            }
        }

        public void UpdateOrder(CarWashOrder order)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var trans = connection.BeginTransaction())
                {
                    var cmd = connection.CreateCommand();
                    cmd.CommandText = @"
                        UPDATE Orders SET
                            ClientId = @cid, CarModel = @carModel, CarNumber = @carNumber,
                            BodyTypeCategory = @bodyCat, Time = @time, BoxNumber = @box,
                            WasherId = @washer, PaymentMethod = @payMethod,
                            TotalPrice = @totalPrice, ExtraCost = @extraCost,
                            ExtraCostReason = @extraReason, DiscountPercent = @discPct,
                            DiscountAmount = @discAmt, Notes = @notes, Status = @status
                        WHERE Id = @id
                    ";
                    cmd.Parameters.AddWithValue("@cid", order.ClientId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@carModel", order.CarModel);
                    cmd.Parameters.AddWithValue("@carNumber", order.CarNumber);
                    cmd.Parameters.AddWithValue("@bodyCat", order.BodyTypeCategory);
                    cmd.Parameters.AddWithValue("@time", order.Time.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@box", order.BoxNumber);
                    cmd.Parameters.AddWithValue("@washer", order.WasherId);
                    cmd.Parameters.AddWithValue("@payMethod", order.PaymentMethod);
                    cmd.Parameters.AddWithValue("@totalPrice", order.TotalPrice);
                    cmd.Parameters.AddWithValue("@extraCost", order.ExtraCost);
                    cmd.Parameters.AddWithValue("@extraReason", order.ExtraCostReason ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@discPct", order.DiscountPercent);
                    cmd.Parameters.AddWithValue("@discAmt", order.DiscountAmount);
                    cmd.Parameters.AddWithValue("@notes", order.Notes ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@status", order.Status);
                    cmd.Parameters.AddWithValue("@id", order.Id);
                    cmd.ExecuteNonQuery();

                    var delSvc = connection.CreateCommand();
                    delSvc.CommandText = "DELETE FROM OrderServices WHERE OrderId = @oid";
                    delSvc.Parameters.AddWithValue("@oid", order.Id);
                    delSvc.ExecuteNonQuery();
                    foreach (int sid in order.ServiceIds)
                    {
                        var insSvc = connection.CreateCommand();
                        insSvc.CommandText = "INSERT INTO OrderServices (OrderId, ServiceId) VALUES (@oid, @sid)";
                        insSvc.Parameters.AddWithValue("@oid", order.Id);
                        insSvc.Parameters.AddWithValue("@sid", sid);
                        insSvc.ExecuteNonQuery();
                    }
                    trans.Commit();
                }
            }
        }

        public CarWashOrder GetOrderById(int orderId)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT Id, ShiftId, ClientId, CarModel, CarNumber, BodyTypeCategory, Time, BoxNumber, WasherId,
                           PaymentMethod, TotalPrice, ExtraCost, ExtraCostReason,
                           DiscountPercent, DiscountAmount, Notes, Status
                    FROM Orders WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", orderId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return null;
                    var order = new CarWashOrder
                    {
                        Id = reader.GetInt32(0),
                        ShiftId = reader.GetInt32(1),
                        ClientId = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                        CarModel = reader.GetString(3),
                        CarNumber = reader.GetString(4),
                        BodyTypeCategory = reader.GetInt32(5),
                        Time = DateTime.Parse(reader.GetString(6)),
                        BoxNumber = reader.GetInt32(7),
                        WasherId = reader.GetInt32(8),
                        PaymentMethod = reader.GetString(9),
                        TotalPrice = (decimal)reader.GetDouble(10),
                        ExtraCost = (decimal)reader.GetDouble(11),
                        ExtraCostReason = reader.IsDBNull(12) ? null : reader.GetString(12),
                        DiscountPercent = (decimal)reader.GetDouble(13),
                        DiscountAmount = (decimal)reader.GetDouble(14),
                        Notes = reader.IsDBNull(15) ? null : reader.GetString(15),
                        Status = reader.GetString(16),
                        ServiceIds = new List<int>()
                    };
                    var svcCmd = connection.CreateCommand();
                    svcCmd.CommandText = "SELECT ServiceId FROM OrderServices WHERE OrderId = @oid";
                    svcCmd.Parameters.AddWithValue("@oid", order.Id);
                    using (var svcR = svcCmd.ExecuteReader())
                    {
                        while (svcR.Read()) order.ServiceIds.Add(svcR.GetInt32(0));
                    }
                    return order;
                }
            }
        }

        public void UpdateOrderStatus(int orderId, string status)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "UPDATE Orders SET Status = @status WHERE Id = @id";
                cmd.Parameters.AddWithValue("@status", status);
                cmd.Parameters.AddWithValue("@id", orderId);
                cmd.ExecuteNonQuery();
            }
        }

        public List<CarWashOrder> GetOrdersByShiftId(int shiftId)
        {
            var orders = new List<CarWashOrder>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT Id, ShiftId, ClientId, CarModel, CarNumber, BodyTypeCategory, Time, BoxNumber, WasherId,
                           PaymentMethod, TotalPrice, ExtraCost, ExtraCostReason,
                           DiscountPercent, DiscountAmount, Notes, Status
                    FROM Orders WHERE ShiftId = @sid";
                cmd.Parameters.AddWithValue("@sid", shiftId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var order = new CarWashOrder
                        {
                            Id = reader.GetInt32(0),
                            ShiftId = reader.GetInt32(1),
                            ClientId = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                            CarModel = reader.GetString(3),
                            CarNumber = reader.GetString(4),
                            BodyTypeCategory = reader.GetInt32(5),
                            Time = DateTime.Parse(reader.GetString(6)),
                            BoxNumber = reader.GetInt32(7),
                            WasherId = reader.GetInt32(8),
                            PaymentMethod = reader.GetString(9),
                            TotalPrice = (decimal)reader.GetDouble(10),
                            ExtraCost = (decimal)reader.GetDouble(11),
                            ExtraCostReason = reader.IsDBNull(12) ? null : reader.GetString(12),
                            DiscountPercent = (decimal)reader.GetDouble(13),
                            DiscountAmount = (decimal)reader.GetDouble(14),
                            Notes = reader.IsDBNull(15) ? null : reader.GetString(15),
                            Status = reader.GetString(16),
                            ServiceIds = new List<int>()
                        };

                        var svcCmd = connection.CreateCommand();
                        svcCmd.CommandText = "SELECT ServiceId FROM OrderServices WHERE OrderId = @oid";
                        svcCmd.Parameters.AddWithValue("@oid", order.Id);
                        using (var svcR = svcCmd.ExecuteReader())
                        {
                            while (svcR.Read()) order.ServiceIds.Add(svcR.GetInt32(0));
                        }
                        orders.Add(order);
                    }
                }
            }
            return orders;
        }

        // ---- Clients ----
        public List<Client> GetAllClients()
        {
            var clients = new List<Client>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT Id, FullName, Phone, CarModel, CarNumber, DefaultDiscountPercent,
                           RegistrationDate, LastVisitDate, TotalSpent, VisitsCount, Notes
                    FROM Clients ORDER BY FullName";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        clients.Add(new Client
                        {
                            Id = reader.GetInt32(0),
                            FullName = reader.GetString(1),
                            Phone = reader.IsDBNull(2) ? null : reader.GetString(2),
                            CarModel = reader.IsDBNull(3) ? null : reader.GetString(3),
                            CarNumber = reader.IsDBNull(4) ? null : reader.GetString(4),
                            DefaultDiscountPercent = (decimal)reader.GetDouble(5),
                            RegistrationDate = DateTime.Parse(reader.GetString(6)),
                            LastVisitDate = reader.IsDBNull(7) ? (DateTime?)null : DateTime.Parse(reader.GetString(7)),
                            TotalSpent = (decimal)reader.GetDouble(8),
                            VisitsCount = reader.GetInt32(9),
                            Notes = reader.IsDBNull(10) ? null : reader.GetString(10)
                        });
                    }
                }
            }
            return clients;
        }

        public Client GetClientById(int id)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT Id, FullName, Phone, CarModel, CarNumber, DefaultDiscountPercent,
                           RegistrationDate, LastVisitDate, TotalSpent, VisitsCount, Notes
                    FROM Clients WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", id);
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return null;
                    return new Client
                    {
                        Id = reader.GetInt32(0),
                        FullName = reader.GetString(1),
                        Phone = reader.IsDBNull(2) ? null : reader.GetString(2),
                        CarModel = reader.IsDBNull(3) ? null : reader.GetString(3),
                        CarNumber = reader.IsDBNull(4) ? null : reader.GetString(4),
                        DefaultDiscountPercent = (decimal)reader.GetDouble(5),
                        RegistrationDate = DateTime.Parse(reader.GetString(6)),
                        LastVisitDate = reader.IsDBNull(7) ? (DateTime?)null : DateTime.Parse(reader.GetString(7)),
                        TotalSpent = (decimal)reader.GetDouble(8),
                        VisitsCount = reader.GetInt32(9),
                        Notes = reader.IsDBNull(10) ? null : reader.GetString(10)
                    };
                }
            }
        }

        public void AddClient(Client client)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Clients (FullName, Phone, CarModel, CarNumber, DefaultDiscountPercent,
                                         RegistrationDate, LastVisitDate, TotalSpent, VisitsCount, Notes)
                    VALUES (@name, @phone, @carModel, @carNumber, @discount,
                            @regDate, @lastVisit, @total, @visits, @notes);
                    SELECT last_insert_rowid();
                ";
                cmd.Parameters.AddWithValue("@name", client.FullName);
                cmd.Parameters.AddWithValue("@phone", client.Phone ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@carModel", client.CarModel ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@carNumber", client.CarNumber ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@discount", client.DefaultDiscountPercent);
                cmd.Parameters.AddWithValue("@regDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@lastVisit", client.LastVisitDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@total", client.TotalSpent);
                cmd.Parameters.AddWithValue("@visits", client.VisitsCount);
                cmd.Parameters.AddWithValue("@notes", client.Notes ?? (object)DBNull.Value);
                client.Id = (int)(long)cmd.ExecuteScalar();
            }
        }

        public void UpdateClient(Client client)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    UPDATE Clients SET
                        FullName = @name, Phone = @phone, CarModel = @carModel, CarNumber = @carNumber,
                        DefaultDiscountPercent = @discount, RegistrationDate = @regDate,
                        LastVisitDate = @lastVisit, TotalSpent = @total, VisitsCount = @visits, Notes = @notes
                    WHERE Id = @id";
                cmd.Parameters.AddWithValue("@name", client.FullName);
                cmd.Parameters.AddWithValue("@phone", client.Phone ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@carModel", client.CarModel ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@carNumber", client.CarNumber ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@discount", client.DefaultDiscountPercent);
                cmd.Parameters.AddWithValue("@regDate", client.RegistrationDate.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@lastVisit", client.LastVisitDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@total", client.TotalSpent);
                cmd.Parameters.AddWithValue("@visits", client.VisitsCount);
                cmd.Parameters.AddWithValue("@notes", client.Notes ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@id", client.Id);
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteClient(int clientId)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "DELETE FROM Clients WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", clientId);
                cmd.ExecuteNonQuery();
            }
        }

        public List<CarWashOrder> GetOrdersByClientId(int clientId)
        {
            var orders = new List<CarWashOrder>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT Id, ShiftId, ClientId, CarModel, CarNumber, BodyTypeCategory, Time, BoxNumber, WasherId,
                           PaymentMethod, TotalPrice, ExtraCost, ExtraCostReason,
                           DiscountPercent, DiscountAmount, Notes, Status
                    FROM Orders WHERE ClientId = @cid";
                cmd.Parameters.AddWithValue("@cid", clientId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var order = new CarWashOrder
                        {
                            Id = reader.GetInt32(0),
                            ShiftId = reader.GetInt32(1),
                            ClientId = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                            CarModel = reader.GetString(3),
                            CarNumber = reader.GetString(4),
                            BodyTypeCategory = reader.GetInt32(5),
                            Time = DateTime.Parse(reader.GetString(6)),
                            BoxNumber = reader.GetInt32(7),
                            WasherId = reader.GetInt32(8),
                            PaymentMethod = reader.GetString(9),
                            TotalPrice = (decimal)reader.GetDouble(10),
                            ExtraCost = (decimal)reader.GetDouble(11),
                            ExtraCostReason = reader.IsDBNull(12) ? null : reader.GetString(12),
                            DiscountPercent = (decimal)reader.GetDouble(13),
                            DiscountAmount = (decimal)reader.GetDouble(14),
                            Notes = reader.IsDBNull(15) ? null : reader.GetString(15),
                            Status = reader.GetString(16),
                            ServiceIds = new List<int>()
                        };
                        var svcCmd = connection.CreateCommand();
                        svcCmd.CommandText = "SELECT ServiceId FROM OrderServices WHERE OrderId = @oid";
                        svcCmd.Parameters.AddWithValue("@oid", order.Id);
                        using (var svcR = svcCmd.ExecuteReader())
                        {
                            while (svcR.Read()) order.ServiceIds.Add(svcR.GetInt32(0));
                        }
                        orders.Add(order);
                    }
                }
            }
            return orders;
        }

        // ---- Appointments ----
        public List<Appointment> GetAllAppointments()
        {
            var apps = new List<Appointment>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT Id, CarNumber, CarModel, CarBodyType, BodyTypeCategory,
                           AppointmentDate, DurationMinutes, BoxNumber, ExtraCost,
                           ExtraCostReason, Notes, IsCompleted, OrderId
                    FROM Appointments ORDER BY AppointmentDate";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var app = new Appointment
                        {
                            Id = reader.GetInt32(0),
                            CarNumber = reader.GetString(1),
                            CarModel = reader.GetString(2),
                            CarBodyType = reader.GetString(3),
                            BodyTypeCategory = reader.GetInt32(4),
                            AppointmentDate = DateTime.Parse(reader.GetString(5)),
                            DurationMinutes = reader.GetInt32(6),
                            BoxNumber = reader.GetInt32(7),
                            ExtraCost = (decimal)reader.GetDouble(8),
                            ExtraCostReason = reader.IsDBNull(9) ? null : reader.GetString(9),
                            Notes = reader.IsDBNull(10) ? null : reader.GetString(10),
                            IsCompleted = reader.GetBoolean(11),
                            OrderId = reader.IsDBNull(12) ? (int?)null : reader.GetInt32(12),
                            ServiceIds = new List<int>()
                        };
                        var svcCmd = connection.CreateCommand();
                        svcCmd.CommandText = "SELECT ServiceId FROM AppointmentServices WHERE AppointmentId = @aid";
                        svcCmd.Parameters.AddWithValue("@aid", app.Id);
                        using (var svcR = svcCmd.ExecuteReader())
                        {
                            while (svcR.Read()) app.ServiceIds.Add(svcR.GetInt32(0));
                        }
                        apps.Add(app);
                    }
                }
            }
            return apps;
        }

        public List<Appointment> GetAppointmentsByDate(DateTime date)
        {
            return GetAllAppointments().Where(a => a.AppointmentDate.Date == date.Date && !a.IsCompleted).OrderBy(a => a.AppointmentDate).ToList();
        }

        public Appointment GetAppointmentById(int id)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT Id, CarNumber, CarModel, CarBodyType, BodyTypeCategory,
                           AppointmentDate, DurationMinutes, BoxNumber, ExtraCost,
                           ExtraCostReason, Notes, IsCompleted, OrderId
                    FROM Appointments WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", id);
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return null;
                    var app = new Appointment
                    {
                        Id = reader.GetInt32(0),
                        CarNumber = reader.GetString(1),
                        CarModel = reader.GetString(2),
                        CarBodyType = reader.GetString(3),
                        BodyTypeCategory = reader.GetInt32(4),
                        AppointmentDate = DateTime.Parse(reader.GetString(5)),
                        DurationMinutes = reader.GetInt32(6),
                        BoxNumber = reader.GetInt32(7),
                        ExtraCost = (decimal)reader.GetDouble(8),
                        ExtraCostReason = reader.IsDBNull(9) ? null : reader.GetString(9),
                        Notes = reader.IsDBNull(10) ? null : reader.GetString(10),
                        IsCompleted = reader.GetBoolean(11),
                        OrderId = reader.IsDBNull(12) ? (int?)null : reader.GetInt32(12),
                        ServiceIds = new List<int>()
                    };
                    var svcCmd = connection.CreateCommand();
                    svcCmd.CommandText = "SELECT ServiceId FROM AppointmentServices WHERE AppointmentId = @aid";
                    svcCmd.Parameters.AddWithValue("@aid", app.Id);
                    using (var svcR = svcCmd.ExecuteReader())
                    {
                        while (svcR.Read()) app.ServiceIds.Add(svcR.GetInt32(0));
                    }
                    return app;
                }
            }
        }

        public void AddAppointment(Appointment appointment)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var trans = connection.BeginTransaction())
                {
                    var cmd = connection.CreateCommand();
                    cmd.CommandText = @"
                        INSERT INTO Appointments (
                            CarNumber, CarModel, CarBodyType, BodyTypeCategory,
                            AppointmentDate, DurationMinutes, BoxNumber, ExtraCost,
                            ExtraCostReason, Notes, IsCompleted, OrderId
                        ) VALUES (
                            @carNum, @carModel, @bodyType, @bodyCat,
                            @appDate, @dur, @box, @extraCost,
                            @extraReason, @notes, 0, NULL
                        );
                        SELECT last_insert_rowid();
                    ";
                    cmd.Parameters.AddWithValue("@carNum", appointment.CarNumber);
                    cmd.Parameters.AddWithValue("@carModel", appointment.CarModel);
                    cmd.Parameters.AddWithValue("@bodyType", appointment.CarBodyType ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@bodyCat", appointment.BodyTypeCategory);
                    cmd.Parameters.AddWithValue("@appDate", appointment.AppointmentDate.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@dur", appointment.DurationMinutes);
                    cmd.Parameters.AddWithValue("@box", appointment.BoxNumber);
                    cmd.Parameters.AddWithValue("@extraCost", appointment.ExtraCost);
                    cmd.Parameters.AddWithValue("@extraReason", appointment.ExtraCostReason ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@notes", appointment.Notes ?? (object)DBNull.Value);
                    appointment.Id = (int)(long)cmd.ExecuteScalar();
                    foreach (int sid in appointment.ServiceIds)
                    {
                        var svcCmd = connection.CreateCommand();
                        svcCmd.CommandText = "INSERT INTO AppointmentServices (AppointmentId, ServiceId) VALUES (@aid, @sid)";
                        svcCmd.Parameters.AddWithValue("@aid", appointment.Id);
                        svcCmd.Parameters.AddWithValue("@sid", sid);
                        svcCmd.ExecuteNonQuery();
                    }
                    trans.Commit();
                }
            }
        }

        public void UpdateAppointment(Appointment appointment)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var trans = connection.BeginTransaction())
                {
                    var cmd = connection.CreateCommand();
                    cmd.CommandText = @"
                        UPDATE Appointments SET
                            CarNumber = @carNum, CarModel = @carModel,
                            CarBodyType = @bodyType, BodyTypeCategory = @bodyCat,
                            AppointmentDate = @appDate, DurationMinutes = @dur,
                            BoxNumber = @box, ExtraCost = @extraCost,
                            ExtraCostReason = @extraReason, Notes = @notes,
                            IsCompleted = @completed, OrderId = @orderId
                        WHERE Id = @id";
                    cmd.Parameters.AddWithValue("@carNum", appointment.CarNumber);
                    cmd.Parameters.AddWithValue("@carModel", appointment.CarModel);
                    cmd.Parameters.AddWithValue("@bodyType", appointment.CarBodyType ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@bodyCat", appointment.BodyTypeCategory);
                    cmd.Parameters.AddWithValue("@appDate", appointment.AppointmentDate.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@dur", appointment.DurationMinutes);
                    cmd.Parameters.AddWithValue("@box", appointment.BoxNumber);
                    cmd.Parameters.AddWithValue("@extraCost", appointment.ExtraCost);
                    cmd.Parameters.AddWithValue("@extraReason", appointment.ExtraCostReason ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@notes", appointment.Notes ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@completed", appointment.IsCompleted ? 1 : 0);
                    cmd.Parameters.AddWithValue("@orderId", appointment.OrderId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@id", appointment.Id);
                    cmd.ExecuteNonQuery();

                    var delSvc = connection.CreateCommand();
                    delSvc.CommandText = "DELETE FROM AppointmentServices WHERE AppointmentId = @aid";
                    delSvc.Parameters.AddWithValue("@aid", appointment.Id);
                    delSvc.ExecuteNonQuery();
                    foreach (int sid in appointment.ServiceIds)
                    {
                        var insSvc = connection.CreateCommand();
                        insSvc.CommandText = "INSERT INTO AppointmentServices (AppointmentId, ServiceId) VALUES (@aid, @sid)";
                        insSvc.Parameters.AddWithValue("@aid", appointment.Id);
                        insSvc.Parameters.AddWithValue("@sid", sid);
                        insSvc.ExecuteNonQuery();
                    }
                    trans.Commit();
                }
            }
        }

        public void DeleteAppointment(int appointmentId)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "DELETE FROM Appointments WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", appointmentId);
                cmd.ExecuteNonQuery();
            }
        }

        public bool IsBoxAvailable(int boxNumber, DateTime startTime, int durationMinutes)
        {
            var endTime = startTime.AddMinutes(durationMinutes);
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT COUNT(*) FROM Appointments
                    WHERE BoxNumber = @box
                      AND IsCompleted = 0
                      AND AppointmentDate < @end
                      AND datetime(AppointmentDate, '+' || DurationMinutes || ' minutes') > @start
                ";
                cmd.Parameters.AddWithValue("@box", boxNumber);
                cmd.Parameters.AddWithValue("@start", startTime.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@end", endTime.ToString("yyyy-MM-dd HH:mm:ss"));
                long count = (long)cmd.ExecuteScalar();
                return count == 0;
            }
        }

        // ---- Schedules ----
        public List<EmployeeSchedule> GetSchedule(int year, int month)
        {
            var result = new List<EmployeeSchedule>();
            var users = GetAllUsersIncludingInactive();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                foreach (var u in users)
                {
                    var empSch = new EmployeeSchedule
                    {
                        EmployeeId = u.Id,
                        EmployeeName = u.FullName,
                        Position = u.IsAdmin ? "Администратор" : "Мойщик",
                        Days = new Dictionary<int, string>()
                    };
                    for (int day = 1; day <= DateTime.DaysInMonth(year, month); day++)
                    {
                        var cmd = connection.CreateCommand();
                        cmd.CommandText = "SELECT Status FROM EmployeeSchedules WHERE EmployeeId = @eid AND Year = @y AND Month = @m AND Day = @d";
                        cmd.Parameters.AddWithValue("@eid", u.Id);
                        cmd.Parameters.AddWithValue("@y", year);
                        cmd.Parameters.AddWithValue("@m", month);
                        cmd.Parameters.AddWithValue("@d", day);
                        var status = cmd.ExecuteScalar() as string;
                        empSch.Days[day] = status ?? "";
                    }
                    result.Add(empSch);
                }
            }
            return result;
        }

        public void SaveSchedule(int year, int month, List<EmployeeSchedule> scheduleData)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var trans = connection.BeginTransaction())
                {
                    var delCmd = connection.CreateCommand();
                    delCmd.CommandText = "DELETE FROM EmployeeSchedules WHERE Year = @y AND Month = @m";
                    delCmd.Parameters.AddWithValue("@y", year);
                    delCmd.Parameters.AddWithValue("@m", month);
                    delCmd.ExecuteNonQuery();
                    foreach (var emp in scheduleData)
                    {
                        foreach (var kv in emp.Days)
                        {
                            if (string.IsNullOrEmpty(kv.Value)) continue;
                            var insCmd = connection.CreateCommand();
                            insCmd.CommandText = @"
                                INSERT INTO EmployeeSchedules (EmployeeId, Year, Month, Day, Status)
                                VALUES (@eid, @y, @m, @d, @status)";
                            insCmd.Parameters.AddWithValue("@eid", emp.EmployeeId);
                            insCmd.Parameters.AddWithValue("@y", year);
                            insCmd.Parameters.AddWithValue("@m", month);
                            insCmd.Parameters.AddWithValue("@d", kv.Key);
                            insCmd.Parameters.AddWithValue("@status", kv.Value);
                            insCmd.ExecuteNonQuery();
                        }
                    }
                    trans.Commit();
                }
            }
        }

        // ---- Additional helpers ----
        public string CanCloseShift(int shiftId)
        {
            var shift = GetShiftById(shiftId);
            if (shift == null) return "Смена не найдена";
            if (shift.IsClosed) return "Смена уже закрыта";
            var inProgress = shift.Orders?.Any(o => o.Status == "Выполняется") ?? false;
            if (inProgress) return "Нельзя закрыть смену! Есть заказы со статусом 'Выполняется'.";
            var todayApps = GetAppointmentsByDate(DateTime.Now);
            if (todayApps.Any()) return "Нельзя закрыть смену! Есть активные предварительные записи на сегодня.";
            return null;
        }

        public void UpdateClientLoyalty(int clientId)
        {
            var orders = GetOrdersByClientId(clientId).Where(o => o.Status == "Выполнен").ToList();
            var client = GetClientById(clientId);
            if (client != null)
            {
                client.VisitsCount = orders.Count;
                client.TotalSpent = orders.Sum(o => o.FinalPrice);
                client.LastVisitDate = orders.Any() ? orders.Max(o => o.Time) : (DateTime?)null;
                UpdateClient(client);
            }
        }

        public void RecalculateAllClientsStats()
        {
            var clients = GetAllClients();
            foreach (var c in clients)
                UpdateClientLoyalty(c.Id);
        }

        public string ExportDataToJson(string exportType = "full")
        {
            // 1. Создаем структуру "на лету" без класса AppData
            var exportData = new
            {
                Users = GetAllUsersIncludingInactive(),
                Services = GetAllServices(),
                Clients = GetAllClients(),
                Appointments = GetAllAppointments(),
                Shifts = GetAllShifts()
            };

            // 2. Безопасный путь: сохраняем в "Мои документы\MyCarWashing\Exports"
            // Windows 100% разрешит сюда запись, и юзер легко найдет свои файлы
            string documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string exportsFolder = Path.Combine(documentsFolder, "MyCarWashing", "Exports");

            if (!Directory.Exists(exportsFolder))
            {
                Directory.CreateDirectory(exportsFolder);
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            string fileName = $"export_{exportType}_{timestamp}.json";
            string filePath = Path.Combine(exportsFolder, fileName);

            // 3. Конвертируем и сохраняем
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(exportData, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filePath, json);

            return filePath; // Возвращаем путь, чтобы интерфейс мог показать юзеру, где лежит файл
        }

        public static event Action DataChanged;
        public static void NotifyDataChanged() => DataChanged?.Invoke();

        // ---- Additional methods for compatibility ----
        public CarWashOrder ConvertAppointmentToOrder(Appointment appointment, int shiftId, int washerId)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var trans = connection.BeginTransaction())
                {
                    // Создаём заказ
                    var order = new CarWashOrder
                    {
                        CarModel = appointment.CarModel,
                        CarNumber = appointment.CarNumber,
                        CarBodyType = appointment.CarBodyType,
                        BodyTypeCategory = appointment.BodyTypeCategory,
                        Time = appointment.AppointmentDate,
                        ShiftId = shiftId,
                        BoxNumber = appointment.BoxNumber,
                        WasherId = washerId,
                        ServiceIds = new List<int>(appointment.ServiceIds),
                        ExtraCost = appointment.ExtraCost,
                        ExtraCostReason = appointment.ExtraCostReason,
                        Notes = appointment.Notes,
                        Status = "Выполняется",
                        PaymentMethod = "Наличные",
                        IsAppointment = true,
                        AppointmentId = appointment.Id
                    };

                    // Рассчитываем TotalPrice
                    var services = GetAllServices();
                    order.TotalPrice = appointment.ServiceIds.Sum(sid =>
                        services.FirstOrDefault(s => s.Id == sid)?.GetPrice(appointment.BodyTypeCategory) ?? 0);

                    // Добавляем заказ в БД
                    var cmd = connection.CreateCommand();
                    cmd.CommandText = @"
                INSERT INTO Orders (
                    ShiftId, ClientId, CarModel, CarNumber, BodyTypeCategory, Time, BoxNumber, WasherId,
                    PaymentMethod, TotalPrice, ExtraCost, ExtraCostReason,
                    DiscountPercent, DiscountAmount, Notes, Status
                ) VALUES (
                    @sid, @cid, @carModel, @carNumber, @bodyCat, @time, @box, @washer,
                    @payMethod, @totalPrice, @extraCost, @extraReason,
                    @discPct, @discAmt, @notes, @status
                );
                SELECT last_insert_rowid();
            ";
                    cmd.Parameters.AddWithValue("@sid", shiftId);
                    cmd.Parameters.AddWithValue("@cid", DBNull.Value);
                    cmd.Parameters.AddWithValue("@carModel", order.CarModel);
                    cmd.Parameters.AddWithValue("@carNumber", order.CarNumber);
                    cmd.Parameters.AddWithValue("@bodyCat", order.BodyTypeCategory);
                    cmd.Parameters.AddWithValue("@time", order.Time.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@box", order.BoxNumber);
                    cmd.Parameters.AddWithValue("@washer", order.WasherId);
                    cmd.Parameters.AddWithValue("@payMethod", order.PaymentMethod);
                    cmd.Parameters.AddWithValue("@totalPrice", order.TotalPrice);
                    cmd.Parameters.AddWithValue("@extraCost", order.ExtraCost);
                    cmd.Parameters.AddWithValue("@extraReason", order.ExtraCostReason ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@discPct", 0);
                    cmd.Parameters.AddWithValue("@discAmt", 0);
                    cmd.Parameters.AddWithValue("@notes", order.Notes ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@status", order.Status);
                    order.Id = (int)(long)cmd.ExecuteScalar();

                    // Добавляем услуги заказа
                    foreach (int sid in order.ServiceIds)
                    {
                        var svcCmd = connection.CreateCommand();
                        svcCmd.CommandText = "INSERT INTO OrderServices (OrderId, ServiceId) VALUES (@oid, @sid)";
                        svcCmd.Parameters.AddWithValue("@oid", order.Id);
                        svcCmd.Parameters.AddWithValue("@sid", sid);
                        svcCmd.ExecuteNonQuery();
                    }

                    // Отмечаем запись как выполненную
                    var updCmd = connection.CreateCommand();
                    updCmd.CommandText = "UPDATE Appointments SET IsCompleted = 1, OrderId = @oid WHERE Id = @aid";
                    updCmd.Parameters.AddWithValue("@oid", order.Id);
                    updCmd.Parameters.AddWithValue("@aid", appointment.Id);
                    updCmd.ExecuteNonQuery();

                    trans.Commit();
                    return order;
                }
            }
        }

        public CarWashOrder GetOrderByAppointmentId(int appointmentId)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT OrderId FROM Appointments WHERE Id = @aid";
                cmd.Parameters.AddWithValue("@aid", appointmentId);
                var orderId = cmd.ExecuteScalar();
                if (orderId == null || orderId == DBNull.Value)
                    return null;
                return GetOrderById(Convert.ToInt32(orderId));
            }
        }

        public bool IsShiftOpen(int shiftId)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT IsClosed FROM Shifts WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", shiftId);
                var result = cmd.ExecuteScalar();
                if (result == null) return false;
                return Convert.ToInt32(result) == 0;
            }
        }

        // ВРЕМЕННЫЙ МЕТОД ДЛЯ ПРОВЕРКИ НАЛИЧИЯ ДАННЫХ В ТАБЛИЦЕ УСЛУГ SERVICES
        public void CheckServices()
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM Services";
                long count = (long)cmd.ExecuteScalar();
                System.Diagnostics.Debug.WriteLine($"=== Services count: {count} ===");

                if (count > 0)
                {
                    var selectCmd = connection.CreateCommand();
                    selectCmd.CommandText = "SELECT Id, Name FROM Services";
                    using (var reader = selectCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            System.Diagnostics.Debug.WriteLine($"  Service: Id={reader.GetInt32(0)}, Name={reader.GetString(1)}");
                        }
                    }
                }
            }
        }
    }
}
