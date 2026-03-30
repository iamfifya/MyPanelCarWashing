using Microsoft.Extensions.DependencyInjection;
using MyPanelCarWashing.Services;
using System;
using System.Windows;

namespace MyPanelCarWashing
{
    public partial class App : Application
    {
        private ServiceProvider _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Глобальная обработка необработанных исключений
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                MessageBox.Show($"Критическая ошибка: {ex?.Message}\n\nПриложение будет закрыто.",
                    "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            };

            DispatcherUnhandledException += (sender, args) =>
            {
                // Не показываем ошибку при каждом сбое, только логируем
                System.Diagnostics.Debug.WriteLine($"Ошибка UI: {args.Exception.Message}");
                System.Diagnostics.Debug.WriteLine(args.Exception.StackTrace);
                args.Handled = true; // Помечаем как обработанную, чтобы приложение не падало
            };

            // Настройка DI
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // Запуск приложения
            var loginWindow = _serviceProvider.GetRequiredService<LoginWindow>();
            loginWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Регистрируем сервисы
            services.AddSingleton<DataService>();

            // Регистрируем окна
            services.AddTransient<LoginWindow>();
            services.AddTransient<MainWindow>();
            services.AddTransient<AddEditOrderWindow>();
            services.AddTransient<EditOrderServicesWindow>();
            services.AddTransient<EmployeeCardWindow>();
            services.AddTransient<AddEditEmployeeWindow>();
            services.AddTransient<ServiceManagementWindow>();
            services.AddTransient<AddEditServiceWindow>();
            services.AddTransient<ReportsWindow>();
            services.AddTransient<MonthlyReportWindow>();
            services.AddTransient<StartShiftWindow>();
            services.AddTransient<AppointmentsWindow>();
            services.AddTransient<ScheduleWindow>();
            services.AddTransient<CustomDatePicker>();
            services.AddTransient<CustomReportWindow>();
            services.AddTransient<ClientsWindow>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}
