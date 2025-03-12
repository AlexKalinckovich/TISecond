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
    private static string? _outputFilePath;
    
    public MainWindow()
    {
        InitializeComponent();
    }


    private static List<string> GetFileDataText(string fileData)
    {
        var list = new List<string>(fileData.Length / 100);
        for (int i = 0; i < fileData.Length; i += 100)
        {
            list.Add(i + 100 <= fileData.Length ? fileData.Substring(i, 100) : fileData[i..]);
        }
        return list;
    }
    private void UpdatePath(TextBox textBox,TextBox listView)
    {
        var inputFilePath = FileManager.GetFilePath();
        
        if (string.IsNullOrEmpty(inputFilePath)) return;
        
        var fileDataList = GetFileDataText(File.ReadAllText(inputFilePath));
        OutputFileListView.ItemsSource = new ObservableCollection<string>();
        KeyViewer.ItemsSource = new ObservableCollection<string>();
        if (inputFilePath.Length > 0)
        {
            var list = new List<string>(fileDataList);
            textBox.Text = inputFilePath;
            listView.Text = string.Join("",list);
        }
    }
    
    private void OpenCipherFileButtonClick(object sender, RoutedEventArgs e)
    {
        UpdatePath(InputFilePath,ManualBitsInput);
    }

    private void OpenResultFileButtonClick(object sender, RoutedEventArgs e)
    {
        var outputFilePath = FileManager.GetFilePath();
        if (outputFilePath != null)
        {
            OutputFilePath.Text = outputFilePath;
            _outputFilePath = outputFilePath;
        }
    }

    private void UpdateOutputView(string? outputFilePath, string encryptedData,List<string> keyStages)
    {
        if (outputFilePath == null || encryptedData.Length == 0) return;
        var list = new List<string> { encryptedData };
        OutputFileListView.ItemsSource = new ObservableCollection<string?>(list!);
        KeyViewer.ItemsSource = new ObservableCollection<string?>(keyStages!);
    }
    
    private void StartProcessButtonClick(object sender, RoutedEventArgs e)
    {
        if (_outputFilePath == null || !ValidateInputs()) return;

        var bitString = ManualBitsInput.Text;
        try
        {
            
            using var cipher = new ThreadEncoder(
                KeyInput.Text,
                bitString,
                _outputFilePath
            );

            cipher.StartProcessing();
            UpdateOutputView(_outputFilePath,cipher.GetEncryptedBytes(),cipher.GetKeyStages());
        }
        catch (ArgumentException ex)
        {
            MessageManager.ShowError(ex.Message);
        }
        catch (IOException ex)
        {
            MessageManager.ShowError(ex.Message);
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
               && !string.IsNullOrWhiteSpace(ManualBitsInput.Text)
               && !string.IsNullOrWhiteSpace(OutputFilePath.Text);
    }
}