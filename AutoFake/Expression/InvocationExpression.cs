using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Expression;
using AutoFake.Abstractions.Setup;
using LinqExpression = System.Linq.Expressions.Expression;

namespace AutoFake.Expression;

#pragma warning disable AF0001 // There are methods called from another assembly at runtime
public class InvocationExpression : IInvocationExpression
#pragma warning restore AF0001
{
	private readonly IMemberVisitorFactory _memberVisitorFactory;
	private readonly LinqExpression _expression;
	private IReadOnlyList<IFakeArgument>? _arguments;

	internal InvocationExpression(IMemberVisitorFactory memberVisitorFactory, LinqExpression expression)
	{
		_memberVisitorFactory = memberVisitorFactory;
		_expression = expression;
		ThrowWhenArgumentsAreNotMatched = true;
	}

	public bool ThrowWhenArgumentsAreNotMatched { get; set; }

	T IInvocationExpression.AcceptMemberVisitor<T>(IExecutableMemberVisitor<T> visitor) => AcceptMemberVisitor(visitor);
	internal T AcceptMemberVisitor<T>(IExecutableMemberVisitor<T> visitor)
	{
		return AcceptMemberVisitor(new ExecutableExpressionVisitor<T>(visitor));
	}

	T IInvocationExpression.AcceptMemberVisitor<T>(IMemberVisitor<T> visitor) => AcceptMemberVisitor(visitor);
	internal T AcceptMemberVisitor<T>(IMemberVisitor<T> visitor)
	{
		return AcceptMemberVisitor(new ExpressionVisitor<T>(visitor));
	}

	private T AcceptMemberVisitor<T>(ExecutableExpressionVisitor<T> visitor)
	{
		visitor.Visit(_expression);
		return visitor.Result.Key ? visitor.Result.Value
			: throw new NotSupportedException($"Invalid expression format. Type '{_expression.GetType().FullName}'. Source: {_expression}.");
	}

	private class ExecutableExpressionVisitor<T> : ExpressionVisitor
	{
		private readonly IExecutableMemberVisitor<T> _memberVisitor;

		public KeyValuePair<bool, T> Result { get; protected set; }

		public ExecutableExpressionVisitor(IExecutableMemberVisitor<T> memberVisitor)
		{
			_memberVisitor = memberVisitor;
		}

		protected override LinqExpression VisitMethodCall(MethodCallExpression node)
		{
			Result = new(true, _memberVisitor.Visit(node, node.Method));
			return node;
		}

		protected override LinqExpression VisitMember(MemberExpression node)
		{
			switch (node.Member)
			{
				case FieldInfo field: Result = new(true, _memberVisitor.Visit(field)); break;
				case PropertyInfo property: Result = new(true, _memberVisitor.Visit(property)); break;
			}

			return node;
		}
	}

	private class ExpressionVisitor<T> : ExecutableExpressionVisitor<T>
	{
		private readonly IMemberVisitor<T> _memberVisitor;

		public ExpressionVisitor(IMemberVisitor<T> memberVisitor) : base(memberVisitor)
		{
			_memberVisitor = memberVisitor;
		}

		protected override LinqExpression VisitNew(NewExpression node)
		{
			Result = new(true, _memberVisitor.Visit(node, node.Constructor));
			return node;
		}
	}

	//-----------------------------------------------------------------------------------------------------------

	ISourceMember IInvocationExpression.GetSourceMember() => GetSourceMember();
	internal ISourceMember GetSourceMember()
	{
		var memberVisitor = _memberVisitorFactory.GetMemberVisitor<IGetSourceMemberVisitor>();
		return ((IInvocationExpression)this).AcceptMemberVisitor(memberVisitor);
	}

	private IReadOnlyList<IFakeArgument> GetArguments()
	{
		if (_arguments == null)
		{
			var visitor = _memberVisitorFactory.GetMemberVisitor<IGetArgumentsMemberVisitor>();
			_arguments = ((IInvocationExpression)this).AcceptMemberVisitor(visitor);
		}

		return _arguments;
	}

	public bool VerifyArguments(object[] currentArguments, IExecutionContext executionContext)
	{
		if (executionContext.WhenFunc != null && !executionContext.WhenFunc())
		{
			return false;
		}

		var fakeArguments = GetArguments();
		for (var i = 0; i < currentArguments.Length; i++)
		{
			var fakeArgument = fakeArguments[i];
			if (!fakeArgument.Check(currentArguments[i]))
			{
				return ThrowWhenArgumentsAreNotMatched
					? throw new ArgumentException(
						$"Setup and actual arguments are not matched. Expected - {fakeArgument}, actual - {EqualityArgumentChecker.ToString(currentArguments[i])}.")
					: false;
			}
		}

		return true;
	}

	public Task VerifyExpectedCallsAsync(Task task, IExecutionContext executionContext)
	{
		return task.ContinueWith(t => VerifyExpectedCalls(executionContext));
	}

	public Task<T> VerifyExpectedCallsTypedAsync<T>(Task<T> task, IExecutionContext executionContext)
	{
		return task.ContinueWith(t =>
		{
			VerifyExpectedCalls(executionContext);
			return t.Result;
		});
	}

	public void VerifyExpectedCalls(IExecutionContext executionContext)
	{
		if (executionContext.CallsChecker != null && !executionContext.CallsChecker(executionContext.ActualCallsNumber))
		{
			throw new MethodAccessException($"Setup and actual calls are not matched. Actual value - {executionContext.ActualCallsNumber}.");
		}
	}
}
