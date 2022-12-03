using AutoFake.Abstractions.Expression;
using AutoFake.Abstractions.Setup.Mocks;
using System.Linq.Expressions;

namespace AutoFake.Abstractions.Setup.Configurations;

public interface IMockConfiguration
{
	IMockConfigurationFactory ConfigurationFactory { get; }
	IInvocationExpression.Create ExpressionFactory { get; }
	IMockCollection MockCollection { get; }
	IMockFactory MockFactory { get; }
	IReplaceMock GetReplaceMock(LambdaExpression expression);
	IReplaceMock GetRemoveMock(LambdaExpression expression);
	IVerifyMock GetVerifyMock(LambdaExpression expression);
}
