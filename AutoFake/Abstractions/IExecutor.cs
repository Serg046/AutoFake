using System;
using System.Linq.Expressions;

namespace AutoFake.Abstractions
{
	public interface IExecutor<T>
    {
        TReturn Execute<TReturn>(Expression<Func<T, TReturn>> expression);
        void Execute(Expression<Action<T>> expression);
        TReturn Execute<TReturn>(Expression<Func<TReturn>> expression);
        void Execute(Expression<Action> expression);
    }
}
