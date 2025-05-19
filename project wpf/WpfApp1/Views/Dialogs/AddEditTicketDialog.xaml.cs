// File: WpfApp1/Views/Dialogs/AddEditTicketDialog.xaml.cs
using System.Windows;
using WpfApp1.Models; 
// using WpfApp1.ViewModels.Dialogs; // Больше не нужен здесь

namespace WpfApp1.Views.Dialogs
{
    public partial class AddEditTicketDialog : Window
    {
        // Конструктор теперь может быть без параметров, 
        // т.к. Ticket передается при инициализации ViewModel ДО создания окна.
        // Либо можно оставить конструктор с Ticket, но он не будет использоваться
        // DialogService для передачи данных.
        // Оставим конструктор по умолчанию.
        public AddEditTicketDialog() 
        {
            InitializeComponent();
            // DataContext будет установлен извне через DialogService перед вызовом ShowDialog
        }

        // Обработчик Window_Loaded больше не нужен, т.к. инициализация ViewModel
        // происходит до вызова ShowDialog в DialogService.
        // private async void Window_Loaded(object sender, RoutedEventArgs e) { ... }
    }
}