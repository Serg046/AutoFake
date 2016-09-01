using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AutoFake.Exceptions;
using GuardExtensions;
using AutoFake.Setup;

namespace AutoFake
{
    internal class TestMethod
    {
        private readonly GeneratedObject _generatedObject;

        public TestMethod(GeneratedObject generatedObject)
        {
            Guard.IsNotNull(generatedObject);
            _generatedObject = generatedObject;
        }

        public object Execute(LambdaExpression invocationExpression)
        {
            SetReturnObjects();
            var result = ExpressionUtils.GetInvocationResult(_generatedObject, invocationExpression.Body);
            VerifySetups();

            return result;
        }

        private void SetReturnObjects()
        {
            foreach (var mockedMemberInfo in _generatedObject.MockedMembers.Where(m => !m.Setup.IsVoid))
            {
                var field = _generatedObject.Type.GetField(mockedMemberInfo.ReturnValueField.Name, BindingFlags.NonPublic | BindingFlags.Static);
                if (field == null)
                    throw new FakeGeneretingException($"'{mockedMemberInfo.ReturnValueField.Name}' is not found in the generated object");
                field.SetValue(null, mockedMemberInfo.Setup.ReturnObject);
            }
        }

        private void VerifySetups()
        {
            foreach (var mockedMemberInfo in _generatedObject.MockedMembers)
            {
                if (mockedMemberInfo.Setup.NeedCheckArguments)
                {
                    var ids = GetActualCallsIds(mockedMemberInfo);
                    VerifyMethodArguments(mockedMemberInfo, ids);

                    if (mockedMemberInfo.Setup.NeedCheckCallsCount)
                        VerifyExpectedCallsCount(mockedMemberInfo.Setup, ids.Count);
                }
                else if (mockedMemberInfo.Setup.NeedCheckCallsCount)
                {
                    var actualCallsCount = GetActualCallsIds(mockedMemberInfo).Count;
                    VerifyExpectedCallsCount(mockedMemberInfo.Setup, actualCallsCount);
                }
            }
        }

        private void VerifyMethodArguments(MockedMemberInfo mockedMemberInfo, IEnumerable<int> actualCallsIds)
        {
            foreach (var index in actualCallsIds)
            {
                var argumentFields = mockedMemberInfo.GetArguments(index);
                for (var i = 0; i < argumentFields.Count; i++)
                {
                    var setupArg = mockedMemberInfo.Setup.SetupArguments[i];
                    var field = _generatedObject.Type.GetField(argumentFields[i].Name,
                        BindingFlags.NonPublic | BindingFlags.Static);

                    if (field == null)
                        throw new FakeGeneretingException($"'{argumentFields[i].Name}' is not found in the generated object");

                    var realArg = field.GetValue(null);
                    if (!setupArg.Equals(realArg))
                        throw new VerifiableException(
                            $"Setup and real arguments are different. Expected: {setupArg}. Actual: {realArg}.");
                }
            }
        }

        private List<int> GetActualCallsIds(MockedMemberInfo mockedMemberInfo)
        {
            var field = _generatedObject.Type.GetField(mockedMemberInfo.ActualCallsIdsField.Name,
                BindingFlags.NonPublic | BindingFlags.Static);
            if (field == null)
                throw new FakeGeneretingException($"'{mockedMemberInfo.ActualCallsIdsField.Name}' is not found in the generated object");
            return (List<int>)field.GetValue(null);
        }

        private void VerifyExpectedCallsCount(FakeSetupPack setup, int actualCallsCount)
        {
            if (!setup.ExpectedCallsCountFunc(actualCallsCount))
            {
                throw new ExpectedCallsException($"Setup and actual calls count are different. Actual: {actualCallsCount}.");
            }
        }
    }
}
