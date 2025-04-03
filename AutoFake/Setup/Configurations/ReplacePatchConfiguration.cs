using AutoFake.Abstractions.Setup.Configurations;
using AutoFake.Abstractions.Setup.Patches;

namespace AutoFake.Setup.Configurations;

internal class ReplacePatchConfiguration<TReturn>(IPatch patch) : IReplacePatchConfiguration<TReturn>
{
    public IReplacePatchConfiguration<TReturn> Return(TReturn value)
    {
        if (patch.RetValueField != null)
        {
            var retField = patch.PatchedType?.GetField(patch.RetValueField.Name) ?? throw new MissingMemberException(patch.PatchedType?.FullName, patch.RetValueField.Name);
            retField.SetValue(obj: null, value);
        }

        return this;
    }
}