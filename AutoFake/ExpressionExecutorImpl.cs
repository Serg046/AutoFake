using System.Reflection;
using AutoFake.Abstractions.Expression;
using AutoFake.Expression;

namespace AutoFake
{
	internal class ExpressionExecutorImpl
	{
		private readonly Fake _fake;
		private readonly IInvocationExpression _invocationExpression;
		private readonly IMemberVisitorFactory _memberVisitorFactory;

		public ExpressionExecutorImpl(Fake fake, IInvocationExpression invocationExpression, IMemberVisitorFactory memberVisitorFactory)
		{
			_fake = fake;
			_invocationExpression = invocationExpression;
			_memberVisitorFactory = memberVisitorFactory;
		}

		public GetValueMemberVisitor Execute()
		{
			var fakeObject = _fake.GetFakeObject();
			var visitor = _memberVisitorFactory.GetValueMemberVisitor(fakeObject.Instance);
			try
			{
				_invocationExpression.AcceptMemberVisitor(_memberVisitorFactory.GetTargetMemberVisitor(visitor, fakeObject.SourceType));
				return visitor;
			}
			catch (TargetInvocationException ex) when (ex.InnerException != null)
			{
				throw ex.InnerException;
			}
		}
	}
}
