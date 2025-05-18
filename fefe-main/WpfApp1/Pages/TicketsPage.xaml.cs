using System.Windows.Controls;
using WpfApp1.ViewModels; // Добавьте using для ViewModel

namespace WpfApp1.Pages
{
    public partial class TicketsPage : Page
    {
        public TicketsPage(TicketsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}