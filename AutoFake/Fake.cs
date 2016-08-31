using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AutoFake.Exceptions;
using GuardExtensions;

namespace AutoFake
{
    public class Fake<T>
    {
        private readonly FakeGenerator _fakeGenerator;
        private string _assemblyFileName;

        public Fake(params object[] contructorArgs)
        {
            Guard.IsNotNull(contructorArgs);

            var typeInfo = new TypeInfo(typeof(T), contructorArgs);
            _fakeGenerator = new FakeGenerator(typeInfo);

            Setups = new List<FakeSetupPack>();
        }

        internal List<FakeSetupPack> Setups { get; }

        public FakeSetup<T, TReturn> Setup<TReturn>(Expression<Func<TReturn>> setupFunc)
        {
            Guard.IsNotNull(setupFunc);
            return new FakeSetup<T, TReturn>(this, ExpressionUtils.GetMethodInfo(setupFunc), GetSetupArguments(setupFunc.Body));
        }

        public FakeSetup<T, TReturn> Setup<TInput, TReturn>(Expression<Func<TInput, TReturn>> instanceSetupFunc)
        {
            Guard.IsNotNull(instanceSetupFunc);
            return new FakeSetup<T, TReturn>(this, ExpressionUtils.GetMethodInfo(instanceSetupFunc), GetSetupArguments(instanceSetupFunc.Body));
        }

        public FakeSetup<T> Setup<TInput>(Expression<Action<TInput>> voidInstanceSetupFunc)
        {
            Guard.IsNotNull(voidInstanceSetupFunc);
            return new FakeSetup<T>(this, ExpressionUtils.GetMethodInfo(voidInstanceSetupFunc), GetSetupArguments(voidInstanceSetupFunc.Body));
        }

        private object[] GetSetupArguments(Expression expression)
        {
            var result = new object[0];

            if (expression is UnaryExpression)
            {
                result = GetSetupArguments(((UnaryExpression)expression).Operand);
            }
            else if (expression is MethodCallExpression)
            {
                result = ExpressionUtils.GetArguments((MethodCallExpression)expression).ToArray();
            }

            return result;
        }

        public void SaveFakeAssembly(string fileName)
        {
            Guard.IsNotNull(fileName);
            _assemblyFileName = fileName;
        }

        public TReturn Execute<TReturn>(Expression<Func<T, TReturn>> executeFunc)
        {
            Guard.IsNotNull(executeFunc);
            return (TReturn)Execute((LambdaExpression)executeFunc);
        }

        public void Execute(Expression<Action<T>> executeFunc)
        {
            Guard.IsNotNull(executeFunc);
            Execute((LambdaExpression)executeFunc);
        }

        private object Execute(LambdaExpression expression)
        {
            if (Setups.Count == 0)
                throw new FakeGeneretingException("Setup pack is not found");

            var generatedObject = _fakeGenerator.Generate(Setups, ExpressionUtils.GetMethodInfo(expression));
            if (_assemblyFileName != null)
                _fakeGenerator.Save(_assemblyFileName);

            SetReturnObjects(generatedObject);
            var result = GetInvocationResult(expression.Body, generatedObject);
            VerifySetups(generatedObject);
            return result;
        }

        private void SetReturnObjects(GeneratedObject generatedObject)
        {
            foreach (var mockedMemberInfo in generatedObject.MockedMembers.Where(m => !m.Setup.IsVoid))
            {
                var field = generatedObject.Type.GetField(mockedMemberInfo.ReturnValueField.Name, BindingFlags.NonPublic | BindingFlags.Static);
                if (field == null)
                    throw new FakeGeneretingException($"'{mockedMemberInfo.ReturnValueField.Name}' is not found in the generated object");
                field.SetValue(null, mockedMemberInfo.Setup.ReturnObject);
            }
        }

        private void VerifySetups(GeneratedObject generatedObject)
        {
            foreach (var mockedMemberInfo in generatedObject.MockedMembers)
            {
                if (mockedMemberInfo.Setup.IsVerifiable)
                {
                    var ids = GetActualCallsIds(generatedObject, mockedMemberInfo);
                    VerifyMethodArguments(generatedObject, mockedMemberInfo, ids);

                    if (mockedMemberInfo.Setup.ExpectedCallsCount != -1)
                        VerifyExpectedCallsCount(mockedMemberInfo.Setup.ExpectedCallsCount, ids.Count);
                }
                else if (mockedMemberInfo.Setup.ExpectedCallsCount != -1)
                {
                    var actualCallsCount = GetActualCallsIds(generatedObject, mockedMemberInfo).Count;
                    VerifyExpectedCallsCount(mockedMemberInfo.Setup.ExpectedCallsCount, actualCallsCount);
                }
            }
        }

        private static void VerifyMethodArguments(GeneratedObject generatedObject, MockedMemberInfo mockedMemberInfo, IEnumerable<int> actualCallsIds)
        {
            foreach (var index in actualCallsIds)
            {
                var argumentFields = mockedMemberInfo.GetArguments(index);
                for (var i = 0; i < argumentFields.Count; i++)
                {
                    var setupArg = mockedMemberInfo.Setup.SetupArguments[i];
                    var field = generatedObject.Type.GetField(argumentFields[i].Name,
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

        private List<int> GetActualCallsIds(GeneratedObject generatedObject, MockedMemberInfo mockedMemberInfo)
        {
            var field = generatedObject.Type.GetField(mockedMemberInfo.ActualCallsIdsField.Name,
                BindingFlags.NonPublic | BindingFlags.Static);
            if (field == null)
                throw new FakeGeneretingException($"'{mockedMemberInfo.ActualCallsIdsField.Name}' is not found in the generated object");
            return (List<int>)field.GetValue(null);
        }

        private void VerifyExpectedCallsCount(int expectedCallsCount, int actualCallsCount)
        {
            if (expectedCallsCount != actualCallsCount)
                throw new ExpectedCallsException(
                    $"Setup and actual calls count are different. Expected: {expectedCallsCount}. Actual: {actualCallsCount}.");
        }

        private object GetInvocationResult(Expression executeFunc, GeneratedObject generatedObject)
        {
            object result;
            if (executeFunc is MethodCallExpression)
            {
                var methodCallExpression = (MethodCallExpression)executeFunc;
                var method = generatedObject.Type.GetMethod(methodCallExpression.Method.Name,
                    methodCallExpression.Method.GetParameters().Select(p => p.ParameterType).ToArray());

                var callExpression = Expression
                    .Call(Expression.Constant(generatedObject.Instance), method, methodCallExpression.Arguments);

                result = Expression.Lambda(callExpression).Compile().DynamicInvoke();
            }
            else if (executeFunc is MemberExpression)
            {
                var propInfo = ((MemberExpression)executeFunc).Member as PropertyInfo;
                if (propInfo == null)
                    throw new FakeGeneretingException($"Cannot execute provided expression: {executeFunc}");
                var property = generatedObject.Type.GetProperty(propInfo.Name);
                result = property.GetValue(generatedObject.Instance, null);
            }
            else if (executeFunc is UnaryExpression)
            {
                result = GetInvocationResult(((UnaryExpression)executeFunc).Operand, generatedObject);
            }
            else
                throw new NotSupportedExpressionException($"Ivalid expression format. Type {executeFunc.GetType().FullName}. Source: {executeFunc}.");
            return result;
        }
    }
}
