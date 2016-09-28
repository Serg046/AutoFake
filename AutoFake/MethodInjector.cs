using System.Linq;
using AutoFake.Exceptions;
using AutoFake.Setup;
using GuardExtensions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake
{
    internal class MethodInjector
    {
        private readonly IMethodMocker _methodMocker;
        private readonly FakeSetupPack _setup;

        public MethodInjector(IMethodMocker methodMocker)
        {
            Guard.IsNotNull(methodMocker);
            _methodMocker = methodMocker;
            _setup = methodMocker.MemberInfo.Setup;
        }

        public void Process(ILProcessor ilProcessor, Instruction instruction)
        {
            Guard.AreNotNull(ilProcessor, instruction);
            Guard.That(instruction).Satisfy(i => i.OpCode.OperandType == OperandType.InlineMethod);

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
            Guard.IsNotNull(method);

            return method.DeclaringType.FullName == _methodMocker.TypeInfo.GetInstalledMethodTypeName(_setup)
                && method.Name == _setup.Method.Name
                && IsCorrectMethodOverload(method);
        }

        private bool IsCorrectMethodOverload(MethodReference method)
            => method.Parameters.Select(p => p.ParameterType.FullName)
                .SequenceEqual(_setup.Method.GetParameters().Select(p => p.ParameterType.FullName));
    }
}
