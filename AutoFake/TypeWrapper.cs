using System;
using System.Linq.Expressions;
using AutoFake.Expression;
using InvocationExpression = AutoFake.Expression.InvocationExpression;
using LinqExpression = System.Linq.Expressions.Expression;

namespace AutoFake
{
    public class TypeWrapper
    {
        private readonly FakeObjectInfo _fakeObject;

        internal TypeWrapper(FakeObjectInfo fakeObject)
        {
            _fakeObject = fakeObject;
        }

        public void Execute(Expression<Action> expression) => ExecuteImpl(expression);
        public void Execute<TInput>(Expression<Action<TInput>> expression) => ExecuteImpl(expression);

        public TResult Execute<TResult>(Expression<Func<TResult>> expression) => (TResult)ExecuteImpl(expression);
        public TResult Execute<TInput, TResult>(Expression<Func<TInput, TResult>> expression) => (TResult)ExecuteImpl(expression);

        private object ExecuteImpl(LinqExpression expression)
        {
            var invocationExpression = new InvocationExpression(expression);
            var visitor = new GetValueMemberVisitor(_fakeObject);
            invocationExpression.AcceptMemberVisitor(visitor);
            return visitor.RuntimeValue;
        }
    }
}
