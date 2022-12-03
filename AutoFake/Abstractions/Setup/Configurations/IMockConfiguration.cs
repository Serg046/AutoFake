using AutoFake.Abstractions.Setup.Mocks;
using System.Linq.Expressions;

namespace AutoFake.Abstractions.Setup.Configurations
{
	internal interface IMockConfiguration
	{
		IMockConfigurationFactory ConfigurationFactory { get; }
		AutoFake.Expression.InvocationExpression.Create ExpressionFactory { get; }
		IMockCollection MockCollection { get; }
		IMockFactory MockFactory { get; }
		IReplaceMock GetReplaceMock(LambdaExpression expression);
		IReplaceMock GetRemoveMock(LambdaExpression expression);
		IVerifyMock GetVerifyMock(LambdaExpression expression);
	}
}
