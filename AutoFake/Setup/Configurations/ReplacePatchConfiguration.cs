using AutoFake.Abstractions.Setup.Configurations;
using AutoFake.Abstractions.Setup.Patches;

namespace AutoFake.Setup.Configurations;

internal class ReplacePatchConfiguration<TReturn>(IPatch patch) : IReplacePatchConfiguration<TReturn>
{
    public IReplacePatchConfiguration<TReturn> Return(TReturn value)
    {
        var type = patch.PatchedAssembly?.GetType(patch.Type.GetClrTypeFullName()) ?? throw new MissingMemberException("Cannot find a patched type");
        var field = type.GetField(patch.RetValueField.Name) ?? throw new MissingMemberException(type.FullName, patch.RetValueField.Name);
        field.SetValue(obj: null, value);
        return this;
    }
}