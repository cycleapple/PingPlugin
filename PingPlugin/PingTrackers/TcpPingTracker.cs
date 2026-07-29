using Dalamud.Plugin.Services;
using PingPlugin.GameAddressDetectors;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PingPlugin.PingTrackers
{
    /// <summary>
    /// Measures the TCP handshake time to the active FFXIV world-server endpoint.
    /// Taiwan game servers do not answer ICMP echo requests, so ICMP cannot be used here.
    /// </summary>
    public sealed class TcpPingTracker : PingTracker
    {
        private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(2);
        private readonly IPluginLog pluginLog;

        public TcpPingTracker(PingConfiguration config, GameAddressDetector addressDetector, IPluginLog pluginLog)
            : base(config, addressDetector, PingTrackerKind.Aggregate, pluginLog)
        {
            this.pluginLog = pluginLog;
        }

        protected override async Task PingLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var endPoint = addressDetector.EndPoint;
                if (endPoint.Port > 0 && !Equals(endPoint.Address, IPAddress.Loopback))
                {
                    try
                    {
                        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                        timeout.CancelAfter(ConnectTimeout);
                        Errored = false;
                        NextRTTCalculation(await MeasureHandshakeAsync(endPoint, timeout.Token));
                    }
                    catch (OperationCanceledException) when (!token.IsCancellationRequested)
                    {
                        Errored = true;
                        if (Verbose)
                            pluginLog.Warning("TCP ping to {Endpoint} timed out.", endPoint);
                    }
                    catch (Exception exception)
                    {
                        Errored = true;
                        if (Verbose)
                            pluginLog.Warning(exception, "TCP ping to {Endpoint} failed.", endPoint);
                    }
                }

                await Task.Delay(3000, token);
            }
        }

        public static async Task<ulong> MeasureHandshakeAsync(IPEndPoint endPoint, CancellationToken token)
        {
            using var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            var stopwatch = Stopwatch.StartNew();
            await socket.ConnectAsync(endPoint, token);
            stopwatch.Stop();
            return (ulong)Math.Max(1, Math.Round(stopwatch.Elapsed.TotalMilliseconds));
        }
    }
}
