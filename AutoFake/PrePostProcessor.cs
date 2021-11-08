using AutoFake.Expression;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Linq;
using System.Threading.Tasks;
using FieldAttributes = Mono.Cecil.FieldAttributes;

namespace AutoFake
{
    internal class PrePostProcessor : IPrePostProcessor
    {
        private const FieldAttributes AccessLevel = FieldAttributes.Public | FieldAttributes.Static;
        private readonly ITypeInfo _typeInfo;
        private readonly IAssemblyWriter _assemblyWriter;

        public PrePostProcessor(ITypeInfo typeInfo, IAssemblyWriter assemblyWriter)
        {
	        _typeInfo = typeInfo;
	        _assemblyWriter = assemblyWriter;
        }

        public FieldDefinition GenerateField(string name, Type returnType)
        {
            var type = _assemblyWriter.ImportToFieldsAsm(returnType);
            var field = new FieldDefinition(name, AccessLevel, type);
            _assemblyWriter.AddField(field);
            return field;
        }

        public void InjectVerification(IEmitter emitter, FieldDefinition setupBody, FieldDefinition executionContext)
        {
	        foreach (var instruction in emitter.Body.Instructions.Where(cmd => cmd.OpCode == OpCodes.Ret).ToList())
	        {
		        InjectVerifications(emitter, emitter.ShiftDown(instruction), setupBody, executionContext);
	        }
        }

        private void InjectVerifications(IEmitter emitter, Instruction retInstruction, FieldDefinition setupBody, FieldDefinition executionContext)
        {
	        FieldReference setupBodyRef = setupBody;
	        FieldReference executionContextRef = executionContext;
	        if (_typeInfo.IsMultipleAssembliesMode)
	        {
		        setupBodyRef = emitter.Body.Method.Module.ImportReference(setupBody);
		        executionContextRef = emitter.Body.Method.Module.ImportReference(executionContext);
	        }

			var verificator = GetVerificator(emitter.Body.Method, out var isAsync);
	        VariableDefinition? retValue = null;
	        if (isAsync)
	        {
		        retValue = new VariableDefinition(emitter.Body.Method.ReturnType);
		        emitter.Body.Variables.Add(retValue);
		        emitter.InsertBefore(retInstruction, Instruction.Create(OpCodes.Stloc, retValue));
	        }

	        emitter.InsertBefore(retInstruction, Instruction.Create(OpCodes.Ldsfld, setupBodyRef));
	        if (retValue != null) emitter.InsertBefore(retInstruction, Instruction.Create(OpCodes.Ldloc, retValue));
	        emitter.InsertBefore(retInstruction, Instruction.Create(OpCodes.Ldsfld, executionContextRef));
	        emitter.InsertBefore(retInstruction, Instruction.Create(OpCodes.Call, verificator));
        }

        private MethodReference GetVerificator(MethodReference method, out bool isAsync)
        {
	        var returnType = method.ReturnType;
	        if (returnType.FullName == typeof(Task).FullName)
	        {
		        isAsync = true;
		        var methodInfo = typeof(InvocationExpression).GetMethod(nameof(InvocationExpression.VerifyExpectedCallsAsync));
		        return method.Module.ImportReference(methodInfo);
	        }
	        else if (returnType.Namespace == typeof(Task).Namespace && returnType.Name == "Task`1" &&
	                 returnType is GenericInstanceType genericReturnType)
	        {
		        isAsync = true;
		        var methodInfo = typeof(InvocationExpression).GetMethod(nameof(InvocationExpression.VerifyExpectedCallsTypedAsync));
		        var open = method.Module.ImportReference(methodInfo);
		        var closed = new GenericInstanceMethod(open);
		        closed.GenericArguments.Add(genericReturnType.GenericArguments.Single());
		        return closed;
	        }
	        else
	        {
		        isAsync = false;
		        var methodInfo = typeof(InvocationExpression).GetMethod(nameof(InvocationExpression.VerifyExpectedCalls));
		        return method.Module.ImportReference(methodInfo);
	        }
        }
    }
}
