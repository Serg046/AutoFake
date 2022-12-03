using AutoFake.Abstractions.Setup;
using AutoFake.Abstractions.Setup.Configurations;
using AutoFake.Abstractions.Setup.Mocks;
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

		public IReplaceMock GetReplaceMock(LambdaExpression expression)
		{
			var invocationExpression = ExpressionFactory(expression);
			var mock = MockFactory.GetExpressionBasedMock<IReplaceMock>(invocationExpression);
			MockCollection.Mocks.Add(mock);
			return mock;
		}

		public IReplaceMock GetRemoveMock(LambdaExpression expression)
		{
			var invocationExpression = ExpressionFactory(expression);
			var mock = MockFactory.GetExpressionBasedMock<IReplaceMock>(invocationExpression);
			MockCollection.Mocks.Add(mock);
			return mock;
		}

		public IVerifyMock GetVerifyMock(LambdaExpression expression)
		{
			var invocationExpression = ExpressionFactory(expression);
			var mock = MockFactory.GetExpressionBasedMock<IVerifyMock>(invocationExpression);
			MockCollection.Mocks.Add(mock);
			return mock;
		}
	}
}
