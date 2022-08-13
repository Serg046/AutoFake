using AutoFake.Abstractions.Setup;
using AutoFake.Abstractions.Setup.Configurations;
using AutoFake.Setup.Mocks;
using System;
using System.Linq.Expressions;

namespace AutoFake.Setup.Configurations
{
	internal class MockConfiguration : IMockConfiguration
	{
		public Expression.InvocationExpression.Create ExpressionFactory { get; }
		public IMockConfigurationFactory ConfigurationFactory { get; }
		public IMockFactory MockFactory { get; }
		public IMockCollection MockCollection { get; }

		public MockConfiguration(Expression.InvocationExpression.Create expressionFactory, IMockConfigurationFactory cfgFactory, IMockFactory mockFactory, IMockCollection mockCollection)
		{
			ExpressionFactory = expressionFactory;
			ConfigurationFactory = cfgFactory;
			MockFactory = mockFactory;
			MockCollection = mockCollection;
		}

		public ReplaceMock GetReplaceMock(LambdaExpression expression)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));
			var invocationExpression = ExpressionFactory(expression);
			var mock = MockFactory.GetExpressionBasedMock<ReplaceMock>(invocationExpression);
			MockCollection.Mocks.Add(mock);
			return mock;
		}

		public ReplaceMock GetRemoveMock(LambdaExpression expression)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));
			var invocationExpression = ExpressionFactory(expression);
			var mock = MockFactory.GetExpressionBasedMock<ReplaceMock>(invocationExpression);
			MockCollection.Mocks.Add(mock);
			return mock;
		}

		public VerifyMock GetVerifyMock(LambdaExpression expression)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));
			var invocationExpression = ExpressionFactory(expression);
			var mock = MockFactory.GetExpressionBasedMock<VerifyMock>(invocationExpression);
			MockCollection.Mocks.Add(mock);
			return mock;
		}
	}
}
