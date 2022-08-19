using AutoFake.Setup.Mocks;
using System;
using System.Linq.Expressions;
using AutoFake.Abstractions.Setup;
using AutoFake.Abstractions.Setup.Configurations;
using AutoFake.Abstractions;

namespace AutoFake.Setup.Configurations
{
	internal class FuncMockConfiguration<TSut, TReturn> : MockConfigurations<TSut>, IFuncMockConfiguration<TSut, TReturn>
	{
		private readonly ExpressionExecutor<TReturn> _executor;

		internal FuncMockConfiguration(IMockConfiguration mockConfiguration, ITypeInfo typeInfo, ExpressionExecutor<TReturn> executor,
			Func<IMockCollection, IContractProcessor> createContractProcessor)
			: base(mockConfiguration, typeInfo, createContractProcessor)
		{
			_executor = executor;
		}

		public TReturn Execute() => _executor.Execute();
	}

	internal class ActionMockConfiguration<TSut> : MockConfigurations<TSut>, IActionMockConfiguration<TSut>
	{
		private readonly ExpressionExecutor _executor;

		internal ActionMockConfiguration(IMockConfiguration mockConfiguration, ITypeInfo typeInfo, ExpressionExecutor executor,
			Func<IMockCollection, IContractProcessor> createContractProcessor)
			: base(mockConfiguration, typeInfo, createContractProcessor)
		{
			_executor = executor;
		}

		public void Execute() => _executor.Execute();
	}

	internal abstract class MockConfigurations<TSut>
	{
		private readonly IMockConfiguration _cfg;
		private readonly ITypeInfo _typeInfo;
		private readonly Func<IMockCollection, IContractProcessor> _createContractProcessor;

		internal MockConfigurations(IMockConfiguration mockConfiguration, ITypeInfo typeInfo, Func<IMockCollection, IContractProcessor> createContractProcessor)
		{
			_cfg = mockConfiguration;
			_typeInfo = typeInfo;
			_createContractProcessor = createContractProcessor;
		}

		public IReplaceMockConfiguration<TSut, TReturn> Replace<TReturn>(Expression<Func<TSut, TReturn>> instanceSetupFunc) => ReplaceImpl<TReturn>(instanceSetupFunc);

		public IReplaceMockConfiguration<TSut, TReturn> Replace<TInput, TReturn>(Expression<Func<TInput, TReturn>> instanceSetupFunc) => ReplaceImpl<TReturn>(instanceSetupFunc);

		public IReplaceMockConfiguration<TSut, TReturn> Replace<TReturn>(Expression<Func<TReturn>> staticSetupFunc) => ReplaceImpl<TReturn>(staticSetupFunc);

		protected IReplaceMockConfiguration<TSut, TReturn> ReplaceImpl<TReturn>(LambdaExpression expression)
		{
			var mock = _cfg.GetReplaceMock(expression);
			return _cfg.ConfigurationFactory.GetReplaceMockConfiguration<IReplaceMockConfiguration<TSut, TReturn>>(mock);
		}

		public IRemoveMockConfiguration<TSut> Remove(Expression<Action<TSut>> voidInstanceSetupFunc) => RemoveImpl(voidInstanceSetupFunc);

		public IRemoveMockConfiguration<TSut> Remove<TInput>(Expression<Action<TInput>> voidInstanceSetupFunc) => RemoveImpl(voidInstanceSetupFunc);

		public IRemoveMockConfiguration<TSut> Remove(Expression<Action> voidStaticSetupFunc) => RemoveImpl(voidStaticSetupFunc);

		protected IRemoveMockConfiguration<TSut> RemoveImpl(LambdaExpression expression)
		{
			var mock = _cfg.GetRemoveMock(expression);
			return _cfg.ConfigurationFactory.GetReplaceMockConfiguration<IRemoveMockConfiguration<TSut>>(mock);
		}

		public IVerifyMockConfiguration Verify<TReturn>(Expression<Func<TSut, TReturn>> instanceSetupFunc)
			=> VerifyImpl(instanceSetupFunc);

		public IVerifyMockConfiguration Verify(Expression<Action<TSut>> voidInstanceSetupFunc)
			=> VerifyImpl(voidInstanceSetupFunc);

		public IVerifyMockConfiguration Verify<TInput, TReturn>(Expression<Func<TInput, TReturn>> instanceSetupFunc)
			=> VerifyImpl(instanceSetupFunc);

		public IVerifyMockConfiguration Verify<TInput>(Expression<Action<TInput>> voidInstanceSetupFunc)
			=> VerifyImpl(voidInstanceSetupFunc);

		public IVerifyMockConfiguration Verify<TReturn>(Expression<Func<TReturn>> staticSetupFunc)
			=> VerifyImpl(staticSetupFunc);

		public IVerifyMockConfiguration Verify(Expression<Action> voidStaticSetupFunc)
			=> VerifyImpl(voidStaticSetupFunc);

		protected IVerifyMockConfiguration VerifyImpl(LambdaExpression expression)
		{
			var mock = _cfg.GetVerifyMock(expression);
			return _cfg.ConfigurationFactory.GetVerifyMockConfiguration(mock);
		}

		public IPrependMockConfiguration<TSut> Prepend(Action action)
		{
			var position = _cfg.MockCollection.Mocks.Count;
			_cfg.MockCollection.Mocks.Add(_cfg.MockFactory.GetInsertMock(action, InsertMock.Location.Before));
			return _cfg.ConfigurationFactory.GetInsertMockConfiguration<IPrependMockConfiguration<TSut>>(_cfg, mock => _cfg.MockCollection.Mocks[position] = mock, action);
		}

		public IAppendMockConfiguration<TSut> Append(Action action)
		{
			var position = _cfg.MockCollection.Mocks.Count;
			_cfg.MockCollection.Mocks.Add(_cfg.MockFactory.GetInsertMock(action, InsertMock.Location.After));
			return _cfg.ConfigurationFactory.GetInsertMockConfiguration<IAppendMockConfiguration<TSut>>(_cfg, mock => _cfg.MockCollection.Mocks[position] = mock, action);
		}

		public void Import<T>()
		{
			var type = typeof(T);
			if (_typeInfo.SourceType.Assembly == type.Assembly)
			{
				var contractProcessor = _createContractProcessor(_cfg.MockCollection);
				contractProcessor.AddReplaceContractMocks(_typeInfo.GetTypeDefinition(type));
			}
		}
	}
}
