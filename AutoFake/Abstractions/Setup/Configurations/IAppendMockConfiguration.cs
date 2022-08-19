using System;
using System.Linq.Expressions;

namespace AutoFake.Abstractions.Setup.Configurations
{
	public interface IAppendMockConfiguration<TSut>
	{
		ISourceMemberInsertMockConfiguration<TSut> After<TOut>(Expression<Func<TSut, TOut>> expression);
		ISourceMemberInsertMockConfiguration<TSut> After(Expression<Action<TSut>> expression);
		ISourceMemberInsertMockConfiguration<TSut> After<TIn, TOut>(Expression<Func<TIn, TOut>> expression);
		ISourceMemberInsertMockConfiguration<TSut> After<TIn>(Expression<Action<TIn>> expression);
		ISourceMemberInsertMockConfiguration<TSut> After<TOut>(Expression<Func<TOut>> expression);
		ISourceMemberInsertMockConfiguration<TSut> After(Expression<Action> expression);
	}
}
