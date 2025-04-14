using System.Reflection;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup;
using AutoFake.Abstractions.Setup.Patches;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Pure.DI;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using MethodBody = Mono.Cecil.Cil.MethodBody;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;
using static Pure.DI.Tag;

namespace AutoFake.Setup.Patches;

internal class ReplacePatch : IPatch
{
    private readonly Lazy<FieldDefinition> _retValueField;
    private MethodDefinition? _getArgumentsMethod;
    private readonly IMemberNamePool _memberNamePool;
    private readonly MethodDefinition _entryPoint;
    private readonly MethodDefinition _patchCallback;
    private readonly IPatchMember _patchMember;
    private readonly Func<MethodBody, Instruction, IEmitter> _createEmitter;
    private readonly Func<IEmitter, IByteCodeProcessor> _createProcessor;

    public ReplacePatch(IMemberNamePool memberNamePool, string patchKey, MethodDefinition entryPoint, [Tag(PatchCallback)]MethodDefinition patchCallback,
        IPatchMember patchMember, Func<MethodBody, Instruction, IEmitter> createEmitter, Func<IEmitter, IByteCodeProcessor> createProcessor)
    {
        _memberNamePool = memberNamePool;
        _entryPoint = entryPoint;
        _patchCallback = patchCallback;
        _patchMember = patchMember;
        _createEmitter = createEmitter;
        _createProcessor = createProcessor;

        Key = patchKey;
        _retValueField = new(() =>
        {
            var field = new FieldDefinition(_memberNamePool.NextFieldName($"{_entryPoint.Name}_{_patchMember.Name}_RetValue"),
                FieldAttributes.Static | FieldAttributes.Public, _patchMember.ReturnType);
            _entryPoint.DeclaringType.Fields.Add(field);
            return field;
        });
    }

    public string Key { get; }
    public Type? PatchedType { get; private set; }
    public ModuleDefinition Module => _entryPoint.DeclaringType.Module;
    public object[] Arguments { get; private set; } = [];
    public FieldDefinition? RetValueField => _retValueField.IsValueCreated ? _retValueField.Value : null;
    
    public bool IsMatch(Instruction instruction) => _patchMember.IsMatch(instruction);

    public void Inject(MethodBody method, Instruction instruction)
    {
        EnsureGetArgumentsMethodCreated();
        InjectReplacement(method, instruction);
    }
    
    private void EnsureGetArgumentsMethodCreated()
    {
        if (_getArgumentsMethod == null)
        {
            _getArgumentsMethod = GenerateGetArgumentsMethod();
            _entryPoint.DeclaringType.Methods.Add(_getArgumentsMethod);
        }
    }

    private void InjectReplacement(MethodBody method, Instruction instruction)
    {
        var nop = Instruction.Create(OpCodes.Nop);
        var emitter = _createEmitter(method, instruction);
        var processor = _createProcessor(emitter);
        var array = processor.CreateArrayVariable(Module, _patchMember.GetParameters().Count);
        var arguments = processor.ReadMethodArguments(_patchMember, array);
        ValidateArguments(emitter, array, nop);
        ReturnRetField(emitter);
        emitter.Emit(nop);
        PushRecordedArgumentsBack(emitter, arguments);
    }

    private MethodDefinition GenerateGetArgumentsMethod()
    {
        var name = _memberNamePool.NextFieldName($"{_entryPoint.Name}_{_patchMember.Name}_GetArguments");
        var method = new MethodDefinition(name, MethodAttributes.Public | MethodAttributes.Static, Module.ImportReference(typeof(object[])));
        AddParameters(method);
        AddBody(method);

        var emitter = _createEmitter(method.Body, method.Body.Instructions.Last());
        var processor = _createProcessor(emitter);
        var array = processor.CreateArrayVariable(Module, _patchMember.GetParameters().Count);
        processor.ReadMethodArguments(_patchMember, array);
        if (_patchMember.HasThis) emitter.Emit(Instruction.Create(OpCodes.Pop));
        emitter.Emit(Instruction.Create(OpCodes.Ldloc, array));
        return method;
    }

    private void AddBody(MethodDefinition method)
    {
        foreach (var cmd in _patchCallback.Body.Instructions.TakeWhile(i => !IsMatch(i)))
        {
            method.Body.Instructions.Add(cmd);
        }

        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
    }

    private void AddParameters(MethodDefinition method)
    {
        // Lambda is always an instance method which has this arg that's why Ldarg_0 points to it, static works differently
        // Ldarg_1 points to the first method argument in instance methods
        method.Parameters.Add(
            new ParameterDefinition("thisParameter", ParameterAttributes.Unused, Module.TypeSystem.Object));
        foreach (var parameter in _patchCallback.Parameters)
        {
            method.Parameters.Add(
                new ParameterDefinition(parameter.Name, parameter.Attributes, parameter.ParameterType));
        }
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
        if (_patchMember.HasThis) emitter.Emit(Instruction.Create(OpCodes.Pop));
        var opCode = emitter.BaseInstruction.OpCode == OpCodes.Ldsflda || emitter.BaseInstruction.OpCode == OpCodes.Ldflda
            ? OpCodes.Ldsflda
            : OpCodes.Ldsfld;
        emitter.Emit(Instruction.Create(opCode, _retValueField.Value));
        emitter.Emit(Instruction.Create(OpCodes.Br, emitter.BaseInstruction.Next));
    }

    private void ValidateArguments(IEmitter emitter, VariableDefinition array, Instruction nop)
    {
        emitter.Emit(Instruction.Create(OpCodes.Ldstr, Key));
        emitter.Emit(Instruction.Create(OpCodes.Ldloc, array));
        emitter.Emit(Instruction.Create(OpCodes.Call,
            Module.ImportReference(
                typeof(Fake).GetMethod(nameof(Fake.ValidateArguments)))));
        emitter.Emit(Instruction.Create(OpCodes.Brfalse, nop));
    }

    public void LoadType(Assembly assembly)
    {
        PatchedType = assembly.GetType(_entryPoint.DeclaringType.GetClrTypeFullName()) ?? throw new MissingMemberException("Cannot find a patched type");
        
        if (_getArgumentsMethod != null)
        {
            var method = PatchedType.GetMethod(_getArgumentsMethod.Name) ??
                         throw new MissingMethodException(PatchedType.FullName, _getArgumentsMethod.Name);
            var args = method.GetParameters().Select(p => CreateDefault(p.ParameterType));
            Arguments = method.Invoke(obj: null, args.ToArray()) as object[] ?? throw new InvalidOperationException("Cannot read arguments");
        }
    }
    
    private object? CreateDefault(Type type)
    {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}