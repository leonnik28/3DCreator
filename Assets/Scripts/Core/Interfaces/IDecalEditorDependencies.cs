namespace Fotocentr.Core
{
    /// <summary>
    /// Зависимости для панели редактирования декалей. Позволяет инжектить сервисы.
    /// </summary>
    public interface IDecalEditorDependencies
    {
        void Inject(DecalManager decalManager, ISceneCapture sceneCapture);
    }
}
