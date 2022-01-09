using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace ShareScreen
{
    public class MjpegWriter : IDisposable
    {
        public MjpegWriter(Stream stream, string boundary = "--boundary")
        {
            Stream = stream;
            Boundary = boundary;
        }

        private string Boundary { get; set; }
        private Stream Stream { get; set; }

        public void WriteHeader()
        {
            Write("HTTP/1.1 200 OK\r\n" + "Content-Type: multipart/x-mixed-replace; boundary=" + Boundary + "\r\n");
            Stream.Flush();
        }

        public void Write(MemoryStream imageStream)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine();
            stringBuilder.AppendLine(Boundary);
            stringBuilder.AppendLine("Content-Type: image/jpeg");
            stringBuilder.AppendLine("Content-Length: " + imageStream.Length.ToString());
            stringBuilder.AppendLine();

            Write(stringBuilder.ToString());
            imageStream.WriteTo(Stream);
            Write("\r\n");

            Stream.Flush();
        }

        private void Write(string text)
        {
            var data = BytesOf(text);
            Stream.Write(data, 0, data.Length);
        }

        private static byte[] BytesOf(string text)
        {
            return Encoding.ASCII.GetBytes(text);
        }

        private static MemoryStream BytesOf(Image image)
        {
            var memoryStream = new MemoryStream();
            image.Save(memoryStream, ImageFormat.Jpeg);
            return memoryStream;
        }

        public void Dispose()
        {
            try
            {
                Stream?.Dispose();
            }
            finally
            {
                Stream = null;
            }
        }
    }
}
