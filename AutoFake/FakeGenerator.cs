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
                ApplyMock(method, mock, mocker);
                mock.AfterInjection(mocker, method.Body.GetILProcessor());
                mockedMembers.Add(mocker.MemberInfo);
            }
        }

        private void ApplyMock(MethodDefinition currentMethod, IMock mock, IMocker mocker)
        {
            if (IsAsyncMethod(currentMethod, out var asyncMethod))
            {
                ApplyMock(asyncMethod, mock, mocker);
            }

            var ilProcessor = currentMethod.Body.GetILProcessor();
            foreach (var instruction in currentMethod.Body.Instructions.ToList())
            {
                if (mock.IsSourceInstruction(mocker.TypeInfo, currentMethod.Body, instruction))
                {
                    mock.Inject(mocker, ilProcessor, instruction);
                }
                else if (instruction.Operand is MethodReference method && IsFakeAssemblyMethod(method, mocker))
                {
                    var methodDefinition = method.Resolve();
                    ApplyMock(methodDefinition, mock, mocker);
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
