namespace AutoFake.Abstractions.Setup.Configurations;

public interface IReplacePatchConfiguration<TReturn>
{
    IReplacePatchConfiguration<TReturn> Return(TReturn returnObject);
}