using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AutoFake.Setup;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake
{
    internal class FakeMethod
    {
        private readonly MethodDefinition _method;
        private readonly IMethodMocker _mocker;

        public FakeMethod(MethodDefinition method, IMethodMocker mocker)
        {
            _method = method;
            _mocker = mocker;
        }

        public void ApplyMock(IMock mock) => ApplyMock(_method, mock);

        private void ApplyMock(MethodDefinition currentMethod, IMock mock)
        {
            if (mock.IsAsyncMethod(currentMethod, out var asyncMethod))
            {
                ApplyMock(asyncMethod, mock);
            }

            var ilProcessor = currentMethod.Body.GetILProcessor();
            foreach (var instruction in currentMethod.Body.Instructions.ToList())
            {
                if (mock.IsInstalledInstruction(_mocker.TypeInfo, instruction))
                {
                    mock.Inject(_mocker, ilProcessor, instruction);
                }
                else if (instruction.Operand is MethodReference method && IsFakeAssemblyMethod(method))
                {
                    var methodDefinition = method.Resolve();
                    ApplyMock(methodDefinition, mock);
                }
            }
            _mocker.InjectVerification(ilProcessor);
        }

        private bool IsFakeAssemblyMethod(MethodReference methodReference)
            => methodReference.DeclaringType.Scope is ModuleDefinition module && module == _mocker.TypeInfo.Module;
    }
}
