using System.Text;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;

namespace TISecond.Models.Cipher;

public class ThreadEncoder : IDisposable
{
    private const int MinimumKeyLength = 24;
    private const int BlockSizeBits = 24;
    private readonly BitArray _key;
    private readonly BlockingCollection<BitArray> _dataQueue = new();
    private readonly List<string> _keyStages = [];
    private readonly List<string> _encryptedBlocks = [];
    private readonly int _originalBitCount;
    private bool _disposed;
    private uint _iteration = 1;

    public ThreadEncoder(string key, string bitString, string outputFilePath)
    {
        // Валидация ключа
        var validatedKey = KeyValidator.ValidateKey(key);
        if (validatedKey.Length < MinimumKeyLength)
            throw new ArgumentException($"Ключ должен содержать минимум {MinimumKeyLength} бит.");

        _key = new BitArray(validatedKey.Select(c => c == '1').ToArray());
        OutputFilePath = outputFilePath;
        
        bitString = KeyValidator.ValidateKey(bitString);
        
        _originalBitCount = bitString.Length;
        if (_originalBitCount == 0)
        {
            throw new ArgumentException("Текст пустой или полностью состоит из некорректных символов.");
        }
        
        var inputBits = new BitArray(_originalBitCount);
        for (var i = 0; i < _originalBitCount; i++)
            inputBits[i] = bitString[i] == '1';

        SourceBits = inputBits;
    }

    private string OutputFilePath { get; }
    private BitArray SourceBits { get; }

    public void StartProcessing()
    {
        if(_originalBitCount == 0) return;
        
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
            var position = 0;
            while (position < _originalBitCount)
            {
                var bitsToProcess = Math.Min(BlockSizeBits, _originalBitCount - position);
                var block = new BitArray(bitsToProcess);
                
                for (var i = 0; i < bitsToProcess; i++)
                    block[i] = SourceBits[position + i];

                var encryptedBlock = ProcessBlock(block);
                _dataQueue.Add(encryptedBlock, ct);
                
                position += bitsToProcess;
                UpdateKey();
            }
            _dataQueue.CompleteAdding();
        }
        catch
        {
            _dataQueue.CompleteAdding();
            throw;
        }
    }

    private BitArray ProcessBlock(BitArray block)
    {
        var keyPart = new BitArray(_key);
        if (block.Length < _key.Length)
            keyPart.Length = block.Length;

        block.Xor(keyPart);
        return block;
    }

    private void UpdateKey()
    {
        // Логика обновления ключа
        var feedback = _key[0] ^ _key[3] ^ _key[4] ^ _key[23];
        _key.RightShift(1);
        _key[^1] = feedback;

        _keyStages.Add($"Итерация {_iteration}: {GetBitView(_key)}");
        _iteration++;
    }

    private void WriteEncryptedData(CancellationToken ct)
    {
        using var fs = new FileStream(OutputFilePath, FileMode.Create);
        using var bw = new BinaryWriter(fs);
        var sb = new StringBuilder();
        foreach (var block in _dataQueue.GetConsumingEnumerable(ct))
        {
            foreach (var bit in block)
            {
                sb.Append((bool)bit ? "1" : "0");
            }
            
            var str = sb.ToString();
            _encryptedBlocks.Add(str);

            bw.Write(str);
            sb.Clear();
        }
    }

    public string GetEncryptedBytes()
    {
        return string.Join("", _encryptedBlocks);
    }

    private static byte[] ConvertToBytes(BitArray bits)
    {
        if(bits.Length == 0) return [];
        
        var bytes = new byte[(bits.Length + 7) / 8];
        bits.CopyTo(bytes, 0);
        return bytes;
    }

    public List<string> GetKeyStages() => _keyStages;

    public void Dispose()
    {
        if (_disposed) return;
        _dataQueue.Dispose();
        _disposed = true;
    }

    private static string GetBitView(BitArray bits)
    {
        var sb = new StringBuilder(bits.Length);
        foreach (bool bit in bits)
            sb.Append(bit ? "1" : "0");
        return sb.ToString();
    }
}