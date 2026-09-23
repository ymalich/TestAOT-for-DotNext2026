using BenchmarkDotNet.Attributes;
using Microsoft.IO;

namespace Benchmarks;

public class AesBench
{
    private static readonly RecyclableMemoryStreamManager RecyclableMemoryStreamManager = new RecyclableMemoryStreamManager();

    private byte[] _iv = [];

    private byte[] _key = [];

    static private byte[] _data = new byte[1024 * 1024];

    private Aes _aesAlg = Aes.Create();

    private MemoryStream outStream = new();

    [GlobalSetup]
    public void Setup()
    {
        _iv = new byte[_aesAlg.IV.Length];
        _key = new byte[_aesAlg.Key.Length];
        Random.Shared.NextBytes(_iv);
        Random.Shared.NextBytes(_key);
        Random.Shared.NextBytes(_data);

        _aesAlg.IV = _iv;
        _aesAlg.Key = _key;

        ICryptoTransform encryptor = _aesAlg.CreateEncryptor(_key, _iv);

        outStream = new MemoryStream();

        // Create the streams used for encryption.
        {
            using CryptoStream csEncrypt = new CryptoStream(outStream, encryptor, CryptoStreamMode.Write, true);
            csEncrypt.Write(_data, 0, _data.Length);
        }

        outStream.Position = 0;
        
        ////using var cStream = new CryptoStream(outStream, _aesAlg.CreateDecryptor(_key, _iv), CryptoStreamMode.Read, true);
        ////var data2 = new byte[1024 * 1024];
        ////cStream.ReadExactly(data2);

        //if (_encData.Length > outStream.Length)
        //{
        //    _encData = new byte[outStream.Length];
        //}

        ////outStream.WriteTo(_encData);
    }

    [Benchmark]
    public void Encrypt()
    {
        using var outStream = RecyclableMemoryStreamManager.GetStream("1");

        // Create the streams used for encryption.
        using CryptoStream csEncrypt = new CryptoStream(outStream, _aesAlg.CreateEncryptor(_key, _iv), CryptoStreamMode.Write);
        csEncrypt.Write(_data, 0, _data.Length);
    }

    [Benchmark]
    public void Decrypt()
    {
        var data2 = ArrayPool<byte>.Shared.Rent(_data.Length);
        outStream.Position = 0;
        using var cStream = new CryptoStream(outStream, _aesAlg.CreateDecryptor(_key, _iv), CryptoStreamMode.Read, true);
        cStream.ReadAtLeast(data2, _data.Length);
        ArrayPool<byte>.Shared.Return(data2);
    }
}