// fefe-main/WpfApp1/Interfaces/INavigationService.cs
// Interfaces/INavigationService.cs
using System; // Добавлен using для Type

namespace WpfApp1.Interfaces;

public interface INavigationService
{
    // Метод для навигации к странице по её типу
    void NavigateTo(Type pageType);

    // Можно добавить и другие методы, например, CanGoBack, GoBack и т.д.,
    // но для MVP пока хватит основного.
}