using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Exceptions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup
{
    internal abstract class Mock
    {
        private const string ASYNC_STATE_MACHINE_ATTRIBUTE = "AsyncStateMachineAttribute";

        protected Mock(MethodInfo method, List<FakeArgument> setupArguments)
        {
            Method = method;
            SetupArguments = setupArguments;
        }

        public MethodInfo Method { get; }
        public List<FakeArgument> SetupArguments { get; }

        public abstract void PrepareForInjecting(IMocker mocker);
        public abstract void Inject(IMethodMocker methodMocker, ILProcessor ilProcessor, Instruction instruction);
        public abstract void Initialize(MockedMemberInfo mockedMemberInfo, GeneratedObject generatedObject);
        public abstract void Verify(MockedMemberInfo mockedMemberInfo, GeneratedObject generatedObject);

        public bool IsInstalledMethod(TypeInfo typeInfo, MethodReference method)
            => method.DeclaringType.FullName == typeInfo.GetInstalledMethodTypeName(this) && method.EquivalentTo(Method);

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
                    throw new FakeGeneretingException("Unexpected exception. AsyncStateMachine has several arguments or 0.");
                TypeReference generatedAsyncType = asyncAttribute.ConstructorArguments[0].Value;
                asyncMethod = generatedAsyncType.Resolve().Methods.Single(m => m.Name == "MoveNext");
                return true;
            }
            asyncMethod = null;
            return false;
        }

        protected void Verify(MockedMemberInfo mockedMemberInfo, GeneratedObject generatedObject,
            bool needCheckArguments, Func<int, bool> expectedCallsCountFunc)
        {
            if (needCheckArguments)
            {
                var ids = GetActualCallIds(mockedMemberInfo.ActualCallsField.Name, generatedObject);
                VerifyMethodArguments(mockedMemberInfo, generatedObject, ids);

                if (expectedCallsCountFunc != null)
                    VerifyExpectedCallsCount(expectedCallsCountFunc, ids.Count);
            }
            else if (expectedCallsCountFunc != null)
            {
                var actualCallsCount = GetActualCallIds(mockedMemberInfo.ActualCallsField.Name, generatedObject).Count;
                VerifyExpectedCallsCount(expectedCallsCountFunc, actualCallsCount);
            }
        }

        protected FieldInfo GetField(GeneratedObject generatedObject, string fieldName)
            => generatedObject.Type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);

        private List<int> GetActualCallIds(string actualCallsFieldName, GeneratedObject generatedObject)
        {
            var field = GetField(generatedObject, actualCallsFieldName);
            if (field == null)
                throw new FakeGeneretingException($"'{actualCallsFieldName}' is not found in the generated object");
            return (List<int>)field.GetValue(null);
        }

        private void VerifyMethodArguments(MockedMemberInfo mockedMemberInfo, GeneratedObject generatedObject, IEnumerable<int> actualCallIds)
        {
            foreach (var index in actualCallIds)
            {
                var argumentFields = mockedMemberInfo.GetArguments(index);

                if (argumentFields.Count != mockedMemberInfo.Mock.SetupArguments.Count)
                {
                    throw new FakeGeneretingException(
                        $"Installed and actual arguments count is diffrent. Installed: {mockedMemberInfo.Mock.SetupArguments.Count}, Actual: {argumentFields.Count}.");
                }

                for (var i = 0; i < argumentFields.Count; i++)
                {
                    var argumentChecker = mockedMemberInfo.Mock.SetupArguments[i];
                    var field = GetField(generatedObject, argumentFields[i].Name);
                    if (field == null)
                        throw new FakeGeneretingException($"'{argumentFields[i].Name}' is not found in the generated object");

                    var realArg = field.GetValue(null);
                    if (!argumentChecker.Check(realArg))
                        throw new VerifiableException(
                            $"Setup and real arguments are different. Runtime argument - {realArg}.");
                }
            }
        }

        private void VerifyExpectedCallsCount(Func<int, bool> expectedCallsCountFunc, int actualCallsCount)
        {
            if (expectedCallsCountFunc?.Invoke(actualCallsCount) != true)
            {
                throw new ExpectedCallsException($"Setup and actual calls count are different. Actual: {actualCallsCount}.");
            }
        }
    }
}
