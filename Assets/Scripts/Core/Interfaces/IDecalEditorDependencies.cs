namespace Fotocentr.Core
{
    public interface IDecalEditorDependencies
    {
        void Inject(DecalManager decalManager, ISceneCapture sceneCapture);
    }
}
