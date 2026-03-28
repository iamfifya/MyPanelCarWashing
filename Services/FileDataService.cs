using MyPanelCarWashing.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
                FullName = "Анна",
                IsAdmin = true
            });

            defaultData.Users.Add(new User
            {
                Id = defaultData.GetNextUserId(),
                Login = "1",
                Password = "1",
                FullName = "Анастасия",
                IsAdmin = true
            });

            // Добавляем тестового сотрудника
            defaultData.Users.Add(new User
            {
                Id = defaultData.GetNextUserId(),
                Login = "1",
                Password = "1",
                FullName = "переименуй меня",
                IsAdmin = false
            });

            // Загружаем услуги из CSV
            LoadServicesFromCsv(defaultData);

            SaveData(defaultData);
            return defaultData;
        }

        var json = File.ReadAllText(dataPath);
        return JsonConvert.DeserializeObject<AppData>(json);
    }

    private static void LoadServicesFromCsv(AppData data)
    {
        // Основные услуги (Лист1)
        var mainServices = new Dictionary<string, Dictionary<int, decimal>>
        {
            { "Техническая мойка", new Dictionary<int, decimal> { { 1, 700 }, { 2, 750 }, { 3, 850 }, { 4, 900 } } },
            { "Профессиональная мойка кузова", new Dictionary<int, decimal> { { 1, 1100 }, { 2, 1300 }, { 3, 1500 }, { 4, 1800 } } },
            { "Комплекс \"ИЗИ\"", new Dictionary<int, decimal> { { 1, 1900 }, { 2, 2100 }, { 3, 2400 }, { 4, 2700 } } },
            { "Комплекс \"Глянец\"", new Dictionary<int, decimal> { { 1, 2900 }, { 2, 3100 }, { 3, 3400 }, { 4, 3700 } } }
        };

        // Дополнительные услуги (Лист2)
        var extraServices = new Dictionary<string, Dictionary<int, decimal>>
        {
            { "Багажник(цена от)", new Dictionary<int, decimal> { { 1, 300 } } },
            { "Пылесос(цена от)", new Dictionary<int, decimal> { { 1, 300 } } },
            { "Влажная уборка(цена от)", new Dictionary<int, decimal> { { 1, 300 } } },
            { "Стекла(цена от)", new Dictionary<int, decimal> { { 1, 300 } } },
            { "Кварцевое покрытие", new Dictionary<int, decimal> { { 1, 850 }, { 2, 950 }, { 3, 1050 }, { 4, 1150 } } },
            { "Полироль пластика", new Dictionary<int, decimal> { { 1, 300 } } },
            { "Кондиционер кожи(цена от)", new Dictionary<int, decimal> { { 1, 1500 } } },
            { "Чистка руля", new Dictionary<int, decimal> { { 1, 500 } } },
            { "Обработка силиконом", new Dictionary<int, decimal> { { 1, 300 } } },
            { "Удаление насекомых", new Dictionary<int, decimal> { { 1, 250 } } },
            { "Битум, металлические вкрапления(цена за элемент)", new Dictionary<int, decimal> { { 1, 150 } } },
            { "Антидождь быстрый", new Dictionary<int, decimal> { { 1, 150 } } },
            { "Антидождь крайтека(передний контур)", new Dictionary<int, decimal> { { 1, 3500 } } },
            { "Антидождь крайтека(вкруг)", new Dictionary<int, decimal> { { 1, 6000 } } },
            { "Очистка дисков", new Dictionary<int, decimal> { { 1, 300 } } },
            { "Мойка колес", new Dictionary<int, decimal> { { 1, 1200 } } },
            { "Мойка двигателя(цена от)", new Dictionary<int, decimal> { { 1, 1500 } } }
        };

        int serviceId = 1;

        // Добавляем основные услуги
        foreach (var service in mainServices)
        {
            data.Services.Add(new Service
            {
                Id = serviceId++,
                Name = service.Key,
                DurationMinutes = GetDurationForService(service.Key),
                Description = GetDescriptionForService(service.Key),
                IsActive = true,
                PriceByBodyType = service.Value
            });
        }

        // Добавляем дополнительные услуги
        foreach (var service in extraServices)
        {
            data.Services.Add(new Service
            {
                Id = serviceId++,
                Name = service.Key,
                DurationMinutes = GetDurationForService(service.Key),
                Description = GetDescriptionForService(service.Key),
                IsActive = true,
                PriceByBodyType = service.Value
            });
        }

        System.Diagnostics.Debug.WriteLine($"Загружено услуг: {data.Services.Count}");
    }

    private static int GetDurationForService(string serviceName)
    {
        // Обычный switch вместо switch expression
        switch (serviceName)
        {
            case "Техническая мойка":
                return 30;
            case "Профессиональная мойка кузова":
                return 45;
            case "Комплекс \"ИЗИ\"":
                return 60;
            case "Комплекс \"Глянец\"":
                return 90;
            case "Кварцевое покрытие":
                return 60;
            case "Полироль пластика":
                return 30;
            case "Кондиционер кожи(цена от)":
                return 45;
            case "Чистка руля":
                return 15;
            case "Обработка силиконом":
                return 20;
            case "Удаление насекомых":
                return 15;
            case "Битум, металлические вкрапления(цена за элемент)":
                return 15;
            case "Антидождь быстрый":
                return 15;
            case "Антидождь крайтека(передний контур)":
                return 30;
            case "Антидождь крайтека(вкруг)":
                return 45;
            case "Очистка дисков":
                return 20;
            case "Мойка колес(под щетку 4 колеса с шампунем)":
                return 30;
            case "Мойка двигателя(цена от)":
                return 45;
            default:
                return 30;
        }
    }

    private static string GetDescriptionForService(string serviceName)
    {
        // Обычный switch вместо switch expression
        switch (serviceName)
        {
            case "Техническая мойка":
                return "Двухфазная мойка без сушки, коврики";
            case "Профессиональная мойка кузова":
                return "Двухфазная мойка, воск, турбосушка, коврики, педальный блок";
            case "Комплекс \"ИЗИ\"":
                return "Трехфазная мойка, влажная уборка, пылесос, стекла, турбосушка, коврики, педальный блок, чернение";
            case "Комплекс \"Глянец\"":
                return "Двухфазная мойка, кварц, влажная уборка, пылесос, стекла, багажник, полироль, турбосушка, коврики, педальный блок, чернение";
            case "Кварцевое покрытие":
                return "Защитное кварцевое покрытие кузова";
            case "Полироль пластика":
                return "Полировка пластиковых элементов";
            case "Кондиционер кожи(цена от)":
                return "Уход за кожаным салоном";
            case "Чистка руля":
                return "Химчистка рулевого колеса";
            case "Обработка силиконом":
                return "Силиконовая обработка уплотнительных резинок";
            case "Удаление насекомых":
                return "Удаление следов насекомых";
            case "Битум, металлические вкрапления(цена за элемент)":
                return "Удаление битумных пятен";
            case "Антидождь быстрый":
                return "Быстрое нанесение антидождя";
            case "Антидождь крайтека(передний контур)":
                return "Качественный антидождь на переднюю часть";
            case "Антидождь крайтека(вкруг)":
                return "Качественный антидождь на весь автомобиль";
            case "Очистка дисков":
                return "Очистка колесных дисков";
            case "Мойка колес":
                return "Полная мойка колес щеткой с шампунем";
            case "Мойка двигателя(цена от)":
                return "Мойка подкапотного пространства";
            default:
                return "";
        }
    }
}
