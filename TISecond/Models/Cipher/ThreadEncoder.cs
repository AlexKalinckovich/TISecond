
namespace TISecond.Models.Cipher;

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class ThreadEncoder : IDisposable
{
    private const int MinimumKeyLength = 24;
    private const int BlockSizeBytes = 3; // 24 бита
    private readonly string? _inputFilePath;
    private readonly string? _outputFilePath;
    private readonly BitArray _key;
    private readonly BlockingCollection<byte[]> _dataQueue = new();
    private readonly List<string> _keyStages = new();
    private readonly List<byte[]> blocks = new();
    private bool _disposed;
    private uint _iteration = 1;

    public ThreadEncoder(string key, string? inputFilePath, string? outputFilePath)
    {
        var validatedKey = ValidateKey(key);
        if (validatedKey.Length < MinimumKeyLength)
            throw new ArgumentException($"Ключ должен содержать минимум {MinimumKeyLength} бит.");
        
        _key = new BitArray(validatedKey.Select(c => c == '1').ToArray());

        if (!File.Exists(inputFilePath))
            throw new FileNotFoundException("Входной файл не найден.", inputFilePath);
        _inputFilePath = inputFilePath;

        try
        {
            File.WriteAllText(outputFilePath!, string.Empty); // Создаем/очищаем файл
        }
        catch
        {
            throw new IOException("Невозможно записать в выходной файл.");
        }
        _outputFilePath = outputFilePath;

    }

    private static string ValidateKey(string key)
    {
        return new string(key.Where(c => c is '0' or '1').ToArray());
    }

    public void StartProcessing()
    {
        var cts = new CancellationTokenSource();
        
        var processingTasks = new[]
        {
            Task.Run(() => ReadAndEncrypt(cts.Token), cts.Token), 
            Task.Run(() => WriteEncryptedData(cts.Token), cts.Token)
        };

        Task.WaitAll(processingTasks, cts.Token);
    }

    private void ReadAndEncrypt(CancellationToken ct)
    {
        try
        {
            using var inputStream = File.OpenRead(_inputFilePath!);
            var buffer = new byte[BlockSizeBytes];
            int bytesRead;
            
            while ((bytesRead = inputStream.Read(buffer, 0, BlockSizeBytes)) > 0)
            {
                if (bytesRead < BlockSizeBytes)
                    Array.Resize(ref buffer, bytesRead);

                var encryptedBlock = ProcessBlock(buffer);
                _dataQueue.Add(encryptedBlock, ct);

                UpdateKey();
                buffer = new byte[BlockSizeBytes]; // Сброс буфера
            }
            _dataQueue.CompleteAdding();
        }
        catch
        {
            _dataQueue.CompleteAdding();
            throw;
        }
    }
    
    private byte[] ProcessBlock(byte[] data)
    {
        var dataBits = new BitArray(data);
        var dataLength = dataBits.Length;
        
        var keyPart = new BitArray(_key);
        if (dataLength < _key.Length)
        {
            keyPart.Length = dataLength;
        }
        
        // Применяем XOR
        dataBits.Xor(keyPart);

        return ConvertToBytes(dataBits);
    }

    private void UpdateKey()
    {
        // Вычисляем обратную связь: 24 бит XOR 4 бит XOR 3 бит XOR 1 бит
        var feedback = _key[0] ^ _key[2] ^ _key[3] ^ _key[23];
        char result = feedback ? '1' : '0';
        _keyStages.Add($"On {_iteration} iteration exit bit is {result}");
        _iteration++;

        _key.LeftShift(1);
        // Устанавливаем новый бит на последнюю позицию
        _key[0] = feedback;

    }

    private static byte[] ConvertToBytes(BitArray bits)
    {
        var bytes = new byte[(bits.Length + 7) / 8];
        bits.CopyTo(bytes, 0);
        return bytes;
    }

    private void WriteEncryptedData(CancellationToken ct)
    {
        using var outputStream = File.OpenWrite(_outputFilePath!);
        foreach (var block in _dataQueue.GetConsumingEnumerable(ct))
        {
            blocks.Add(block);
            outputStream.Write(block, 0, block.Length);
        }
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        _dataQueue.Dispose();
        _disposed = true;
    }

    public List<string> GetKeyStages()
    {
        return _keyStages;
    }

    public byte[] GetEncryptedData()
    {
        long size = 0;
        foreach (var block in blocks)
        {
            size += block.Length;
        }
        
        byte[] encryptedData = new byte[size];
        int index = 0;
        foreach (var item in blocks.SelectMany(block => block))
        {
            encryptedData[index++] = item;
        }
        return encryptedData;
    }
}