using AutoFake.Abstractions.Setup.Patches;

namespace AutoFake.Abstractions.Setup.Configurations;

public interface IPatchConfigurationFactory
{
    Func<IPatch,IReplacePatchConfiguration<TReturn>> CreateReplacePatchConfiguration<TReturn>();
}