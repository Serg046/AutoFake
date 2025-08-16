using System.Diagnostics.CodeAnalysis;
using AutoFake.Abstractions.Setup;
using Mono.Cecil;
using Pure.DI;
using static Pure.DI.Tag;

namespace AutoFake.Setup;

internal class PatchMemberFactory(
    [Tag(Method)] Func<MethodReference, IPatchMember> createPatchMethod,
    [Tag(Constructor)] Func<MethodReference, IPatchMember> createPatchConstructor,
    [Tag(Field)] Func<FieldReference, IPatchMember> createPatchField) : IPatchMemberFactory
{
    public bool TryCreatePatchMember(object operand, [NotNullWhen(true)]out IPatchMember? patchMember)
    {
        if (operand is MethodReference methodRef)
        {
            patchMember = methodRef.Name == ".ctor" ? createPatchConstructor(methodRef) : createPatchMethod(methodRef);
            return true;
        }
        
        if (operand is FieldReference fieldRef)
        {
            patchMember = createPatchField(fieldRef);
            return true;
        }

        patchMember = null;
        return false;
    }
}