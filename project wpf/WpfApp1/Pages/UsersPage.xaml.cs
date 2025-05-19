// WpfApp1/Pages/UsersPage.xaml.cs
using System.Windows.Controls;
using WpfApp1.ViewModels; // Пространство имен для UsersViewModel
using System.Windows;     // Для MessageBox и Application (если понадобится для логирования ошибок)
using System;             // Для Exception

namespace WpfApp1.Pages
{
    public partial class UsersPage : Page
    {
        // ViewModel будет предоставлена через DI контейнер.
        // Конструктор теперь принимает UsersViewModel как параметр.
        public UsersPage(UsersViewModel viewModel) // <--- ВНЕДРЯЕМ ViewModel через конструктор
        {
            InitializeComponent();

            if (viewModel == null)
            {
                // Обработка случая, если ViewModel не была предоставлена.
                // Это не должно происходить при правильной настройке DI.
                var errorMessage = "Критическая ошибка: UsersViewModel не была предоставлена для UsersPage.";
                // Логирование ошибки, если у вас настроен логгер, доступный статически или через временный ServiceLocator.
                // App.GetService<ILogger<UsersPage>>()?.LogError(errorMessage); // Пример, если есть статический логгер
                MessageBox.Show(errorMessage, "Ошибка ViewModel", MessageBoxButton.OK, MessageBoxImage.Error);
                // Можно предпринять дополнительные действия, например, закрыть страницу или приложение,
                // в зависимости от критичности.
                return; // Прерываем дальнейшую инициализацию, если ViewModel null.
            }

            DataContext = viewModel; // Устанавливаем DataContext страницы на инжектированную ViewModel.
            // UsersViewModel сама должна позаботиться о вызове LoadUsersAsync (например, в своем конструкторе или через команду).
        }

        // Конструктор по умолчанию (без параметров) удален,
        // так как теперь страница всегда должна создаваться с внедренной ViewModel.
        // Если по какой-то причине он был необходим (например, для дизайнера XAML, хотя это редкость для страниц с ViewModel),
        // то эту ситуацию нужно будет пересмотреть.
        // public UsersPage()
        // {
        //     InitializeComponent();
        //     // Старый код с App.GetService<UsersViewModel>() удален.
        // }
    }
}