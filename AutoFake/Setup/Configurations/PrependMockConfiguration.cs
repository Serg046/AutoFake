using AutoFake.Setup.Mocks;
using System;
using System.Linq.Expressions;
using AutoFake.Abstractions.Setup.Configurations;
using AutoFake.Abstractions.Setup.Mocks;

namespace AutoFake.Setup.Configurations
{
	internal class PrependMockConfiguration<TSut> : IPrependMockConfiguration<TSut>
	{
		private readonly IMockConfiguration _mockConfiguration;
		private readonly Action<IMock> _setMock;
		private readonly Action _closure;

		internal PrependMockConfiguration(IMockConfiguration mockConfiguration, Action<IMock> setMock, Action closure)
		{
			_mockConfiguration = mockConfiguration;
			_setMock = setMock;
			_closure = closure;
		}

		public ISourceMemberInsertMockConfiguration<TSut> Before<TOut>(Expression<Func<TSut, TOut>> expression) => BeforeImpl(expression);

		public ISourceMemberInsertMockConfiguration<TSut> Before(Expression<Action<TSut>> expression) => BeforeImpl(expression);

		public ISourceMemberInsertMockConfiguration<TSut> Before<TIn, TOut>(Expression<Func<TIn, TOut>> expression) => BeforeImpl(expression);

		public ISourceMemberInsertMockConfiguration<TSut> Before<TIn>(Expression<Action<TIn>> expression) => BeforeImpl(expression);

		public ISourceMemberInsertMockConfiguration<TSut> Before<TOut>(Expression<Func<TOut>> expression) => BeforeImpl(expression);

		public ISourceMemberInsertMockConfiguration<TSut> Before(Expression<Action> expression) => BeforeImpl(expression);


		protected ISourceMemberInsertMockConfiguration<TSut> BeforeImpl(LambdaExpression expression)
		{
			var mock = _mockConfiguration.MockFactory.GetSourceMemberInsertMock(_mockConfiguration.ExpressionFactory(expression), _closure, IInsertMock.Location.Before);
			_setMock(mock);
			return _mockConfiguration.ConfigurationFactory.GetSourceMemberInsertMockConfiguration<TSut>(mock);
		}
	}
}
