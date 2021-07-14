using AutoFake.Expression;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodBody = Mono.Cecil.Cil.MethodBody;

namespace AutoFake
{
    internal class PrePostProcessor : IPrePostProcessor
    {
        private const FieldAttributes ACCESS_LEVEL = FieldAttributes.Public | FieldAttributes.Static;
        private readonly ITypeInfo _typeInfo;

        public PrePostProcessor(ITypeInfo typeInfo)
        {
            _typeInfo = typeInfo;
        }

        public FieldDefinition GenerateField(string name, Type returnType)
        {
            var type = _typeInfo.ImportToFieldsAsm(returnType);
            var field = new FieldDefinition(name, ACCESS_LEVEL, type);
            _typeInfo.AddField(field);
            return field;
        }

        public FieldDefinition GenerateCallsAccumulator(string name, MethodBody method)
        {
            var type = _typeInfo.ImportToFieldsAsm(typeof(List<object[]>));
            var field = new FieldDefinition(name, ACCESS_LEVEL, type);
            _typeInfo.AddField(field);

            var fieldRef = _typeInfo.IsMultipleAssembliesMode
	            ? method.Method.Module.ImportReference(field)
	            : field;
            var ctor = typeof(List<object[]>).GetConstructor(new Type[0]);
            var ctorRef = method.Method.Module.ImportReference(ctor);

            method.Instructions.Insert(0, Instruction.Create(OpCodes.Newobj, ctorRef));
            method.Instructions.Insert(1, Instruction.Create(OpCodes.Stsfld, fieldRef));
            return field;
        }

        public void InjectVerification(IEmitter emitter, FieldDefinition setupBody, FieldDefinition executionContext)
        {
	        foreach (var instruction in emitter.Body.Instructions.Where(cmd => cmd.OpCode == OpCodes.Ret).ToList())
	        {
		        InjectVerifications(emitter, instruction, setupBody, executionContext);
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
	        VariableDefinition retValue = null;
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
		        return _typeInfo.ImportReference(methodInfo);
	        }
	        else if (returnType.Namespace == typeof(Task).Namespace && returnType.Name == "Task`1" &&
	                 returnType is GenericInstanceType genericReturnType)
	        {
		        isAsync = true;
		        var methodInfo = typeof(InvocationExpression).GetMethod(nameof(InvocationExpression.VerifyExpectedCallsTypedAsync));
		        var open = _typeInfo.ImportReference(methodInfo);
		        var closed = new GenericInstanceMethod(open);
		        closed.GenericArguments.Add(genericReturnType.GenericArguments.Single());
		        return closed;
	        }
	        else
	        {
		        isAsync = false;
		        var methodInfo = typeof(InvocationExpression).GetMethod(nameof(InvocationExpression.VerifyExpectedCalls));
		        return _typeInfo.ImportReference(methodInfo);
	        }
        }
    }
}
