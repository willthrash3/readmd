using System.IO;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Readmd.Infrastructure;

public sealed class SingleInstanceCoordinator : IDisposable
{
    private readonly CancellationTokenSource _stopListening = new();
    private readonly string _mutexName;
    private readonly string _pipeName;
    private Mutex? _mutex;
    private Task? _listenTask;
    private bool _ownsMutex;

    public SingleInstanceCoordinator(string instanceName)
    {
        var suffix = Hash(instanceName);
        _mutexName = $@"Local\Readmd-{suffix}";
        _pipeName = $"Readmd-{suffix}";
    }

    public event Action<string[]>? OpenRequested;

    public bool TryBecomePrimary()
    {
        _mutex = new Mutex(initiallyOwned: true, _mutexName, out var createdNew);
        _ownsMutex = createdNew;
        return createdNew;
    }

    public void StartListening()
    {
        if (!_ownsMutex)
        {
            return;
        }

        _listenTask = Task.Run(ListenAsync);
    }

    public static void SendOpenRequest(string instanceName, IReadOnlyCollection<string> paths)
    {
        var request = JsonSerializer.Serialize(new OpenRequest(paths.ToArray()));
        var pipeName = $"Readmd-{Hash(instanceName)}";
        var payload = Encoding.UTF8.GetBytes(request);

        for (var attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous);
                client.Connect(250);
                client.Write(payload, 0, payload.Length);
                client.Flush();
                return;
            }
            catch (IOException)
            {
                Thread.Sleep(100);
            }
            catch (TimeoutException)
            {
                Thread.Sleep(100);
            }
        }
    }

    private async Task ListenAsync()
    {
        while (!_stopListening.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync(_stopListening.Token);

                using var reader = new StreamReader(server, Encoding.UTF8);
                var json = await reader.ReadToEndAsync();
                var request = JsonSerializer.Deserialize<OpenRequest>(json);
                OpenRequested?.Invoke(request?.Paths ?? []);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (IOException)
            {
            }
            catch (JsonException)
            {
            }
        }
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes)[..16];
    }

    public void Dispose()
    {
        _stopListening.Cancel();
        try
        {
            _listenTask?.Wait(TimeSpan.FromSeconds(1));
        }
        catch (AggregateException)
        {
        }

        if (_ownsMutex)
        {
            _mutex?.ReleaseMutex();
        }

        _mutex?.Dispose();
        _stopListening.Dispose();
    }
}
