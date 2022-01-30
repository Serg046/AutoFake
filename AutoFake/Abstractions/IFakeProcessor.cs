using System.Collections.Generic;
using AutoFake.Abstractions.Expression;
using AutoFake.Abstractions.Setup.Mocks;

namespace AutoFake.Abstractions
{
	internal interface IFakeProcessor
	{
		void ProcessMethod(IEnumerable<IMock> mocks, IInvocationExpression invocationExpression);
	}
}