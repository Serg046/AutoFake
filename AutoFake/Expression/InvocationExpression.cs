using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Expression;
using AutoFake.Abstractions.Setup;
using AutoFake.Exceptions;
using LinqExpression = System.Linq.Expressions.Expression;

namespace AutoFake.Expression
{
#pragma warning disable AF0001 // There are methods called from another assembly at runtime
	public class InvocationExpression : IInvocationExpression
#pragma warning restore AF0001
	{
	    internal delegate IInvocationExpression Create(LinqExpression expression);

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

        void IInvocationExpression.AcceptMemberVisitor(IMemberVisitor visitor) => AcceptMemberVisitor(visitor);
        internal void AcceptMemberVisitor(IMemberVisitor visitor)
        {
	        var expressionVisitor = new ExpressionVisitor(visitor);
		    expressionVisitor.Visit(_expression);
		    if (!expressionVisitor.Visited)
		    {
			    throw new NotSupportedExpressionException(
				    $"Invalid expression format. Type '{_expression.GetType().FullName}'. Source: {_expression}.");
		    }
        }

        private class ExpressionVisitor : System.Linq.Expressions.ExpressionVisitor
        {
	        private readonly IMemberVisitor _memberVisitorHelper;

	        public ExpressionVisitor(IMemberVisitor memberVisitor)
	        {
		        _memberVisitorHelper = memberVisitor;
	        }

	        private IMemberVisitor MemberVisitor
	        {
		        get
		        {
			        Visited = true;
			        return _memberVisitorHelper;
		        }
	        }

	        public bool Visited { get; private set; }

	        protected override LinqExpression VisitNew(NewExpression node)
	        {
		        MemberVisitor.Visit(node, node.Constructor);
		        return node;
	        }

	        protected override LinqExpression VisitMethodCall(MethodCallExpression node)
	        {
		        MemberVisitor.Visit(node, node.Method);
                return node;
	        }

	        protected override LinqExpression VisitMember(MemberExpression node)
	        {
		        switch (node.Member)
		        {
			        case FieldInfo field: MemberVisitor.Visit(field); break;
			        case PropertyInfo property: MemberVisitor.Visit(property); break;
			        default: throw new NotSupportedException($"'{node.Member.GetType().FullName}' is not supported.");
		        }

		        return node;
	        }
        }

        //-----------------------------------------------------------------------------------------------------------

        ISourceMember IInvocationExpression.GetSourceMember() => GetSourceMember();
        internal ISourceMember GetSourceMember()
        {
            var memberVisitor = _memberVisitorFactory.GetMemberVisitor<IGetSourceMemberVisitor>();
            ((IInvocationExpression)this).AcceptMemberVisitor(memberVisitor);
            return memberVisitor.SourceMember;
        }

        private IReadOnlyList<IFakeArgument> GetArguments()
        {
            if (_arguments == null)
            {
                var visitor = _memberVisitorFactory.GetMemberVisitor<GetArgumentsMemberVisitor>();
                ((IInvocationExpression) this).AcceptMemberVisitor(visitor);
                _arguments = visitor.Arguments;
            }

            return _arguments;
        }

        public bool VerifyArguments(object instance, object[] currentArguments, IExecutionContext executionContext)
		{
			if (executionContext.WhenFunc != null && !executionContext.WhenFunc(instance))
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
						? throw new VerifyException(
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
				throw new ExpectedCallsException($"Setup and actual calls are not matched. Actual value - {executionContext.ActualCallsNumber}.");
			}
		}
    }
}
