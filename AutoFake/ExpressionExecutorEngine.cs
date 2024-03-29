using System;
using System.Reflection;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Expression;

namespace AutoFake;

internal class ExpressionExecutorEngine : IExpressionExecutorEngine
{
	private readonly IFakeObjectInfoSource _fakeObjInfoSource;
	private readonly IInvocationExpression _invocationExpression;
	private readonly IMemberVisitorFactory _memberVisitorFactory;

	public ExpressionExecutorEngine(IFakeObjectInfoSource fakeObjectInfoSource, IInvocationExpression invocationExpression, IMemberVisitorFactory memberVisitorFactory)
	{
		_fakeObjInfoSource = fakeObjectInfoSource;
		_invocationExpression = invocationExpression;
		_memberVisitorFactory = memberVisitorFactory;
	}

	public (Type Type, object? Value) Execute()
	{
		var fakeObject = _fakeObjInfoSource.GetFakeObject();
		var visitor = _memberVisitorFactory.GetValueMemberVisitor(fakeObject.Instance);
		try
		{
			return _invocationExpression.AcceptMemberVisitor(_memberVisitorFactory.GetTargetMemberVisitor(visitor, fakeObject.SourceType));
		}
		catch (TargetInvocationException ex) when (ex.InnerException != null)
		{
			throw ex.InnerException;
		}
	}
}
