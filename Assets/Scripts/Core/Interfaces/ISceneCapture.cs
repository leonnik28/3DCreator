using System;

namespace Fotocentr.Core
{
    /// <summary>
    /// Абстракция съёмки сцены (скриншоты, видео).
    /// </summary>
    public interface ISceneCapture
    {
        void TakeScreenshot();
        void CaptureScreenshotBytes(Action<byte[]> onCaptured);
        void StartVideoRecording();
        void StopVideoRecording();
        bool IsRecording { get; }
    }
}
