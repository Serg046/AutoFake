namespace AutoFake.Abstractions.Setup.Configurations;

public interface IPatchConfiguration
{
    IReplacePatchConfiguration<TReturn> Replace<TReturn>(Func<TReturn> staticSetupFunc);
}