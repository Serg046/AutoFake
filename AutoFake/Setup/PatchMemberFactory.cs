using System.Diagnostics.CodeAnalysis;
using AutoFake.Abstractions.Setup;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Pure.DI;
using static Pure.DI.Tag;

namespace AutoFake.Setup;

internal class PatchMemberFactory(
    [Tag(Method)] Func<MethodReference, IPatchMember> createPatchMethod,
    [Tag(Constructor)] Func<MethodReference, IPatchMember> createPatchConstructor,
    [Tag(Field)] Func<FieldReference, bool, IPatchMember> createPatchField) : IPatchMemberFactory
{
    public bool TryCreatePatchMember(Instruction instruction, [NotNullWhen(true)]out IPatchMember? patchMember)
    {
        if (instruction.Operand is MethodReference methodRef)
        {
            patchMember = methodRef.Name == ".ctor" ? createPatchConstructor(methodRef) : createPatchMethod(methodRef);
            return true;
        }
        
        if (instruction.Operand is FieldReference fieldRef)
        {
            patchMember = createPatchField(fieldRef, instruction.OpCode.Code != PatchField.StaticFieldCode);
            return true;
        }

        patchMember = null;
        return false;
    }
}