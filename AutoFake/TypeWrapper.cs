using System;
using System.Linq.Expressions;
using AutoFake.Expression;
using InvocationExpression = AutoFake.Expression.InvocationExpression;

namespace AutoFake
{
    public class TypeWrapper
    {
        private readonly GeneratedObject _generatedObject;

        internal TypeWrapper(GeneratedObject generatedObject)
        {
            _generatedObject = generatedObject;
        }

        public object Execute(Expression<Action> expression)
        {
            if (!_generatedObject.IsBuilt)
            {
                _generatedObject.Build();
            }

            var invocationExpression = new InvocationExpression(expression);
            var visitor = new GetValueMemberVisitor(_generatedObject);
            invocationExpression.AcceptMemberVisitor(new TargetMemberVisitor(visitor, _generatedObject.Type));
            return visitor.RuntimeValue;
        }

        public T Execute<T>(Expression<Func<T>> expression)
        {
            if (!_generatedObject.IsBuilt)
            {
                _generatedObject.Build();
            }

            var invocationExpression = new InvocationExpression(expression);
            var visitor = new GetValueMemberVisitor(_generatedObject);
            invocationExpression.AcceptMemberVisitor(new TargetMemberVisitor(visitor, _generatedObject.Type));
            return (T)visitor.RuntimeValue;
        }
    }
}
