// PyClient.cs
// Handles the low-level TCP connection to the Python server.

using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class PyClient : MonoBehaviour
{
    public string host = "127.0.0.1";
    public int port = 5005;

    private TcpClient client;
    private NetworkStream stream;
    private Thread recvThread;

    // Event fired whenever a full JSON message is received from Python
    public Action<string> OnJsonReceived;

    void Start()
    {
        try
        {
            client = new TcpClient();
            client.Connect(host, port);
            stream = client.GetStream();

            // Background thread for receiving data
            recvThread = new Thread(RecvLoop) { IsBackground = true };
            recvThread.Start();

            Debug.Log($"[Unity] Connected to {host}:{port}");
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    void OnDestroy()
    {
        // Try to cleanly shut down the thread and the socket
        try { recvThread?.Interrupt(); } catch { }
        try { recvThread?.Join(100); } catch { }
        try { recvThread?.Abort(); } catch { }
        try { stream?.Close(); } catch { }
        try { client?.Close(); } catch { }
    }

    public void SendText(string text)
    {
        if (client == null || !client.Connected) return;

        var json = $"{{\"text\":\"{Escape(text)}\"}}";
        var bytes = Encoding.UTF8.GetBytes(json);

        Debug.Log("[Unity] SEND JSON -> " + json);
        try
        {
            stream.Write(bytes, 0, bytes.Length);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    private void RecvLoop()
    {
        var buf = new byte[8192];

        while (client != null && client.Connected)
        {
            try
            {
                int n = stream.Read(buf, 0, buf.Length);
                if (n <= 0) continue;

                string json = Encoding.UTF8.GetString(buf, 0, n);
                Debug.Log("[Unity] RECV JSON <- " + json);

                OnJsonReceived?.Invoke(json);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
    }

    // Very small JSON escaping helper for quotes and backslashes
    private string Escape(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
