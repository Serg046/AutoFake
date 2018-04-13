using System.Linq;
using AutoFake.Setup;
using Mono.Cecil;

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

            foreach (var instruction in currentMethod.Body.Instructions.ToList())
            {
                if (mock.IsInstalledInstruction(_mocker.TypeInfo, instruction))
                {
                    var proc = currentMethod.Body.GetILProcessor();
                    mock.Inject(_mocker, proc, instruction);
                }
                else if (instruction.Operand is MethodReference method && IsFakeAssemblyMethod(method))
                {
                    var methodDefinition = method.Resolve();
                    if (methodDefinition.IsConstructor)
                    {
                        instruction.Operand = _mocker.TypeInfo.ConvertToSourceAssembly(methodDefinition);
                    }
                    else
                    {
                        ApplyMock(methodDefinition, mock);
                    }
                }
            }
        }

        private bool IsFakeAssemblyMethod(MethodReference methodReference)
            => methodReference.DeclaringType.Scope is ModuleDefinition module && module == _mocker.TypeInfo.Module;
    }
}
