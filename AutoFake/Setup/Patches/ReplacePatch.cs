using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup;
using AutoFake.Abstractions.Setup.Patches;
using Mono.Cecil;
using Mono.Cecil.Cil;
using FieldAttributes = Mono.Cecil.FieldAttributes;

namespace AutoFake.Setup.Patches;

internal class ReplacePatch(IFieldNamePool fieldNamePool, MethodDefinition entryPoint, IPatchMember patchMember) : IPatch
{
    private readonly Lazy<FieldDefinition> _retValueField = new(() =>
    {
        var field = new FieldDefinition(fieldNamePool.NextFieldName($"{entryPoint.Name}_{patchMember.Name}_RetValue"),
            FieldAttributes.Static | FieldAttributes.Public, patchMember.ReturnType);
        entryPoint.DeclaringType.Fields.Add(field);
        return field;
    });

    public Assembly? PatchedAssembly { get; private set; }
    public TypeDefinition Type => entryPoint.DeclaringType;
    public FieldDefinition RetValueField => _retValueField.Value;
    
    public bool IsMatch(Instruction instruction) => patchMember.IsMatch(instruction);

    public void Inject(IEmitter emitter)
    {
        if (patchMember.HasThis) emitter.InsertAbove(Instruction.Create(OpCodes.Pop));
        var opCode = emitter.BaseInstruction.OpCode == OpCodes.Ldsflda || emitter.BaseInstruction.OpCode == OpCodes.Ldflda
            ? OpCodes.Ldsflda
            : OpCodes.Ldsfld;
        emitter.InsertAbove(Instruction.Create(opCode, RetValueField));
        emitter.InsertAbove(Instruction.Create(OpCodes.Br, emitter.BaseInstruction.Next));
    }

    public void LoadAssembly(AssemblyLoadContext alc)
    {
        using var asm = new MemoryStream();
        using var symbols = new MemoryStream();
        var writerParameters = new WriterParameters();
        if (Debugger.IsAttached)
        {
            Type.Module.ReadSymbols();
            writerParameters.SymbolStream = symbols;
            writerParameters.SymbolWriterProvider = new SymbolsWriterProvider();
        }

        Type.Module.Write(asm, writerParameters);
        asm.Position = symbols.Position = 0;
        PatchedAssembly = alc.LoadFromStream(asm, symbols);
    }
    
    private class SymbolsWriterProvider : ISymbolWriterProvider
    {
        public ISymbolWriter GetSymbolWriter(ModuleDefinition module, string fileName) => throw new NotSupportedException("Symbols should be added without files");

        public ISymbolWriter? GetSymbolWriter(ModuleDefinition module, Stream symbolStream)
        {
            return module.HasSymbols ? module.SymbolReader.GetWriterProvider().GetSymbolWriter(module, symbolStream) : null;
        }
    }
}