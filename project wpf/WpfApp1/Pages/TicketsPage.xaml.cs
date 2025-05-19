using System.Windows.Controls;
using WpfApp1.ViewModels; // Добавьте using для ViewModel

namespace WpfApp1.Pages
{
    public partial class TicketsPage : Page // Строка 6 (для CS8646)
    {
        public TicketsPage(TicketsViewModel viewModel)
        {
            InitializeComponent(); // Строка 10 (для CS0121)
            DataContext = viewModel;
        }
    }
}