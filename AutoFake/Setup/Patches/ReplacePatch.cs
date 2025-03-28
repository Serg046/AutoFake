using System.Reflection;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup;
using AutoFake.Abstractions.Setup.Patches;
using Mono.Cecil;
using Mono.Cecil.Cil;
using FieldAttributes = Mono.Cecil.FieldAttributes;

namespace AutoFake.Setup.Patches;

internal class ReplacePatch(IFieldNamePool fieldNamePool, MethodDefinition entryPoint, IPatchMember patchMember) : IPatch
{
    public Assembly? PatchedAssembly { get; set; }
    public TypeDefinition Type => entryPoint.DeclaringType;
    public FieldDefinition RetValueField { get; }
        = new (fieldNamePool.NextFieldName($"{entryPoint.Name}_{patchMember.Name}_RetValue"),
            FieldAttributes.Static | FieldAttributes.Public, patchMember.ReturnType);
    
    public bool IsMatch(Instruction instruction) => patchMember.IsMatch(instruction);

    public void Inject(IEmitter emitter)
    {
        Type.Fields.Add(RetValueField);
        if (patchMember.HasThis) emitter.InsertAbove(Instruction.Create(OpCodes.Pop));
        var opCode = emitter.BaseInstruction.OpCode == OpCodes.Ldsflda || emitter.BaseInstruction.OpCode == OpCodes.Ldflda
            ? OpCodes.Ldsflda
            : OpCodes.Ldsfld;
        emitter.InsertAbove(Instruction.Create(opCode, RetValueField));
        emitter.InsertAbove(Instruction.Create(OpCodes.Br, emitter.BaseInstruction.Next));
    }
}