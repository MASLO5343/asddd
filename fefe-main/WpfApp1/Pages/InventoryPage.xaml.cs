using System.Windows.Controls;

namespace WpfApp1.Pages
{
    public partial class InventoryPage : UserControl
    {
        public InventoryPage()
        {
            InitializeComponent();
            // DataContext обычно устанавливается через DI или в XAML d:DataContext для времени разработки
            // и через реальную ViewModel во время выполнения.
            // Если InventoryViewModel должен быть DataContext по умолчанию,
            // это делается при создании страницы, например, через DI.
        }

        // Если вы обрабатываете событие DataGrid_Sorting, как в вашем файле InventoryPage.xaml.cs:
        private void DataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            if (DataContext is WpfApp1.ViewModels.InventoryViewModel viewModel) // Убедитесь, что используется правильный тип ViewModel
            {
                if (e.Column.SortMemberPath != null)
                {
                    if (viewModel.SortCommand.CanExecute(e.Column.SortMemberPath))
                    {
                        viewModel.SortCommand.Execute(e.Column.SortMemberPath);
                    }
                    e.Handled = true;
                }
            }
        }
    }
}