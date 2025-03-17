using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
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
    
    private static string? _inputFilePath;
    private static string? _outputFilePath;
    
    public MainWindow()
    {
        InitializeComponent();
    }


    private static List<string> GetBitsView(in byte[] buffer)
    {
        var trimmedBuffer = buffer.Length > 300 ? buffer.Take(300).ToArray() : buffer;

        var bits = new BitArray(trimmedBuffer);
        var sb = new StringBuilder(bits.Length);

        foreach (var bit in bits)
        {
            sb.Append((bool)bit ? "1" : "0");
        }

        var bitData = sb.ToString();
        var list = new List<string>(bitData.Length / 150);

        for (int i = 0; i < bitData.Length; i += 150)
        {
            if (i + 150 <= bitData.Length)
            {
                list.Add(bitData.Substring(i, 150));
            }
            else
            {
                list.Add(bitData[i..]);
            }
        }

        return list;
    }

    private void UpdatePath(TextBox textBox,ListView listView)
    {
        var inputFilePath = FileManager.GetFilePath();
        
        if (string.IsNullOrEmpty(inputFilePath)) return;
        
        var bitsView = GetBitsView(File.ReadAllBytes(inputFilePath));
        OutputFileListView.ItemsSource = new ObservableCollection<string>();
        KeyViewer.ItemsSource = new ObservableCollection<string>();
        if (inputFilePath.Length > 0)
        {
            var list = new List<string>();
            textBox.Text = inputFilePath;
            _inputFilePath = inputFilePath;
            list.AddRange(bitsView);
            listView.ItemsSource = new ObservableCollection<string>(list);
        }
    }
    
    private void OpenCipherFileButtonClick(object sender, RoutedEventArgs e)
    {
        UpdatePath(InputFilePath,InputFileListView);
    }

    private void OpenResultFileButtonClick(object sender, RoutedEventArgs e)
    {
        var outputFilePath = FileManager.GetFilePath();
        if (!string.IsNullOrEmpty(outputFilePath))
        {
            OutputFilePath.Text = outputFilePath;
            _outputFilePath = outputFilePath;
        }
    }

    private void UpdateOutputView(string? outputFilePath, byte[] encryptedData)
    {
        if (string.IsNullOrEmpty(outputFilePath)) return;

        var bitView = GetBitsView(encryptedData);
        
        var list = new List<string>();
        list.AddRange(bitView);
        OutputFileListView.ItemsSource = new ObservableCollection<string?>(list!);
    }
    
    private void StartProcessButtonClick(object sender, RoutedEventArgs e)
    {
        if (!ValidateInputs()) return;
        KeyViewer.ItemsSource = new ObservableCollection<string>();
        try
        {
            using var cipher = new ThreadEncoder(
                KeyInput.Text, 
                _inputFilePath, 
                _outputFilePath
            );
            cipher.StartProcessing();
            KeyViewer.ItemsSource = new ObservableCollection<string>(cipher.GetKeyStages());
            UpdateOutputView(_outputFilePath,cipher.GetEncryptedData());
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