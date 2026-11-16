using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Acrofobia.Network
{
    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
    }

    /// <summary>
    /// WebSocket transport built on <see cref="ClientWebSocket"/> (no external packages). It reconnects
    /// on its own with a capped back-off and hands every event to the main thread in <see cref="Update"/>,
    /// so the rest of the app never touches threads. The app works the same when no server is reachable.
    /// </summary>
    public sealed class NetworkClient : MonoBehaviour
    {
        private const int ReplacedCloseCode = 4000;
        private const int ReceiveBufferSize = 16 * 1024;

        [SerializeField] private NetworkSettings settings;

        private readonly ConcurrentQueue<string> inbox = new();
        private readonly ConcurrentQueue<string> outbox = new();
        private readonly ConcurrentQueue<ConnectionStatus> statusChanges = new();
        private readonly SemaphoreSlim outboxSignal = new(0);
        private CancellationTokenSource lifetime;
        private volatile bool connected;

        public ConnectionStatus Status { get; private set; } = ConnectionStatus.Disconnected;
        public Uri ServerUri { get; private set; }

        public event Action<ConnectionStatus> StatusChanged;

        /// <summary>Raised on the main thread with the message type and the raw JSON.</summary>
        public event Action<string, string> MessageReceived;

        private void OnEnable()
        {
            ServerUri = settings.ServerUri;
            lifetime = new CancellationTokenSource();
            _ = Task.Run(() => RunAsync(lifetime.Token));
        }

        private void OnDisable()
        {
            lifetime?.Cancel();
            lifetime?.Dispose();
            lifetime = null;
        }

        private void Update()
        {
            while (statusChanges.TryDequeue(out var status))
            {
                Status = status;
                StatusChanged?.Invoke(status);
            }

            while (inbox.TryDequeue(out string json))
            {
                string type = Protocol.ReadType(json);
                if (type == null)
                {
                    Debug.LogWarning($"[NetworkClient] Ignoring malformed message: {json}");
                    continue;
                }
                MessageReceived?.Invoke(type, json);
            }
        }

        /// <summary>Queues a message. Returns false (and drops it) when there is no connection.</summary>
        public bool Send(string json)
        {
            if (!connected) return false;
            outbox.Enqueue(json);
            outboxSignal.Release();
            return true;
        }

        private async Task RunAsync(CancellationToken token)
        {
            int attempt = 0;
            while (!token.IsCancellationRequested)
            {
                bool replaced = false;
                statusChanges.Enqueue(ConnectionStatus.Connecting);
                using (var socket = new ClientWebSocket())
                using (var connection = CancellationTokenSource.CreateLinkedTokenSource(token))
                {
                    socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(10);
                    try
                    {
                        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(token))
                        {
                            timeout.CancelAfter(TimeSpan.FromSeconds(4));
                            await socket.ConnectAsync(ServerUri, timeout.Token);
                        }

                        attempt = 0;
                        while (outbox.TryDequeue(out _)) { } // anything queued belongs to an older connection
                        connected = true;
                        statusChanges.Enqueue(ConnectionStatus.Connected);

                        _ = IgnoreCancellation(SendLoopAsync(socket, connection.Token));
                        await ReceiveLoopAsync(socket, connection.Token);
                        replaced = socket.CloseStatus.HasValue && (int)socket.CloseStatus.Value == ReplacedCloseCode;
                    }
                    catch (Exception exception) when (exception is WebSocketException or OperationCanceledException or IOException or ObjectDisposedException)
                    {
                        // Expected while the server is down: fall through to the back-off.
                    }
                    finally
                    {
                        connection.Cancel(); // stops this connection's send loop on every exit path
                        connected = false;
                        statusChanges.Enqueue(ConnectionStatus.Disconnected);
                        if (socket.State == WebSocketState.Open) socket.Abort();
                    }
                }

                // Another VR instance took over: wait longer so two clients never fight for the slot.
                double delay = replaced ? 15.0 : Math.Min(settings.maxReconnectDelaySeconds, 0.5 * Math.Pow(2, attempt++));
                await IgnoreCancellation(Task.Delay(TimeSpan.FromSeconds(delay), token));
            }
        }

        private async Task ReceiveLoopAsync(ClientWebSocket socket, CancellationToken token)
        {
            var buffer = new byte[ReceiveBufferSize];
            using var message = new MemoryStream();
            while (!token.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await IgnoreCancellation(socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None));
                    return;
                }

                message.Write(buffer, 0, result.Count);
                if (!result.EndOfMessage) continue;

                inbox.Enqueue(Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length));
                message.SetLength(0);
            }
        }

        private async Task SendLoopAsync(ClientWebSocket socket, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await outboxSignal.WaitAsync(token);
                while (outbox.TryDequeue(out string json))
                {
                    var bytes = Encoding.UTF8.GetBytes(json);
                    await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token);
                }
            }
        }

        private static async Task IgnoreCancellation(Task task)
        {
            try
            {
                await task;
            }
            catch (Exception exception) when (exception is OperationCanceledException or WebSocketException or ObjectDisposedException)
            {
            }
        }
    }
}
