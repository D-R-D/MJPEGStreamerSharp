using MJPEGStreamerSharp;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Program Start.");

        var imageCache = new ImageCache();
        var imageCapturer = new ImageCapturer(imageCache, 30, 1280, 720);
        Console.WriteLine("Create ImageCauturer.");
        var mjpegServer = new MJPEGServer(imageCache, imageCapturer);
        Console.WriteLine("Create MJPEGServer.");
        mjpegServer.StartRequestListener(60000);
        Console.WriteLine("MJPEGServer Started.");

        Thread.Sleep(-1);
    }
}
