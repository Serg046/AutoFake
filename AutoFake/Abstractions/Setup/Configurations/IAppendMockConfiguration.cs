using System;
using System.Linq.Expressions;

namespace AutoFake.Abstractions.Setup.Configurations
{
	public interface IAppendMockConfiguration<T>
	{
		ISourceMemberInsertMockConfiguration After<TOut>(Expression<Func<T, TOut>> expression);
		ISourceMemberInsertMockConfiguration After(Expression<Action<T>> expression);
		ISourceMemberInsertMockConfiguration After<TIn, TOut>(Expression<Func<TIn, TOut>> expression);
		ISourceMemberInsertMockConfiguration After<TIn>(Expression<Action<TIn>> expression);
		ISourceMemberInsertMockConfiguration After<TOut>(Expression<Func<TOut>> expression);
		ISourceMemberInsertMockConfiguration After(Expression<Action> expression);
	}

	public interface IAppendMockConfiguration
	{
		ISourceMemberInsertMockConfiguration After<TIn, TOut>(Expression<Func<TIn, TOut>> expression);
		ISourceMemberInsertMockConfiguration After<TIn>(Expression<Action<TIn>> expression);
		ISourceMemberInsertMockConfiguration After<TOut>(Expression<Func<TOut>> expression);
		ISourceMemberInsertMockConfiguration After(Expression<Action> expression);
	}
}