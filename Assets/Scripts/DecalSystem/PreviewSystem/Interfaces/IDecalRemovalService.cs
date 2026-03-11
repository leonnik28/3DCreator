namespace PreviewSystem.Interfaces
{
    /// <summary>
    /// Сервис для удаления декалей
    /// </summary>
    public interface IDecalRemovalService
    {
        void DeleteSelected();
        bool CanDeleteSelected();
    }
}