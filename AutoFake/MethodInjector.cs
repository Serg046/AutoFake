using System.Linq;
using AutoFake.Exceptions;
using AutoFake.Setup;
using GuardExtensions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake
{
    internal class MethodInjector : IMethodInjector
    {
        private const string ASYNC_STATE_MACHINE_ATTRIBUTE = "AsyncStateMachineAttribute";

        private readonly IMethodMocker _methodMocker;
        private readonly FakeSetupPack _setup;

        public MethodInjector(IMethodMocker methodMocker)
        {
            _methodMocker = methodMocker;
            _setup = methodMocker.MemberInfo.Setup;
        }

        public void Process(ILProcessor ilProcessor, Instruction instruction)
        {
            Guard.That(instruction).Satisfy(IsMethodInstruction);

            _methodMocker.InjectCurrentPositionSaving(ilProcessor, instruction);
            ProcessArguments(ilProcessor, instruction);

            if (!_setup.IsVerification)
            {
                var methodReference = instruction.Operand as MethodReference;
                if (methodReference == null)
                    throw new FakeGeneretingException($"Operand of the mocked instruction must be a method");
                if (!methodReference.Resolve().IsStatic)
                    _methodMocker.RemoveStackArgument(ilProcessor, instruction);

                if (_setup.IsVoid)
                    _methodMocker.RemoveInstruction(ilProcessor, instruction);
                else
                    _methodMocker.ReplaceToRetValueField(ilProcessor, instruction);
            }

            _methodMocker.MemberInfo.SourceCodeCallsCount++;
        }

        private void ProcessArguments(ILProcessor ilProcessor, Instruction instruction)
        {
            if (_setup.SetupArguments.Any())
            {
                if (_setup.NeedCheckArguments)
                {
                    var arguments = _methodMocker.PopMethodArguments(ilProcessor, instruction);
                    _methodMocker.MemberInfo.AddArguments(arguments);

                    if (_setup.IsVerification)
                        _methodMocker.PushMethodArguments(ilProcessor, instruction, arguments);
                }
                else if (!_setup.IsVerification)
                {
                    _methodMocker.RemoveMethodArguments(ilProcessor, instruction);
                }
            }
        }

        public bool IsInstalledMethod(MethodReference method)
        {
            return method.DeclaringType.FullName == _methodMocker.TypeInfo.GetInstalledMethodTypeName(_setup)
                && method.EquivalentTo(_setup.Method);
        }

        public bool IsMethodInstruction(Instruction instruction)
            => instruction.OpCode.OperandType == OperandType.InlineMethod;

        public bool IsAsyncMethod(MethodDefinition method, out MethodDefinition asyncMethod)
        {
            //for .net 4, it is available in .net 4.5
            dynamic asyncAttribute = method.CustomAttributes
                .SingleOrDefault(a => a.AttributeType.Name == ASYNC_STATE_MACHINE_ATTRIBUTE);
            if (asyncAttribute != null)
            {
                if (asyncAttribute.ConstructorArguments.Count != 1)
                    throw new FakeGeneretingException("Unexpected exception. AsyncStateMachine have several arguments or 0.");
                TypeReference generatedAsyncType = asyncAttribute.ConstructorArguments[0].Value;
                asyncMethod = generatedAsyncType.Resolve().Methods.Single(m => m.Name == "MoveNext");
                return true;
            }
            asyncMethod = null;
            return false;
        }
    }
}
