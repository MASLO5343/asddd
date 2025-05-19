using System.Windows.Controls;
using WpfApp1.ViewModels;

namespace WpfApp1.Pages // Пространство имен
{
    public partial class DashboardPage : Page // Имя класса и ключевое слово partial
    {
        public DashboardPage(DashboardViewModel viewModel)
        {
            InitializeComponent(); // Здесь возникает ошибка
            DataContext = viewModel;
        }
    }
}