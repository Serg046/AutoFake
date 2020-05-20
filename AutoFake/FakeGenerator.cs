using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Setup.Mocks;
using Mono.Cecil;

namespace AutoFake
{
    internal class FakeGenerator
    {
        private const string ASYNC_STATE_MACHINE_ATTRIBUTE = "AsyncStateMachineAttribute";

        private readonly ITypeInfo _typeInfo;

        public FakeGenerator(ITypeInfo typeInfo)
        {
            _typeInfo = typeInfo;
        }

        public void Generate(IEnumerable<IMock> mocks, MethodBase executeFunc)
        {
            foreach (var mock in mocks)
            {
                var method = _typeInfo.Methods.Single(m => m.EquivalentTo(executeFunc));
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

            public void Rewrite() => Rewrite(_originalMethod);

            private void Rewrite(MethodDefinition currentMethod)
            {
                if (IsAsyncMethod(currentMethod, out var asyncMethod))
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
                }
            }

            private bool IsFakeAssemblyMethod(MethodReference methodReference)
                => methodReference.DeclaringType.Scope is ModuleDefinition module && module == _originalMethod.Module;

            private bool IsAsyncMethod(MethodDefinition method, out MethodDefinition asyncMethod)
            {
                //for .net 4, it is available in .net 4.5
                dynamic asyncAttribute = method.CustomAttributes
                    .SingleOrDefault(a => a.AttributeType.Name == ASYNC_STATE_MACHINE_ATTRIBUTE);
                if (asyncAttribute != null)
                {
                    TypeReference generatedAsyncType = asyncAttribute.ConstructorArguments[0].Value;
                    asyncMethod = generatedAsyncType.Resolve().Methods.Single(m => m.Name == "MoveNext");
                    return true;
                }
                asyncMethod = null;
                return false;
            }
        }
    }
}
