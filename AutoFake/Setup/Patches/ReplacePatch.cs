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

internal class ReplacePatch(IFieldNamePool fieldNamePool, MethodDefinition entryPoint, IPatchMember patchMember, Func<IEmitter, IByteCodeProcessor> createProcessor) : IPatch
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
        var nop = Instruction.Create(OpCodes.Nop);
        var processor = createProcessor(emitter);
        var array = processor.CreateArrayVariable(Type.Module, patchMember.GetParameters().Count);
        var arguments = processor.RecordMethodCall(patchMember, array);
        ValidateArguments(emitter, array, nop);
        ReturnRetField(emitter);
        emitter.Emit(nop);
        PushRecordedArgumentsBack(emitter, arguments);
    }

    private static void PushRecordedArgumentsBack(IEmitter emitter, IReadOnlyList<VariableDefinition> arguments)
    {
        foreach (var argument in arguments)
        {
            emitter.Emit(Instruction.Create(OpCodes.Ldloc, argument));
        }
    }

    private void ReturnRetField(IEmitter emitter)
    {
        if (patchMember.HasThis) emitter.Emit(Instruction.Create(OpCodes.Pop));
        var opCode = emitter.BaseInstruction.OpCode == OpCodes.Ldsflda || emitter.BaseInstruction.OpCode == OpCodes.Ldflda
            ? OpCodes.Ldsflda
            : OpCodes.Ldsfld;
        emitter.Emit(Instruction.Create(opCode, RetValueField));
        emitter.Emit(Instruction.Create(OpCodes.Br, emitter.BaseInstruction.Next));
    }

    private void ValidateArguments(IEmitter emitter, VariableDefinition array, Instruction nop)
    {
        emitter.Emit(Instruction.Create(OpCodes.Ldloc, array));
        emitter.Emit(Instruction.Create(OpCodes.Call,
            Type.Module.ImportReference(
                typeof(Fake).GetMethod(nameof(Fake.ValidateArguments)))));
        emitter.Emit(Instruction.Create(OpCodes.Brfalse, nop));
    }

    private void ValidateArguments(IEmitter emitter)
    {
        emitter.Emit(Instruction.Create(OpCodes.Dup));
        emitter.Emit(Instruction.Create(OpCodes.Box, Type.Module.TypeSystem.Int32));
        emitter.Emit(Instruction.Create(OpCodes.Call,
            Type.Module.ImportReference(
                typeof(Fake).GetMethod(nameof(Fake.ValidateArguments)))));
        emitter.Emit(Instruction.Create(OpCodes.Brfalse, emitter.BaseInstruction));
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