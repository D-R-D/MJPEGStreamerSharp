using System.Diagnostics;
using System.Net;
using System.Text;
using System.Linq;

namespace MJPEGStreamerSharp
{
    internal class MJPEGServer
    {
        internal ImageCache m_ImageCache;
        internal ImageCapturer m_ImageCapturer;

        private List<HttpListenerContext> clientInfo = new ();

        internal MJPEGServer(ImageCache imageCache, ImageCapturer imageCapturer)
        {
            m_ImageCache = imageCache;
            m_ImageCapturer = imageCapturer;
        }

        /// <summary>
        /// httpサーバーの接続待機処理を回す
        /// </summary>
        /// <param name="portNumber"></param>
        internal void StartRequestListener(int portNumber)
        {
            var server = new HttpListener();
            server.Prefixes.Add(@$"http://+:{portNumber}/"); // サーバーのURLを指定
            server.Start();

            // 非同期でぶん回すためのタスク
            Task.Run(async () => {
                while (true)
                {
                    // GetContextAsync()でリクエスト受け付け -> ProcessRequestAsync(context)で応答
                    // ProcessRequestAsync()での処理内容に関わらずすぐに次のGetContextAsync()が回る
                    var context = await server.GetContextAsync();
                    lock (clientInfo) 
                    {
                        clientInfo.Add(context);
                        if (!m_ImageCapturer.IsActive)
                        {
                            m_ImageCapturer.StartCapture();
                        }
                    }
                    _ = ProcessRequestAsync(context); // リクエストの処理を非同期で開始
                }
            });
        }

        /// <summary>
        /// 受信したリクエストからクエリパラメータを取り出して対応する処理を行う
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        async Task ProcessRequestAsync(HttpListenerContext context)
        {
            // 受信contextからリクエスト内容とレスポンスの作成を行う
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            // クエリパラメータを取得する
            string action = request.QueryString["action"] ?? "snapshot";
            string fps = request.QueryString["fps"] ?? "100";

            // クエリのactionパラメータごとの処理を行う
            // actionパラメータがstreamの場合以外はaction=snapshotとして処理する
            switch (action)
            {
                case "stream":
                    // クライアントへストリームを非同期で送信
                    Console.WriteLine($"[{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}] (ACCESS): action=stream");
                    await SendStreamAsync(response, fps);
                    break;
                default:
                    // クライアントへスナップショットを非同期で送信
                    Console.WriteLine($"[{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}] (ACCESS): action=snapshot");
                    await SendSnapshotAsync(response);
                    break;
            }

            // 処理完了時にresponseを閉じる
            lock (clientInfo)
            {
                clientInfo.Remove(context);
                if (clientInfo.Count <= 0)
                {
                    m_ImageCapturer.StopCapture();
                }
            }
            response.Close();
        }

        /// <summary>
        /// 指定された周期でmjpeg送信
        /// </summary>
        /// <param name="context"></param>
        /// <param name="fps"></param>
        /// <returns></returns>
        async Task SendStreamAsync(HttpListenerResponse response, string fps)
        {
            int frame;
            if (!int.TryParse(fps, out frame))
            {
                frame = 2;
            }
            frame = 1000 / frame;
            int elapsedtime;
            var stopwatch = new Stopwatch();

            response.AddHeader("Content-Type", "multipart/x-mixed-replace; boundary=--myboundary");
            response.SendChunked = true;

            while(m_ImageCache.ImageBytes == null)
            {
                Thread.Sleep(100);
            }

            using (var output = response.OutputStream)
            {
                stopwatch.Start();

                // outputを破棄せずに送信を繰り返す
                while (true)
                {
                    stopwatch.Restart();

                    byte[] mjpegheader = Encoding.UTF8.GetBytes($"\r\n--myboundary\r\nContent-Length: {m_ImageCache.ImageBytes.Length}\r\nContent-Type: image/jpeg\r\n\r\n");

                    await output.WriteAsync(mjpegheader, 0, mjpegheader.Length);
                    await output.WriteAsync(m_ImageCache.ImageBytes, 0, m_ImageCache.ImageBytes.Length);
                    await output.FlushAsync();

                    elapsedtime = (int)stopwatch.ElapsedMilliseconds;
                    if (elapsedtime < frame)
                    {
                        Thread.Sleep(frame - (int)stopwatch.ElapsedMilliseconds);
                    }
                }
            }
        }

        /// <summary>
        /// 最新のデータを単発送信
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        async Task SendSnapshotAsync(HttpListenerResponse response)
        {
            response.ContentType = "image/jpeg";

            while (m_ImageCache.ImageBytes == null) 
            {
                Thread.Sleep(100);
            }

            using (var output = response.OutputStream)
            {
                await output.WriteAsync(m_ImageCache.ImageBytes, 0, m_ImageCache.ImageBytes.Length);
                await output.FlushAsync();
            }
        }
    }
}
