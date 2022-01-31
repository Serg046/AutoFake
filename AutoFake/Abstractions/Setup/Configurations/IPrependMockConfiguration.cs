using System;
using System.Linq.Expressions;

namespace AutoFake.Abstractions.Setup.Configurations
{
	public interface IPrependMockConfiguration<T>
	{
		ISourceMemberInsertMockConfiguration Before<TOut>(Expression<Func<T, TOut>> expression);
		ISourceMemberInsertMockConfiguration Before(Expression<Action<T>> expression);
		ISourceMemberInsertMockConfiguration Before<TIn, TOut>(Expression<Func<TIn, TOut>> expression);
		ISourceMemberInsertMockConfiguration Before<TIn>(Expression<Action<TIn>> expression);
		ISourceMemberInsertMockConfiguration Before<TOut>(Expression<Func<TOut>> expression);
		ISourceMemberInsertMockConfiguration Before(Expression<Action> expression);
	}

	public interface IPrependMockConfiguration
	{
		ISourceMemberInsertMockConfiguration Before<TIn, TOut>(Expression<Func<TIn, TOut>> expression);
		ISourceMemberInsertMockConfiguration Before<TIn>(Expression<Action<TIn>> expression);
		ISourceMemberInsertMockConfiguration Before<TOut>(Expression<Func<TOut>> expression);
		ISourceMemberInsertMockConfiguration Before(Expression<Action> expression);
	}
}