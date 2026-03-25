using MyPanelCarWashing.Services;
using System;

namespace MyPanelCarWashing
{
    class Core
    {
        private static DataService _dataService;

        public static DataService DB
        {
            get
            {
                try
                {
                    if (_dataService == null)
                        _dataService = new DataService();
                    return _dataService;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка Core.DB: {ex.Message}");
                    throw;
                }
            }
        }

        public static void RefreshData()
        {
            _dataService = new DataService();
        }
    }
}