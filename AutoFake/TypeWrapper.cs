﻿using System;
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
        public void Execute<TInput>(Expression<Action<TInput>> expression) => ExecuteImpl(expression);

        public TResult Execute<TResult>(Expression<Func<TResult>> expression) => (TResult)ExecuteImpl(expression);
        public TResult Execute<TInput, TResult>(Expression<Func<TInput, TResult>> expression) => (TResult)ExecuteImpl(expression);

        private object ExecuteImpl(LinqExpression expression)
        {
            if (!_generatedObject.IsBuilt) _generatedObject.Build();

            var invocationExpression = new InvocationExpression(expression);
            var visitor = new GetValueMemberVisitor(_generatedObject);
            invocationExpression.AcceptMemberVisitor(visitor);
            return visitor.RuntimeValue;
        }
    }
}
