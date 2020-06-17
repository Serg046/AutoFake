using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AutoFake.Exceptions;
using LinqExpression = System.Linq.Expressions.Expression;

namespace AutoFake.Expression
{
    internal class GetArgumentsMemberVisitor : IMemberVisitor
    {
        private IList<IFakeArgument> _arguments;
        private bool _isRuntimeValueSet;

        public IList<IFakeArgument> Arguments
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

        public void Visit(PropertyInfo propertyInfo) => Arguments = new List<IFakeArgument>();

        public void Visit(FieldInfo fieldInfo) => Arguments = new List<IFakeArgument>();

        public void Visit(NewExpression newExpression, ConstructorInfo constructorInfo)
            => Arguments = newExpression.Arguments.Select(TryGetArgument).ToList();

        public void Visit(MethodCallExpression methodExpression, MethodInfo methodInfo)
            => Arguments = methodExpression.Arguments.Select(TryGetArgument).ToList();

        [ExcludeFromCodeCoverage]
        private IFakeArgument TryGetArgument(LinqExpression expression)
        {
            switch (expression)
            {
                case ConstantExpression ce: return GetArgument(ce);
                case UnaryExpression ue: return GetArgument(ue);
                case MethodCallExpression mce: return GetArgument(mce);
                case LinqExpression le: return CreateFakeArgument(le);
                default: throw new NotSupportedExpressionException($"Invalid expression format. Type '{expression.GetType().FullName}'. Source: {expression}.");
            }
        }

        private IFakeArgument GetArgument(ConstantExpression expression) => CreateFakeArgument(expression.Value);

        private IFakeArgument GetArgument(UnaryExpression expression) => TryGetArgument(expression.Operand);

        private IFakeArgument CreateFakeArgument(LinqExpression expression)
        {
            var arg = GetArgumentInstance(expression);
            return CreateFakeArgument(arg);
        }

        private static object GetArgumentInstance(LinqExpression expression)
        {
            var convertExpr = LinqExpression.Convert(expression, typeof(object));
            var lambda = LinqExpression.Lambda<Func<object>>(convertExpr);
            return lambda.Compile().Invoke();
        }

        private IFakeArgument GetArgument(MethodCallExpression expression)
        {
            if (expression.Method.DeclaringType == typeof(Arg))
            {
                if (expression.Method.Name == nameof(Arg.IsAny))
                {
                    return new FakeArgument(new SuccessfulArgumentChecker());
                }
                else if (expression.Method.Name == nameof(Arg.Is))
                {
                    if (expression.Arguments.Count == 1)
                    {
                        var lambdaExpr = (LambdaExpression) expression.Arguments.Single();
                        var lambda = lambdaExpr.Compile();
                        var checker = new LambdaArgumentChecker(lambda);
                        return new FakeArgument(checker);
                    }
                    else if (expression.Arguments.Count == 2)
                    {
                        return CreateEqualityComparerArgument(expression);
                    }
                }
            }
            return CreateFakeArgument(expression);
        }

        private static IFakeArgument CreateEqualityComparerArgument(MethodCallExpression expression)
        {
            var instance = GetArgumentInstance(expression.Arguments[0]);
            var genericComparer = GetArgumentInstance(expression.Arguments[1]);
            var genericEqualityComparer = genericComparer.GetType().GetInterfaces()
                .Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEqualityComparer<>));
            var extension = typeof(Extensions).GetMethod(nameof(Extensions.ToNonGeneric));
            var genericExtension = extension.MakeGenericMethod(
                genericEqualityComparer.GetGenericArguments().Single());
            var comparer = genericExtension.Invoke(null, new[] {genericComparer}) as IEqualityComparer;
            return new FakeArgument(new EqualityArgumentChecker(instance, comparer));
        }

        private IFakeArgument CreateFakeArgument(object arg)
        {
            var checker = new EqualityArgumentChecker(arg);
            return new FakeArgument(checker);
        }
    }
}
