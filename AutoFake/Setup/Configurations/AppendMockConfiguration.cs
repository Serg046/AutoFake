using AutoFake.Setup.Mocks;
using System;
using System.Linq.Expressions;
using AutoFake.Abstractions.Setup.Configurations;
using AutoFake.Abstractions.Setup.Mocks;

namespace AutoFake.Setup.Configurations
{
	internal class AppendMockConfiguration<TSut> : IAppendMockConfiguration<TSut>
	{
		private readonly IMockConfiguration _mockConfiguration;
		private readonly Action<IMock> _setMock;
		private readonly Action _closure;

		internal AppendMockConfiguration(IMockConfiguration mockConfiguration, Action<IMock> setMock, Action closure)
		{
			_mockConfiguration = mockConfiguration;
			_setMock = setMock;
			_closure = closure;
		}

		public ISourceMemberInsertMockConfiguration<TSut> After<TOut>(Expression<Func<TSut, TOut>> expression) => AfterImpl(expression);

		public ISourceMemberInsertMockConfiguration<TSut> After(Expression<Action<TSut>> expression) => AfterImpl(expression);

		public ISourceMemberInsertMockConfiguration<TSut> After<TIn, TOut>(Expression<Func<TIn, TOut>> expression) => AfterImpl(expression);

		public ISourceMemberInsertMockConfiguration<TSut> After<TIn>(Expression<Action<TIn>> expression) => AfterImpl(expression);

		public ISourceMemberInsertMockConfiguration<TSut> After<TOut>(Expression<Func<TOut>> expression) => AfterImpl(expression);

		public ISourceMemberInsertMockConfiguration<TSut> After(Expression<Action> expression) => AfterImpl(expression);

		protected ISourceMemberInsertMockConfiguration<TSut> AfterImpl(LambdaExpression expression)
		{
			var mock = _mockConfiguration.MockFactory.GetSourceMemberInsertMock(_mockConfiguration.ExpressionFactory(expression), _closure, IInsertMock.Location.After);
			_setMock(mock);
			return _mockConfiguration.ConfigurationFactory.GetSourceMemberInsertMockConfiguration<TSut>(mock);
		}
	}
}
