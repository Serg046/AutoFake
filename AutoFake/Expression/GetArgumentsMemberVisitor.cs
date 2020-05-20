using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AutoFake.Exceptions;
using Microsoft.CSharp.RuntimeBinder;
using LinqExpression = System.Linq.Expressions.Expression;

namespace AutoFake.Expression
{
    internal class GetArgumentsMemberVisitor : IMemberVisitor
    {
        private IList<FakeArgument> _arguments;
        private bool _isRuntimeValueSet;

        public IList<FakeArgument> Arguments
        {
            get
            {
                if (!_isRuntimeValueSet)
                    throw new InvalidOperationException($"{nameof(Arguments)} property is not set. Please run {nameof(Visit)}() method.");
                return _arguments;
            }
            private set
            {
                _arguments = value;
                _isRuntimeValueSet = true;
            }
        }

        public void Visit(PropertyInfo propertyInfo) => Arguments = new List<FakeArgument>();

        public void Visit(FieldInfo fieldInfo) => Arguments = new List<FakeArgument>();

        public void Visit(NewExpression newExpression, ConstructorInfo constructorInfo)
            => Arguments = newExpression.Arguments.Select(TryGetArgument).ToList();

        public void Visit(MethodCallExpression methodExpression, MethodInfo methodInfo)
            => Arguments = methodExpression.Arguments.Select(TryGetArgument).ToList();

        //[ExcludeFromCodeCoverage]
        private FakeArgument TryGetArgument(LinqExpression expression)
        {
            try
            {
                return GetArgument((dynamic)expression);
            }
            catch (RuntimeBinderException)
            {
                throw new NotSupportedExpressionException(
                    $"Ivalid expression format. Type '{expression.GetType().FullName}'. Source: {expression}.");
            }
        }

        private FakeArgument GetArgument(ConstantExpression expression) => CreateFakeArgument(expression.Value);

        private FakeArgument GetArgument(UnaryExpression expression) => TryGetArgument(expression.Operand);

        private FakeArgument GetArgument(LinqExpression expression)
        {
            var convertExpr = LinqExpression.Convert(expression, typeof(object));
            var lambda = LinqExpression.Lambda<Func<object>>(convertExpr);
            var arg = lambda.Compile().Invoke();
            return CreateFakeArgument(arg);
        }

        private FakeArgument CreateFakeArgument(object arg)
        {
            var checker = new EqualityArgumentChecker(arg);
            return new FakeArgument(checker);
        }

        private FakeArgument GetArgument(MethodCallExpression expression)
        {
            if (expression.Method.DeclaringType == typeof(Arg) && expression.Method.Name == nameof(Arg.Is))
            {
                var lambdaExpr = (LambdaExpression)expression.Arguments.Single();
                var lambda = lambdaExpr.Compile();
                var checker = new LambdaChecker(lambda);
                return new FakeArgument(checker);
            }
            return GetArgument((LinqExpression)expression);
        }
    }
}
