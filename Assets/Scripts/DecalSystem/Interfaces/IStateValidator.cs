namespace DecalSystem.Interfaces
{
    /// <summary>
    /// Интерфейс для валидации состояния
    /// </summary>
    public interface IStateValidator
    {
        bool IsValid();
        void ValidateOrThrow();
    }
}