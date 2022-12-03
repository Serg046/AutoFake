using System;
using System.Linq.Expressions;

namespace AutoFake.Abstractions.Setup.Configurations;

public interface IPrependMockConfiguration<TSut>
{
	ISourceMemberInsertMockConfiguration<TSut> Before<TOut>(Expression<Func<TSut, TOut>> expression);
	ISourceMemberInsertMockConfiguration<TSut> Before(Expression<Action<TSut>> expression);
	ISourceMemberInsertMockConfiguration<TSut> Before<TIn, TOut>(Expression<Func<TIn, TOut>> expression);
	ISourceMemberInsertMockConfiguration<TSut> Before<TIn>(Expression<Action<TIn>> expression);
	ISourceMemberInsertMockConfiguration<TSut> Before<TOut>(Expression<Func<TOut>> expression);
	ISourceMemberInsertMockConfiguration<TSut> Before(Expression<Action> expression);
}
