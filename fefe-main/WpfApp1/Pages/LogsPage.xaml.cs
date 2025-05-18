// fefe-main/WpfApp1/Pages/LogsPage.xaml.cs
using System.Windows.Controls; // Для Page
using WpfApp1.ViewModels; // Для LogsViewModel

namespace WpfApp1.Pages // Убедитесь, что пространство имен верное
{
    /// <summary>
    /// Code-behind для страницы журнала событий.
    /// </summary>
    public partial class LogsPage : Page
    {
        // Конструктор для использования с DI.
        // DI контейнер предоставит экземпляр LogsViewModel.
        public LogsPage(LogsViewModel viewModel)
        {
            InitializeComponent();

            // Устанавливаем DataContext страницы на инжектированную ViewModel
            DataContext = viewModel;

            // TODO: Возможно, добавить обработчик события Loaded страницы,
            // если Вы хотите загружать логи не в конструкторе ViewModel,
            // а при показе страницы.
            // this.Loaded += LogsPage_Loaded;
        }

        // TODO: Пример обработчика Loaded, если нужно загружать логи при показе страницы
        // private void LogsPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        // {
        //     // Отписываемся от события, чтобы избежать множественной загрузки при повторном показе
        //     this.Loaded -= LogsPage_Loaded;
        //
        //     // Получаем ViewModel из DataContext и вызываем команду загрузки логов
        //     if (DataContext is LogsViewModel viewModel)
        //     {
        //         // Вызываем команду загрузки логов
        //         viewModel.LoadLogsCommand.Execute(null);
        //     }
        // }

        // --- Здесь больше НЕ ДОЛЖНО БЫТЬ бизнес-логики или логики загрузки данных ---
        // Вся эта логика находится в LogsViewModel.cs
    }
}