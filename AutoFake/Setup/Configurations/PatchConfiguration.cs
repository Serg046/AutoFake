using AutoFake.Abstractions.Setup;
using AutoFake.Abstractions.Setup.Configurations;

namespace AutoFake.Setup.Configurations;

internal class PatchConfiguration(IPatchCollection patchCollection, IPatchConfigurationFactory patchCfgFactory) : IPatchConfiguration
{
    public IReplacePatchConfiguration<TReturn> Replace<TReturn>(Func<TReturn> patchMember)
    {
        var patch = patchCollection.GetPatch(patchMember.Method);
        return patchCfgFactory.CreateReplacePatchConfiguration<TReturn>()(patch);
    }

    public IReplacePatchConfiguration<TReturn> Replace<TInput, TReturn>(Func<TInput, TReturn> patchMember)
    {
        var patch = patchCollection.GetPatch(patchMember.Method);
        return patchCfgFactory.CreateReplacePatchConfiguration<TReturn>()(patch);
    }
}