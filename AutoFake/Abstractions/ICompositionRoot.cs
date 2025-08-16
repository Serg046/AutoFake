namespace AutoFake.Abstractions;

public interface ICompositionRoot : IServiceProvider
{
    void ReplaceService<T>(Func<ICompositionRoot, T> factory);
}