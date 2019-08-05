using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Setup;
using Mono.Cecil;

namespace AutoFake
{
    internal class FakeGenerator
    {
        private const string ASYNC_STATE_MACHINE_ATTRIBUTE = "AsyncStateMachineAttribute";

        private readonly ITypeInfo _typeInfo;
        private readonly MockerFactory _mockerFactory;

        public FakeGenerator(ITypeInfo typeInfo, MockerFactory mockerFactory)
        {
            _typeInfo = typeInfo;
            _mockerFactory = mockerFactory;
        }

        public void Generate(ICollection<IMock> mocks, ICollection<MockedMemberInfo> mockedMembers, MethodBase executeFunc)
        {
            foreach (var mock in mocks)
            {
                var mocker = _mockerFactory.CreateMocker(_typeInfo, new MockedMemberInfo(mock, executeFunc.Name,
                    (byte)mockedMembers.Count(m => m.TestMethodName == executeFunc.Name)));
                mock.BeforeInjection(mocker);
                var method = _typeInfo.Methods.Single(m => m.EquivalentTo(executeFunc));
                var testMethod = new TestMethod(method, mock, mocker);
                testMethod.Rewrite();
                mock.AfterInjection(mocker, method.Body.GetILProcessor());
                mockedMembers.Add(mocker.MemberInfo);
            }
        }

        private class TestMethod
        {
            private readonly MethodDefinition _originalMethod;
            private readonly IMock _mock;
            private readonly IMocker _mocker;

            public TestMethod(MethodDefinition originalMethod, IMock mock, IMocker mocker)
            {
                _originalMethod = originalMethod;
                _mock = mock;
                _mocker = mocker;
            }

            public void Rewrite() => Rewrite(_originalMethod);

            private void Rewrite(MethodDefinition currentMethod)
            {
                if (IsAsyncMethod(currentMethod, out var asyncMethod))
                {
                    Rewrite(asyncMethod);
                }

                var ilProcessor = currentMethod.Body.GetILProcessor();
                foreach (var instruction in currentMethod.Body.Instructions.ToList())
                {
                    if (_mock.IsSourceInstruction(_mocker.TypeInfo, _originalMethod.Body, instruction))
                    {
                        _mock.Inject(_mocker, ilProcessor, instruction);
                    }
                    else if (instruction.Operand is MethodReference method && IsFakeAssemblyMethod(method, _mocker))
                    {
                        var methodDefinition = method.Resolve();
                        Rewrite(methodDefinition);
                    }
                }
            }

            private bool IsFakeAssemblyMethod(MethodReference methodReference, IMocker mocker)
                => methodReference.DeclaringType.Scope is ModuleDefinition module && module == mocker.TypeInfo.Module;

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
