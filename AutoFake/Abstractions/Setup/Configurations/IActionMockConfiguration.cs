using System;
using System.Linq.Expressions;

namespace AutoFake.Abstractions.Setup.Configurations
{
	public interface IActionMockConfiguration<TSut>
	{
		void Execute();
		IRemoveMockConfiguration<TSut> Remove<TInput>(Expression<Action<TInput>> voidInstanceSetupFunc);
		IRemoveMockConfiguration<TSut> Remove(Expression<Action> voidStaticSetupFunc);
		IRemoveMockConfiguration<TSut> Remove(Expression<Action<TSut>> voidInstanceSetupFunc);
		IVerifyMockConfiguration Verify<TInput, TReturn>(Expression<Func<TInput, TReturn>> instanceSetupFunc);
		IVerifyMockConfiguration Verify<TInput>(Expression<Action<TInput>> voidInstanceSetupFunc);
		IVerifyMockConfiguration Verify<TReturn>(Expression<Func<TReturn>> staticSetupFunc);
		IVerifyMockConfiguration Verify(Expression<Action> voidStaticSetupFunc);
		IVerifyMockConfiguration Verify<TReturn>(Expression<Func<TSut, TReturn>> instanceSetupFunc);
		IVerifyMockConfiguration Verify(Expression<Action<TSut>> voidInstanceSetupFunc);
		IPrependMockConfiguration<TSut> Prepend(Action action);
		IAppendMockConfiguration<TSut> Append(Action action);
		void Import<T>();
	}
}
