using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AutoFake.Exceptions;
using AutoFake.Setup;
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

        public void Visit(MethodCallExpression methodExpression, MethodInfo methodInfo)
        {
            using (var setupContext = new SetupContext())
            {
                var arguments = new List<FakeArgument>();
                foreach (var argument in methodExpression.Arguments.Select(expr => GetArgument(() => expr)))
                {
                    var argumentChecker = setupContext.IsCheckerSet
                        ? setupContext.PopChecker()
                        : new EqualityArgumentChecker(argument);
                    var fakeArgument = new FakeArgument(argumentChecker);
                    arguments.Add(fakeArgument);
                }
                Arguments = arguments;
            }
        }

        private static object GetArgument(Func<LinqExpression> expressionFunc)
        {
            var expression = expressionFunc();
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

        private static object GetArgument(ConstantExpression expression) => expression.Value;

        private static object GetArgument(UnaryExpression expression) => GetArgument(() => expression.Operand);

        private static object GetArgument(LinqExpression expression)
        {
            var convertExpr = LinqExpression.Convert(expression, typeof(object));
            var lambda = LinqExpression.Lambda<Func<object>>(convertExpr);
            return lambda.Compile().Invoke();
        }
    }
}
