namespace AutoFake.Abstractions;

public interface IServiceProvider
{
    T Resolve<T>();
}