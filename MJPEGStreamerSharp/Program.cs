using MJPEGStreamerSharp;

class Program
{
    static void Main(string[] args)
    {
        var imageCache = new ImageCache();
        var imageCapturer = new ImageCapturer(imageCache, 30, 1280, 720);
        var mjpegServer = new MJPEGServer(imageCache, imageCapturer);
        mjpegServer.StartRequestListener(60000);

        Thread.Sleep(-1);
    }
}
