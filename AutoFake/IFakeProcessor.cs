using System.Collections.Generic;
using AutoFake.Expression;
using AutoFake.Setup.Mocks;

namespace AutoFake
{
	internal interface IFakeProcessor
	{
		void ProcessMethod(IEnumerable<IMock> mocks, IInvocationExpression invocationExpression);
	}
}