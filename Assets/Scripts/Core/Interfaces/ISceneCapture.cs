namespace Fotocentr.Core
{
    /// <summary>
    /// Абстракция съёмки сцены (скриншоты, видео).
    /// </summary>
    public interface ISceneCapture
    {
        void TakeScreenshot();
        void StartVideoRecording();
        void StopVideoRecording();
        bool IsRecording { get; }
    }
}
