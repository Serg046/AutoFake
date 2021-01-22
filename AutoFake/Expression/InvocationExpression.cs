using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using AutoFake.Exceptions;
using AutoFake.Setup;
using LinqExpression = System.Linq.Expressions.Expression;

namespace AutoFake.Expression
{
    public class InvocationExpression : IInvocationExpression
    {
        private readonly LinqExpression _expression;
        private IList<IFakeArgument> _arguments;

        internal InvocationExpression(LinqExpression expression)
        {
            _expression = expression;
        }

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
            var memberVisitor = new GetSourceMemberVisitor();
            ((IInvocationExpression)this).AcceptMemberVisitor(memberVisitor);
            return memberVisitor.SourceMember;
        }

        IList<IFakeArgument> IInvocationExpression.GetArguments() => GetArguments();
        internal IList<IFakeArgument> GetArguments()
        {
            if (_arguments == null)
            {
                var visitor = new GetArgumentsMemberVisitor();
                ((IInvocationExpression) this).AcceptMemberVisitor(visitor);
                _arguments = visitor.Arguments;
            }

            return _arguments;
        }

        public void VerifyArguments(object[] currentArguments)
		{
            var fakeArguments = GetArguments();
			for (var i = 0; i < currentArguments.Length; i++)
			{
				var fakeArgument = fakeArguments[i];
				if (!fakeArgument.Check(currentArguments[i]))
				{
					throw new VerifyException(
						$"Setup and actual arguments are not matched. Expected - {fakeArgument}, actual - {EqualityArgumentChecker.ToString(currentArguments[i])}.");
				}
			}
        }

        public Task VerifyExpectedCallsAsync(Task task, ExecutionContext executionContext)
		{
            return task.ContinueWith(t => VerifyExpectedCalls(executionContext));
		}

        public Task<T> VerifyExpectedCallsTypedAsync<T>(Task<T> task, ExecutionContext executionContext)
        {
	        return task.ContinueWith(t =>
	        {
		        VerifyExpectedCalls(executionContext);
                return t.Result;
	        });
        }

        public void VerifyExpectedCalls(ExecutionContext executionContext)
        {
			if (executionContext.CallsChecker != null && !executionContext.CallsChecker(executionContext.ActualCallsNumber))
			{
				throw new ExpectedCallsException($"Setup and actual calls are not matched. Actual value - {executionContext.ActualCallsNumber}.");
			}
		}
    }
}
