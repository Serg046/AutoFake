using System;
using System.Linq.Expressions;
using AutoFake.Expression;
using InvocationExpression = AutoFake.Expression.InvocationExpression;
using LinqExpression = System.Linq.Expressions.Expression;

namespace AutoFake
{
    public class TypeWrapper
    {
        private readonly GeneratedObject _generatedObject;

        internal TypeWrapper(GeneratedObject generatedObject)
        {
            _generatedObject = generatedObject;
        }

        public void Execute(Expression<Action> expression) => ExecuteImpl(expression);

        public T Execute<T>(Expression<Func<T>> expression) => (T)ExecuteImpl(expression);

        private object ExecuteImpl(LinqExpression expression)
        {
            if (!_generatedObject.IsBuilt) _generatedObject.Build();

            var invocationExpression = new InvocationExpression(expression);
            var visitor = new GetValueMemberVisitor(_generatedObject);
            invocationExpression.AcceptMemberVisitor(new TargetMemberVisitor(visitor, _generatedObject.Type));
            return visitor.RuntimeValue;
        }
    }
}
