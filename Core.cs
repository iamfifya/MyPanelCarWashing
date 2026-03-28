using MyPanelCarWashing.Services;
using System;

namespace MyPanelCarWashing
{
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
