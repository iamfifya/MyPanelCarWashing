using MyPanelCarWashing.Services;
using System;

namespace MyPanelCarWashing
{
    // Статический класс для обратной совместимости
    // В новых окнах используйте DI через конструктор
    public static class Core
    {
        private static DataService _dataService;

        public static DataService DB
        {
            get
            {
                if (_dataService == null)
                    _dataService = new DataService();
                return _dataService;
            }
        }

        public static void RefreshData()
        {
            _dataService = new DataService();
        }
    }
}
