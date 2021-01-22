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
        private const FieldAttributes ACCESS_LEVEL = FieldAttributes.Assembly | FieldAttributes.Static;
        private readonly ITypeInfo _typeInfo;

        public PrePostProcessor(ITypeInfo typeInfo)
        {
            _typeInfo = typeInfo;
        }

        public FieldDefinition GenerateField(string name, Type returnType)
        {
            var type = _typeInfo.ImportReference(returnType);
            var field = new FieldDefinition(name, ACCESS_LEVEL, type);
            _typeInfo.AddField(field);
            return field;
        }

        public void InjectVerification(IEmitter emitter, FieldDefinition setupBody, FieldDefinition executionContext)
        {
	        foreach (var instruction in emitter.Body.Instructions.Where(cmd => cmd.OpCode == OpCodes.Ret).ToList())
	        {
				InjectVerifications(emitter, instruction, setupBody, executionContext);
            }
        }

        private void InjectVerifications(IEmitter emitter, Instruction retInstruction,
	        FieldDefinition setupBody, FieldDefinition executionContext)
        {
	        var verificator = GetVerificator(emitter.Body.Method, out var isAsync);
	        VariableDefinition retValue = null;
	        if (isAsync)
	        {
		        retValue = new VariableDefinition(emitter.Body.Method.ReturnType);
		        emitter.Body.Variables.Add(retValue);
		        emitter.InsertBefore(retInstruction, Instruction.Create(OpCodes.Stloc, retValue));
	        }

            emitter.InsertBefore(retInstruction, Instruction.Create(OpCodes.Ldsfld, setupBody));
	        if (retValue != null) emitter.InsertBefore(retInstruction, Instruction.Create(OpCodes.Ldloc, retValue));
	        emitter.InsertBefore(retInstruction, Instruction.Create(OpCodes.Ldsfld, executionContext));
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
