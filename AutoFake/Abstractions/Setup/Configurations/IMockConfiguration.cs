using AutoFake.Setup.Mocks;
using System.Linq.Expressions;

namespace AutoFake.Abstractions.Setup.Configurations
{
	internal interface IMockConfiguration
	{
		IMockConfigurationFactory ConfigurationFactory { get; }
		AutoFake.Expression.InvocationExpression.Create ExpressionFactory { get; }
		IMockCollection MockCollection { get; }
		IMockFactory MockFactory { get; }
		ReplaceMock GetReplaceMock(LambdaExpression expression);
		ReplaceMock GetRemoveMock(LambdaExpression expression);
		VerifyMock GetVerifyMock(LambdaExpression expression);
	}
}
