using System;
using System.Linq.Expressions;

namespace AutoFake.Abstractions.Setup.Configurations
{
	public interface IFuncMockConfiguration<TSut, TExecuteReturn>
	{
		TExecuteReturn Execute();
		IReplaceMockConfiguration<TReturn> Replace<TInput, TReturn>(Expression<Func<TInput, TReturn>> instanceSetupFunc);
		IReplaceMockConfiguration<TReturn> Replace<TReturn>(Expression<Func<TReturn>> staticSetupFunc);
		IReplaceMockConfiguration<TReturn> Replace<TReturn>(Expression<Func<TSut, TReturn>> instanceSetupFunc);
		IRemoveMockConfiguration Remove<TInput>(Expression<Action<TInput>> voidInstanceSetupFunc);
		IRemoveMockConfiguration Remove(Expression<Action> voidStaticSetupFunc);
		IRemoveMockConfiguration Remove(Expression<Action<TSut>> voidInstanceSetupFunc);
		IVerifyMockConfiguration Verify<TInput, TReturn>(Expression<Func<TInput, TReturn>> instanceSetupFunc);
		IVerifyMockConfiguration Verify<TInput>(Expression<Action<TInput>> voidInstanceSetupFunc);
		IVerifyMockConfiguration Verify<TReturn>(Expression<Func<TReturn>> staticSetupFunc);
		IVerifyMockConfiguration Verify(Expression<Action> voidStaticSetupFunc);
		IVerifyMockConfiguration Verify<TReturn>(Expression<Func<TSut, TReturn>> instanceSetupFunc);
		IVerifyMockConfiguration Verify(Expression<Action<TSut>> voidInstanceSetupFunc);
		IPrependMockConfiguration<TSut> Prepend(Action action);
		IAppendMockConfiguration<TSut> Append(Action action);
	}

	public interface IFuncMockConfiguration<TExecuteReturn>
	{
		TExecuteReturn Execute();
		IReplaceMockConfiguration<TReturn> Replace<TInput, TReturn>(Expression<Func<TInput, TReturn>> instanceSetupFunc);
		IReplaceMockConfiguration<TReturn> Replace<TReturn>(Expression<Func<TReturn>> staticSetupFunc);
		IRemoveMockConfiguration Remove<TInput>(Expression<Action<TInput>> voidInstanceSetupFunc);
		IRemoveMockConfiguration Remove(Expression<Action> voidStaticSetupFunc);
		IVerifyMockConfiguration Verify<TInput, TReturn>(Expression<Func<TInput, TReturn>> instanceSetupFunc);
		IVerifyMockConfiguration Verify<TInput>(Expression<Action<TInput>> voidInstanceSetupFunc);
		IVerifyMockConfiguration Verify<TReturn>(Expression<Func<TReturn>> staticSetupFunc);
		IVerifyMockConfiguration Verify(Expression<Action> voidStaticSetupFunc);
		IPrependMockConfiguration Prepend(Action action);
		IAppendMockConfiguration Append(Action action);
	}
}