using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using TISecond.Models;
using TISecond.Models.Cipher;

namespace TISecond.View;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }


    private static void UpdatePath(TextBox textBox,ListView listView)
    {
        var inputFilePath = FileManager.GetFilePath();
        if (inputFilePath.Length > 0)
        {
            textBox.Text = inputFilePath;
            listView.ItemsSource = new ObservableCollection<string>(File.ReadAllLines(inputFilePath));
        }
    }
    
    private void OpenCipherFileButtonClick(object sender, RoutedEventArgs e)
    {
        UpdatePath(InputFilePath,InputFileListView);
    }

    private void OpenResultFileButtonClick(object sender, RoutedEventArgs e)
    {
        UpdatePath(OutputFilePath,OutputFileListView);
    }

    private void UpdateOutputView(string outputFilePath)
    {
        OutputFileListView.ItemsSource = new ObservableCollection<string>(File.ReadAllLines(outputFilePath));   
    }
    
    private void StartProcessButtonClick(object sender, RoutedEventArgs e)
    {
        if (!ValidateInputs()) return;
    
        try
        {
            using var cipher = new ThreadEncoder(
                KeyInput.Text, 
                InputFilePath.Text, 
                OutputFilePath.Text
            );
            UpdateOutputView(OutputFilePath.Text);
            MessageManager.ShowSuccess("Шифрование завершено!");
        }
        catch (ArgumentException ex)
        {
            MessageManager.ShowError($"Ошибка ключа: {ex.Message}");
        }
        catch (FileNotFoundException ex)
        {
            MessageManager.ShowError($"Файл не найден: {ex.FileName}");
        }
        catch (IOException ex)
        {
            MessageManager.ShowError($"Ошибка ввода-вывода: {ex.Message}");
        }
    }

    private static bool IsDifferentPath(string inputFilePath, string outputFilePath)
    {
        var isDifferent = !inputFilePath.Equals(outputFilePath);
        if (!isDifferent)
        {
            MessageManager.ShowWarning("Файлы чтения и записи одинаковые");
        }
        return isDifferent;
    }
    private bool ValidateInputs()
    {
        var isDifferent = IsDifferentPath(inputFilePath:InputFilePath.Text,outputFilePath:OutputFilePath.Text);
        return isDifferent && !string.IsNullOrWhiteSpace(KeyInput.Text) 
               && !string.IsNullOrWhiteSpace(InputFilePath.Text)
               && !string.IsNullOrWhiteSpace(OutputFilePath.Text);
    }
}