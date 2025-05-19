// WpfApp1/LoginWindow.xaml.cs
using System.Windows;

namespace WpfApp1
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            // DataContext обычно устанавливается через DI при создании окна
            // Например, в App.xaml.cs:
            // var loginViewModel = _serviceProvider.GetRequiredService<LoginViewModel>();
            // var loginWindow = new LoginWindow { DataContext = loginViewModel };
        }
    }
}