using System.Text;

namespace MJPEGStreamerSharp
{
    internal class ImageCache
    {
        internal string? Base64ImageData { get; private set; }
        internal byte[]? ImageBytes { get; private set; }

        internal void SetNewImage(string base64ImageData)
        {
            Base64ImageData = base64ImageData;
            ImageBytes = Encoding.UTF8.GetBytes(Base64ImageData);
        }
    }
}
