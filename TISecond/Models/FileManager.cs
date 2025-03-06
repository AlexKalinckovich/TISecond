using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace TISecond.Models;

public static class FileManager
{
    public static string GetFilePath()
    {
        try
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "All files (*.*)|*.*", // Фильтр для текстовых файлов
                Multiselect = false // Запрещаем выбор нескольких файлов
            };

            return openFileDialog.ShowDialog() == true ? openFileDialog.FileName : string.Empty; // Если файл не выбран
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageManager.ShowError($"Ошибка доступа к файлу: {ex.Message}");
        }
        catch (IOException ex)
        {
            MessageManager.ShowError($"Ошибка ввода/вывода: {ex.Message}");
        }
        catch (Exception ex)
        {
            MessageManager.ShowError($"Произошла ошибка: {ex.Message}");
        }
        return string.Empty; // Возвращаем null, если возникло исключение
        
    }
}