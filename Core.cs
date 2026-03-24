using MyPanelCarWashing.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyPanelCarWashing
{
    class Core
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

        // Добавьте метод для принудительной перезагрузки данных
        public static void RefreshData()
        {
            _dataService = new DataService();
        }
    }
}