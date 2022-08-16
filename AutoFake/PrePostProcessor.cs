using AutoFake.Expression;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Linq;
using System.Threading.Tasks;
using AutoFake.Abstractions;
using FieldAttributes = Mono.Cecil.FieldAttributes;

namespace AutoFake
{
    internal class PrePostProcessor : IPrePostProcessor
    {
        private const FieldAttributes AccessLevel = FieldAttributes.Public | FieldAttributes.Static;
        private readonly ITypeInfo _typeInfo;
        private readonly IAssemblyWriter _assemblyWriter;
        private readonly ICecilFactory _cecilFactory;
		private readonly IEmitterPool _emitterPool;

		public PrePostProcessor(ITypeInfo typeInfo, IAssemblyWriter assemblyWriter, ICecilFactory cecilFactory, IEmitterPool emitterPool)
        {
	        _typeInfo = typeInfo;
	        _assemblyWriter = assemblyWriter;
	        _cecilFactory = cecilFactory;
			_emitterPool = emitterPool;
		}

        public FieldDefinition GenerateField(string name, Type returnType)
        {
            var type = _typeInfo.ImportToFieldsAsm(returnType);
            var field = _cecilFactory.CreateFieldDefinition(name, AccessLevel, type);
            _assemblyWriter.AddField(field);
            return field;
        }

        public void InjectVerification(IEmitter emitter, FieldDefinition setupBody, FieldDefinition executionContext)
        {
			var stateMachineMethods = emitter.Body.Method.GetStateMachineMethods(methods => methods
				.SingleOrDefault(m => m.Name is "System.IDisposable.Dispose" or "System.IAsyncDisposable.DisposeAsync"));
			if (stateMachineMethods.SingleOrDefault() is MethodDefinition disposeMethod)
			{
				InjectVerificationToCurrentMethod(_emitterPool.GetEmitter(disposeMethod.Body), setupBody, executionContext);
			}
			else
			{
				InjectVerificationToCurrentMethod(emitter, setupBody, executionContext);
			}
		}

		private void InjectVerificationToCurrentMethod(IEmitter emitter, FieldDefinition setupBody, FieldDefinition executionContext)
		{
			foreach (var instruction in emitter.Body.Instructions.Where(cmd => cmd.OpCode == OpCodes.Ret).ToList())
			{
				InjectVerificationToCurrentMethod(emitter, emitter.ShiftDown(instruction), setupBody, executionContext);
			}
		}

		private void InjectVerificationToCurrentMethod(IEmitter emitter, Instruction retInstruction, FieldDefinition setupBody, FieldDefinition executionContext)
        {
	        FieldReference setupBodyRef = setupBody;
	        FieldReference executionContextRef = executionContext;
	        if (_typeInfo.IsMultipleAssembliesMode)
	        {
		        setupBodyRef = emitter.Body.Method.Module.ImportReference(setupBody);
		        executionContextRef = emitter.Body.Method.Module.ImportReference(executionContext);
	        }

			var verificator = GetVerificator(emitter.Body.Method, out var rewriteRetValue);
	        VariableDefinition? retValue = null;
	        if (rewriteRetValue)
	        {
		        retValue = _cecilFactory.CreateVariable(emitter.Body.Method.ReturnType);
		        emitter.Body.Variables.Add(retValue);
		        emitter.InsertBefore(retInstruction, Instruction.Create(OpCodes.Stloc, retValue));
	        }

	        emitter.InsertBefore(retInstruction, Instruction.Create(OpCodes.Ldsfld, setupBodyRef));
	        if (retValue != null) emitter.InsertBefore(retInstruction, Instruction.Create(OpCodes.Ldloc, retValue));
	        emitter.InsertBefore(retInstruction, Instruction.Create(OpCodes.Ldsfld, executionContextRef));
	        emitter.InsertBefore(retInstruction, Instruction.Create(OpCodes.Call, verificator));
        }

        private MethodReference GetVerificator(MethodReference method, out bool rewriteRetValue)
        {
	        var returnType = method.ReturnType;
	        if (returnType.FullName == typeof(Task).FullName)
	        {
				rewriteRetValue = true;
		        var methodInfo = typeof(InvocationExpression).GetMethod(nameof(InvocationExpression.VerifyExpectedCallsAsync));
		        return method.Module.ImportReference(methodInfo);
	        }
	        else if (returnType.Namespace == typeof(Task).Namespace && returnType.Name == "Task`1" &&
	                 returnType is GenericInstanceType genericTaskType)
	        {
				rewriteRetValue = true;
		        var methodInfo = typeof(InvocationExpression).GetMethod(nameof(InvocationExpression.VerifyExpectedCallsTypedAsync));
		        var open = method.Module.ImportReference(methodInfo);
		        var closed = _cecilFactory.CreateGenericInstanceMethod(open);
		        closed.GenericArguments.Add(genericTaskType.GenericArguments.Single());
		        return closed;
	        }
			else
	        {
				rewriteRetValue = false;
		        var methodInfo = typeof(InvocationExpression).GetMethod(nameof(InvocationExpression.VerifyExpectedCalls));
		        return method.Module.ImportReference(methodInfo);
	        }
        }
    }
}
