using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace PhobiaReliefTherapy.Managers.HeartRate
{
    /// <summary>
    /// Receives BPM from tools/polar_hr_bridge.py over localhost UDP.
    /// Polled on the main thread so Unity Editor reliably sees packets.
    /// </summary>
    public class UdpHeartRateProvider : IHeartRateProvider
    {
        public string SourceName => "UDP Bridge";
        public bool IsConnected => hasRecentPacket;
        public int LatestHeartRate { get; private set; }
        public string StatusMessage { get; private set; } = "Waiting for UDP heart rate bridge...";

        private readonly int listenPort;
        private UdpClient udpClient;
        private bool hasRecentPacket;
        private float lastPacketTime;

        public UdpHeartRateProvider(int listenPort = HeartRateBleConstants.DefaultUdpPort)
        {
            this.listenPort = listenPort;
        }

        public IEnumerator Initialize(Action<bool> onComplete)
        {
            Shutdown();
            hasRecentPacket = false;
            LatestHeartRate = 0;

            try
            {
                udpClient = new UdpClient();
                udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpClient.Client.Bind(new IPEndPoint(IPAddress.Loopback, listenPort));
                udpClient.Client.Blocking = false;
            }
            catch (Exception ex)
            {
                StatusMessage = $"UDP listener failed: {ex.Message}";
                onComplete?.Invoke(false);
                yield break;
            }

            StatusMessage = $"Listening on 127.0.0.1:{listenPort}. Keep polar_hr_bridge.py running.";
            onComplete?.Invoke(true);
            yield break;
        }

        public void Shutdown()
        {
            hasRecentPacket = false;
            try
            {
                udpClient?.Close();
            }
            catch
            {
                // Ignore shutdown errors.
            }

            udpClient = null;
        }

        public void Tick()
        {
            if (udpClient == null)
                return;

            try
            {
                while (udpClient.Available > 0)
                {
                    IPEndPoint remote = new IPEndPoint(IPAddress.Loopback, 0);
                    byte[] data = udpClient.Receive(ref remote);
                    string text = Encoding.UTF8.GetString(data).Trim();

                    if (!TryParseBpm(text, out int bpm))
                        continue;

                    LatestHeartRate = HeartRateBleConstants.ClampDeviceBpm(bpm);
                    hasRecentPacket = true;
                    lastPacketTime = Time.realtimeSinceStartup;
                    StatusMessage = $"Live from Polar: {bpm} BPM";
                }
            }
            catch (SocketException)
            {
                // No packet this frame.
            }
            catch (ObjectDisposedException)
            {
                udpClient = null;
            }
            catch (Exception ex)
            {
                StatusMessage = $"UDP parse error: {ex.Message}";
            }

            if (hasRecentPacket && Time.realtimeSinceStartup - lastPacketTime > 5f)
            {
                hasRecentPacket = false;
                StatusMessage = "UDP heart rate stream paused. Check the bridge script.";
            }
        }

        private static bool TryParseBpm(string text, out int bpm)
        {
            bpm = 0;
            if (string.IsNullOrEmpty(text))
                return false;

            if (text.StartsWith("BPM:", StringComparison.OrdinalIgnoreCase))
                text = text.Substring(4).Trim();

            return int.TryParse(text, out bpm) && bpm >= HeartRateBleConstants.DeviceMinBpm && bpm <= HeartRateBleConstants.DeviceMaxBpm;
        }
    }
}
