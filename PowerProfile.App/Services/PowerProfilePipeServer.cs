using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace PowerProfile.App.Services;

/// <summary>
/// Local, newline-delimited JSON IPC server for the Flutter dashboard.
/// Each client sends one request and receives one response before disconnecting.
/// </summary>
public sealed class PowerProfilePipeServer : IAsyncDisposable
{
    public const string PipeName = "PowerProfile.LocalApi.v1";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly PowerProfileBackend _backend;
    private readonly CancellationTokenSource _stop = new();
    private Task? _acceptLoop;

    public PowerProfilePipeServer(PowerProfileBackend backend) => _backend = backend;

    public void Start() => _acceptLoop ??= Task.Run(AcceptLoopAsync);

    private async Task AcceptLoopAsync()
    {
        while (!_stop.IsCancellationRequested)
        {
            try
            {
                await using var pipe = new NamedPipeServerStream(
                    PipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await pipe.WaitForConnectionAsync(_stop.Token);
                await HandleClientAsync(pipe, _stop.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_stop.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                // A malformed/disconnected client must never take down the tray host.
                await Task.Delay(100, _stop.Token).ConfigureAwait(false);
            }
        }
    }

    private async Task HandleClientAsync(NamedPipeServerStream pipe, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(pipe, Encoding.UTF8, false, 16 * 1024, leaveOpen: true);
        await using var writer = new StreamWriter(pipe, new UTF8Encoding(false), 16 * 1024, leaveOpen: true) { AutoFlush = true };
        PipeResponse response;
        try
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            response = line is null
                ? PipeResponse.FromError(null, "invalid_request", "Request stream ended before a JSON request was received.")
                : Dispatch(line);
        }
        catch (JsonException)
        {
            response = PipeResponse.FromError(null, "invalid_json", "Request must be valid JSON.");
        }
        catch (Exception ex)
        {
            response = PipeResponse.FromError(null, "internal_error", ex.Message);
        }

        await writer.WriteLineAsync(JsonSerializer.Serialize(response, JsonOptions)).ConfigureAwait(false);
    }

    private PipeResponse Dispatch(string line)
    {
        var request = JsonSerializer.Deserialize<PipeRequest>(line, JsonOptions)
            ?? throw new JsonException("Request is empty.");
        if (request.Version != 1)
            return PipeResponse.FromError(request.Id, "unsupported_version", "Only protocol version 1 is supported.");

        try
        {
            object data = request.Command switch
            {
                "getState" => _backend.GetState(),
                "apply" => _backend.Apply(request.Payload),
                "setLanguage" => _backend.SetLanguage(request.Payload),
                "applyRefreshRate" => _backend.ApplyRefreshRate(request.Payload),
                "applyAnimations" => _backend.ApplyAnimations(request.Payload),
                _ => throw new BackendCommandException("unknown_command", $"Unknown command '{request.Command}'."),
            };
            return PipeResponse.Success(request.Id, data);
        }
        catch (BackendCommandException ex)
        {
            return PipeResponse.FromError(request.Id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return PipeResponse.FromError(request.Id, "operation_failed", ex.Message);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        if (_acceptLoop is not null)
        {
            try { await _acceptLoop.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
        }
        _stop.Dispose();
    }

    private sealed record PipeRequest(string? Id, int Version, string? Command, JsonElement Payload);
    private sealed record PipeError(string Code, string Message);
    private sealed record PipeResponse(string? Id, bool Ok, object? Data, PipeError? Error)
    {
        public static PipeResponse Success(string? id, object data) => new(id, true, data, null);
        public static PipeResponse FromError(string? id, string code, string message) => new(id, false, null, new PipeError(code, message));
    }
}
