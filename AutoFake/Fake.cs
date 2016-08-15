using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace AutoFake
{
    public class Fake
    {
        public static Fake<T> For<T>(params object[] contructorArgs) => new Fake<T>(contructorArgs);
    }

    public class Fake<T>
    {
        private readonly FakeGenerator<T> _fakeGenerator;
        private string _assemblyFileName;

        internal Fake(object[] contructorArgs)
        {
            _fakeGenerator = new FakeGenerator<T>(contructorArgs);

            Setups = new List<FakeSetupPack>();
        }

        internal List<FakeSetupPack> Setups { get; }

        public FakeSetup<T, TReturn> Setup<TReturn>(Expression<Func<TReturn>> setupFunc)
        {
            return new FakeSetup<T, TReturn>(this, ExpressionUtils.GetMethodInfo(setupFunc), GetSetupArguments(setupFunc.Body));
        }

        public FakeSetup<T, TReturn> Setup<TInput, TReturn>(Expression<Func<TInput, TReturn>> setupFunc)
        {
            return new FakeSetup<T, TReturn>(this, ExpressionUtils.GetMethodInfo(setupFunc), GetSetupArguments(setupFunc.Body));
        }

        public FakeSetup<T> Setup<TInput>(Expression<Action<TInput>> setupFunc)
        {
            return new FakeSetup<T>(this, ExpressionUtils.GetMethodInfo(setupFunc), GetSetupArguments(setupFunc.Body));
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
                result = ((MethodCallExpression)expression).Arguments
                    .Select(ExpressionUtils.GetArgument).ToArray();
            }

            return result;
        }

        public void SaveFakeAssembly(string fileName) => _assemblyFileName = fileName;

        public TReturn Execute<TReturn>(Expression<Func<T, TReturn>> executeFunc)
        {
            return (TReturn)Execute((LambdaExpression)executeFunc);
        }

        public void Execute(Expression<Action<T>> executeFunc)
        {
            Execute((LambdaExpression)executeFunc);
        }

        private object Execute(LambdaExpression expression)
        {
            if (Setups.Count == 0)
                throw new InvalidOperationException("Setup pack is not found");

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
                field.SetValue(null, mockedMemberInfo.Setup.ReturnObject);
            }
        }

        private void VerifySetups(GeneratedObject generatedObject)
        {
            foreach (var mockedMemberInfo in generatedObject.MockedMembers)
            {
                if (mockedMemberInfo.Setup.ExpectedCallsCount != -1 && mockedMemberInfo.Setup.ExpectedCallsCount != mockedMemberInfo.ActualCallsCount)
                {
                    throw new InvalidOperationException(
                        $"Setup and actual calls count are different. Expected: {mockedMemberInfo.Setup.ExpectedCallsCount}. Actual: {mockedMemberInfo.ActualCallsCount}.");
                }
                if (mockedMemberInfo.Setup.IsVerifiable)
                {
                    foreach (var argumentFieldsList in mockedMemberInfo.ArgumentFields)
                    {
                        for (int i = 0; i < argumentFieldsList.Count; i++)
                        {
                            var setupArg = mockedMemberInfo.Setup.SetupArguments[i];
                            var field = generatedObject.Type.GetField(argumentFieldsList[i].Name,
                                BindingFlags.NonPublic | BindingFlags.Static);
                            var realArg = field.GetValue(null);
                            if (!setupArg.Equals(realArg))
                                throw new InvalidOperationException(
                                    $"Setup and real arguments are different. Expected: {setupArg}. Actual: {realArg}.");
                        }
                    }
                }
            }
        }

        private object GetInvocationResult(Expression executeFunc, GeneratedObject generatedObject)
        {
            object result;
            if (executeFunc is MethodCallExpression)
            {
                var methodCallExpression = (MethodCallExpression)executeFunc;
                var arguments = methodCallExpression.Arguments.Select(ExpressionUtils.GetArgument).ToArray();
                var method = generatedObject.Type.GetMethod(methodCallExpression.Method.Name,
                    methodCallExpression.Method.GetParameters().Select(p => p.ParameterType).ToArray());
                result = method.Invoke(generatedObject.Instance, arguments);
            }
            else if (executeFunc is MemberExpression)
            {
                var propInfo = ((MemberExpression)executeFunc).Member as PropertyInfo;
                var property = generatedObject.Type.GetProperty(propInfo.Name);
                result = property.GetValue(generatedObject.Instance, null);
            }
            else if (executeFunc is UnaryExpression)
            {
                result = GetInvocationResult(((UnaryExpression)executeFunc).Operand, generatedObject);
            }
            else
                throw new InvalidOperationException($"Ivalid expression format. Source: {executeFunc.ToString()}.");
            return result;
        }
    }
}
