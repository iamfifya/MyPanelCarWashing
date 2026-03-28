using MyPanelCarWashing.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;

public static class FileDataService
{
    private static string dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appdata.json");

    public static void SaveData(AppData data)
    {
        try
        {
            // Проверка на дубликаты ID в услугах перед сохранением
            var duplicateIds = data.Services
                .GroupBy(s => s.Id)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateIds.Any())
            {
                System.Diagnostics.Debug.WriteLine($"Предупреждение: найдены дубликаты ID услуг: {string.Join(", ", duplicateIds)}");

                // Оставляем только уникальные услуги
                data.Services = data.Services
                    .GroupBy(s => s.Id)
                    .Select(g => g.First())
                    .OrderBy(s => s.Id)
                    .ToList();
            }

            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(dataPath, json);
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка сохранения данных: {ex.Message}");
        }
    }

    public static AppData LoadData()
    {
        if (!File.Exists(dataPath))
        {
            var defaultData = new AppData();

            // Добавляем тестового администратора
            defaultData.Users.Add(new User
            {
                Id = defaultData.GetNextUserId(),
                Login = "1",
                Password = "1",
                FullName = "Диркеторов Директор Директорович",
                IsAdmin = true
            });

            // Добавляем тестового сотрудника
            defaultData.Users.Add(new User
            {
                Id = defaultData.GetNextUserId(),
                Login = "worker",
                Password = "worker",
                FullName = "Иванов Иван",
                IsAdmin = false
            });

            // Добавляем услуги по умолчанию
            defaultData.Services.Add(new Service
            {
                Id = defaultData.GetNextServiceId(),
                Name = "Мойка кузова",
                Price = 500,
                DurationMinutes = 30,
                Description = "Стандартная мойка кузова",
                IsActive = true
            });

            defaultData.Services.Add(new Service
            {
                Id = defaultData.GetNextServiceId(),
                Name = "Химчистка салона",
                Price = 1500,
                DurationMinutes = 90,
                Description = "Глубокая химчистка салона",
                IsActive = true
            });

            defaultData.Services.Add(new Service
            {
                Id = defaultData.GetNextServiceId(),
                Name = "Полировка кузова",
                Price = 2000,
                DurationMinutes = 120,
                Description = "Полировка кузова",
                IsActive = true
            });

            SaveData(defaultData);
            return defaultData;
        }

        var json = File.ReadAllText(dataPath);
        return JsonConvert.DeserializeObject<AppData>(json);
    }
}
