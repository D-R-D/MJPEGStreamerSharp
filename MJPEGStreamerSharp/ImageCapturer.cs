using System.Diagnostics;

namespace MJPEGStreamerSharp
{
    internal class ImageCapturer
    {
        Process m_CaptureProcess;
        private bool m_IsActive;

        internal bool IsActive => m_IsActive;

        internal ImageCapturer(ImageCache imageCache, int frameRate, ushort width, ushort height)
        {
            // ビデオキャプチャ用のプロセスを開始
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "raspivid";
            startInfo.Arguments = $"-o - -t 0 -hf -w {width} -h {height} -fps {frameRate}"; // カメラの設定を適切に指定
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            m_CaptureProcess = new Process();
            m_CaptureProcess.StartInfo = startInfo;

            m_CaptureProcess.OutputDataReceived += new DataReceivedEventHandler((s, e) => {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    imageCache.SetNewImage(e.Data);
                }
            });
        }

        internal async Task StartCapture()
        {
            m_IsActive = true;

            _ = Task.Run(() => {
                m_CaptureProcess.Start();
                m_CaptureProcess.BeginOutputReadLine();
            });

            await Task.CompletedTask;
        }

        internal void StopCapture()
        {
            m_CaptureProcess.Kill();
            m_CaptureProcess.WaitForExit();
            m_IsActive = false;
        }
    }
}
