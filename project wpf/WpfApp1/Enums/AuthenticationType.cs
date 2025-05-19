using System.ComponentModel; // Добавлено это пространство имен

namespace WpfApp1.Enums
{
    public enum AuthenticationType
    {
        [Description("Локальная учетная запись")] // Добавлен атрибут Description
        Local,

        [Description("Учетная запись Active Directory")] // Добавлен атрибут Description
        ActiveDirectory
    }
}