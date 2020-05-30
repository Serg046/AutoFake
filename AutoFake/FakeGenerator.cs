using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Setup.Mocks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake
{
    internal class FakeGenerator
    {
        private readonly ITypeInfo _typeInfo;

        public FakeGenerator(ITypeInfo typeInfo)
        {
            _typeInfo = typeInfo;
        }

        public void Generate(IEnumerable<IMock> mocks, MethodBase executeFunc)
        {
            foreach (var mock in mocks)
            {
                var executeFuncRef = _typeInfo.Module.ImportReference(executeFunc);
                var method = _typeInfo.Methods.Single(m => m.EquivalentTo(executeFuncRef));
                mock.BeforeInjection(method);
                var testMethod = new TestMethod(method, mock);
                testMethod.Rewrite();
                mock.AfterInjection(method.Body.GetEmitter());
            }
        }

        private class TestMethod
        {
            private readonly MethodDefinition _originalMethod;
            private readonly IMock _mock;

            public TestMethod(MethodDefinition originalMethod, IMock mock)
            {
                _originalMethod = originalMethod;
                _mock = mock;
            }

            public void Rewrite()
            {
                _mock.ProcessInstruction(Instruction.Create(OpCodes.Call, _originalMethod));
                Rewrite(_originalMethod);
            }

            private void Rewrite(MethodDefinition currentMethod)
            {
                if (currentMethod.IsAsync(out var asyncMethod))
                {
                    Rewrite(asyncMethod);
                }

                foreach (var instruction in currentMethod.Body.Instructions.ToList())
                {
                    if (_mock.IsSourceInstruction(_originalMethod, instruction))
                    {
                        _mock.Inject(currentMethod.Body.GetEmitter(), instruction);
                    }
                    else if (instruction.Operand is MethodReference method && IsFakeAssemblyMethod(method))
                    {
                        var methodDefinition = method.Resolve();
                        Rewrite(methodDefinition);
                    }
                    _mock.ProcessInstruction(instruction);
                }
            }

            private bool IsFakeAssemblyMethod(MethodReference methodReference)
                => methodReference.DeclaringType.Scope is ModuleDefinition module && module == _originalMethod.Module;
        }
    }
}
