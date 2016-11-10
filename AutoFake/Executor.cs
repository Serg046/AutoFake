﻿using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AutoFake.Exceptions;
using GuardExtensions;
using AutoFake.Setup;

namespace AutoFake
{
    public class Executor<T>
    {
        private readonly ExecutorImpl _executor;

        internal Executor(GeneratedObject generatedObject, LambdaExpression invocationExpression)
        {
            _executor = new ExecutorImpl(generatedObject, invocationExpression);
        }

        public T Execute() => (T)_executor.Execute();
    }

    public class Executor
    {
        private readonly ExecutorImpl _executor;

        internal Executor(GeneratedObject generatedObject, LambdaExpression invocationExpression)
        {
            _executor = new ExecutorImpl(generatedObject, invocationExpression);
        }

        public void Execute() => _executor.Execute();
    }

    internal class ExecutorImpl
    {
        private readonly GeneratedObject _generatedObject;
        private readonly LambdaExpression _invocationExpression;

        public ExecutorImpl(GeneratedObject generatedObject, LambdaExpression invocationExpression)
        {
            Guard.AreNotNull(generatedObject, invocationExpression);
            _generatedObject = generatedObject;
            _invocationExpression = invocationExpression;
        }

        public object Execute()
        {
            SetReturnObjects();
            var visitor = new GetValueMemberVisitor(_generatedObject);
            _generatedObject.AcceptMemberVisitor(_invocationExpression.Body, visitor);
            var result = visitor.RuntimeValue;
            VerifySetups();

            return result;
        }

        private void SetReturnObjects()
        {
            foreach (var mockedMemberInfo in _generatedObject.MockedMembers.Where(m => !m.Setup.IsVoid))
            {
                var field = _generatedObject.Type.GetField(mockedMemberInfo.RetValueField.Name, BindingFlags.NonPublic | BindingFlags.Static);
                if (field == null)
                    throw new FakeGeneretingException($"'{mockedMemberInfo.RetValueField.Name}' is not found in the generated object");
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
                    var argumentChecker = mockedMemberInfo.Setup.SetupArguments[i];
                    var field = _generatedObject.Type.GetField(argumentFields[i].Name,
                        BindingFlags.NonPublic | BindingFlags.Static);

                    if (field == null)
                        throw new FakeGeneretingException($"'{argumentFields[i].Name}' is not found in the generated object");

                    var realArg = field.GetValue(null);
                    if (!argumentChecker.Check(realArg))
                        throw new VerifiableException(
                            $"Setup and real arguments are different. Expected: {argumentChecker}. Actual: {realArg}.");
                }
            }
        }

        private List<int> GetActualCallsIds(MockedMemberInfo mockedMemberInfo)
        {
            var field = _generatedObject.Type.GetField(mockedMemberInfo.ActualCallsField.Name,
                BindingFlags.NonPublic | BindingFlags.Static);
            if (field == null)
                throw new FakeGeneretingException($"'{mockedMemberInfo.ActualCallsField.Name}' is not found in the generated object");
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