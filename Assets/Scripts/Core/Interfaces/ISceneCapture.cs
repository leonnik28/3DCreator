using System;

namespace Fotocentr.Core
{
    public interface ISceneCapture
    {
        void TakeScreenshot();
        void CaptureScreenshotBytes(Action<byte[]> onCaptured);
        void StartVideoRecording();
        void StopVideoRecording();
        bool IsRecording { get; }
    }
}
