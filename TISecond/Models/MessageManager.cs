using System.Windows;

namespace TISecond.Models;

public static class MessageManager
{
    public static void ShowInfo(string message)
    {
        MessageBox.Show(message, "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public static void ShowError(string message)
    {
        MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public static void ShowWarning(string message)
    {
        MessageBox.Show(message, "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    public static void ShowSuccess(string message)
    {
        MessageBox.Show(message, "Успех", MessageBoxButton.OK, MessageBoxImage.None);
    }
}
