using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using Size = System.Drawing.Size;

namespace ShareScreen
{
    public class StreamingServer : IDisposable
    {
        private readonly List<Socket> _clients;
        private Thread _thread;

        public StreamingServer() : this(Screen.Snapshots(1920, 1080, true))
        { }

        private StreamingServer(IEnumerable<Image> imagesSource)
        {
            _clients = new List<Socket>();
            _thread = null;

            ImagesSource = imagesSource;
            Interval = 50;
        }

        private IEnumerable<Image> ImagesSource { get; }

        private int Interval { get; }

        private bool IsRunning => _thread is { IsAlive: true };

        public void Start(int port)
        {
            lock (this)
            {
                _thread = new Thread(ServerThread) { IsBackground = true };
                _thread.Start(port);
            }
        }

        public void Stop()
        {
            if (!IsRunning) return;
            try
            {
                _thread.Abort();
            }
            finally
            {
                lock (_clients)
                {
                    foreach (var s in _clients)
                    {
                        try
                        {
                            s.Close();
                        }
                        catch
                        {
                            // ignored
                        }
                    }

                    _clients.Clear();

                }

                _thread = null;
            }
        }

        private void ServerThread(object state)
        {
            try
            {
                var server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                server.Bind(new IPEndPoint(IPAddress.Any, (int)state));
                server.Listen(10);

                foreach (var client in server.IncomingConnections())
                    ThreadPool.QueueUserWorkItem(ClientThread, client);
            }
            catch
            {
                // ignored
            }

            Stop();
        }

        private void ClientThread(object client)
        {
            var socket = (Socket)client;

            lock (_clients)
                _clients.Add(socket);

            try
            {
                using var mjpegWriter = new MjpegWriter(new NetworkStream(socket, true));
                mjpegWriter.WriteHeader();

                foreach (var imgStream in ImagesSource.Streams())
                {
                    if (Interval > 0)
                        Thread.Sleep(Interval);

                    mjpegWriter.Write(imgStream);
                }
            }
            catch
            {
                // ignored
            }
            finally
            {
                lock (_clients)
                    _clients.Remove(socket);
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }

    public static class SocketExtensions
    {
        public static IEnumerable<Socket> IncomingConnections(this Socket server)
        {
            while (true)
                yield return server.Accept();
        }
    }

    public static class Screen
    {
        public static IEnumerable<Image> Snapshots()
        {
            return Snapshots(1920, 1080, true);
        }

        public static IEnumerable<Image> Snapshots(int width, int height, bool showCursor)
        {
            var size = new Size(1920, 1080);

            var bitmap = new Bitmap(size.Width, size.Height);
            var graphics = Graphics.FromImage(bitmap);

            var scaled = (width != size.Width || height != size.Height);

            if (scaled)
            {
                bitmap = new Bitmap(width, height);
                graphics = Graphics.FromImage(bitmap);
            }

            var src = new Rectangle(0, 0, 800, 600);
            var dst = new Rectangle(0, 0, 800, height);

            while (true)
            {
                graphics.CopyFromScreen(0, 0, 0, 0, size);

                if (scaled)
                    graphics.DrawImage(bitmap, dst, src, GraphicsUnit.Pixel);

                yield return bitmap;
            }
        }

        internal static IEnumerable<MemoryStream> Streams(this IEnumerable<Image> source)
        {
            var memoryStream = new MemoryStream();

            foreach (var img in source)
            {
                memoryStream.SetLength(0);
                img.Save(memoryStream, ImageFormat.Jpeg);
                yield return memoryStream;
            }

            memoryStream.Close();
        }
    }
}
