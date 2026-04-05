using System.Windows;
using System.Windows.Input;

namespace MyPanelCarWashing.Controls
{
    /// <summary>
    /// Базовый класс для popup-окон с поддержкой перетаскивания и кастомного оформления
    /// </summary>
    public class PopupWindowBase : Window
    {
        public PopupWindowBase()
        {
            // Применяем базовый стиль для popup-окон
            Style = (Style)Application.Current.FindResource("PopupWindowStyle");
            
            // Устанавливаем шаблон окна
            Template = (ControlTemplate)Application.Current.FindResource("PopupWindowTemplate");
            
            // Настраиваем начальные свойства
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        /// <summary>
        /// Обработчик нажатия на заголовок окна для перетаскивания
        /// </summary>
        private void WindowTitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                DragMove();
            }
        }

        /// <summary>
        /// Обработчик нажатия на кнопку закрытия
        /// </summary>
        private void PopupCloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Показать окно как модальное popup относительно владельца
        /// </summary>
        /// <param name="owner">Владелец окна</param>
        /// <returns>Результат диалога (true/false)</returns>
        public new bool? ShowDialog(Window owner = null)
        {
            if (owner != null)
            {
                Owner = owner;
            }
            else if (Application.Current.MainWindow != null)
            {
                Owner = Application.Current.MainWindow;
            }

            // Центрируем окно относительно владельца
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            
            return base.ShowDialog();
        }

        /// <summary>
        /// Показать окно как немодальное popup относительно владельца
        /// </summary>
        /// <param name="owner">Владелец окна</param>
        public new void Show(Window owner = null)
        {
            if (owner != null)
            {
                Owner = owner;
            }
            else if (Application.Current.MainWindow != null)
            {
                Owner = Application.Current.MainWindow;
            }

            // Центрируем окно относительно владельца
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            
            base.Show();
        }
    }
}
