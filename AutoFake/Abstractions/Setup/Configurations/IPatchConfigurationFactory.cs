using AutoFake.Abstractions.Setup.Patches;

namespace AutoFake.Abstractions.Setup.Configurations;

public interface IPatchConfigurationFactory
{
    IReplacePatchConfiguration<TReturn> CreateReplacePatchConfiguration<TReturn>(IPatch patch);
}