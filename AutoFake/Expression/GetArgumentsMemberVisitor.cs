using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Expression;
using LinqExpression = System.Linq.Expressions.Expression;

namespace AutoFake.Expression
{
	internal class GetArgumentsMemberVisitor : IGetArgumentsMemberVisitor
	{
		private readonly Func<Delegate, LambdaArgumentChecker> _getLambdaArgChecker;
		private readonly Func<IFakeArgumentChecker, FakeArgument> _getFakeArg;
		private readonly Func<SuccessfulArgumentChecker> _getSuccessfulArgumentChecker;
		private readonly Func<object, IFakeArgumentChecker.Comparer?, EqualityArgumentChecker> _getEqualityArgumentChecker;

		public GetArgumentsMemberVisitor(
			Func<Delegate, LambdaArgumentChecker> getLambdaArgChecker,
			Func<IFakeArgumentChecker, FakeArgument> getFakeArg,
			Func<SuccessfulArgumentChecker> getSuccessfulArgumentChecker,
			Func<object, IFakeArgumentChecker.Comparer?, EqualityArgumentChecker> getEqualityArgumentChecker)
		{
			_getLambdaArgChecker = getLambdaArgChecker;
			_getFakeArg = getFakeArg;
			_getSuccessfulArgumentChecker = getSuccessfulArgumentChecker;
			_getEqualityArgumentChecker = getEqualityArgumentChecker;
		}

		public IReadOnlyList<IFakeArgument> Visit(PropertyInfo propertyInfo) => new List<IFakeArgument>();

		public IReadOnlyList<IFakeArgument> Visit(FieldInfo fieldInfo) => new List<IFakeArgument>();

		public IReadOnlyList<IFakeArgument> Visit(NewExpression newExpression, ConstructorInfo constructorInfo)
			=> newExpression.Arguments.Select(TryGetArgument).ToList();

		public IReadOnlyList<IFakeArgument> Visit(MethodCallExpression methodExpression, MethodInfo methodInfo)
			=> methodExpression.Arguments.Select(TryGetArgument).ToList();

		private IFakeArgument TryGetArgument(LinqExpression expression)
		{
			return expression switch
			{
				ConstantExpression ce => GetArgument(ce),
				UnaryExpression ue => GetArgument(ue),
				MethodCallExpression mce => GetArgument(mce),
				var le => CreateFakeArgument(le),
			};
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
				if (expression.Method.Name == nameof(Arg.Is))
				{
					if (expression.Arguments.Count == 1)
					{
						var lambdaExpr = LinqExpression.Lambda<Func<Delegate>>(expression.Arguments.Single());
						var @delegate = lambdaExpr.Compile()();
						var checker = _getLambdaArgChecker(@delegate);
						return _getFakeArg(checker);
					}
				}
				return _getFakeArg(_getSuccessfulArgumentChecker());
			}
			return CreateFakeArgument(expression);
		}

		private IFakeArgument CreateFakeArgument(object arg)
		{
			var checker = _getEqualityArgumentChecker(arg, null);
			return _getFakeArg(checker);
		}
	}
}
