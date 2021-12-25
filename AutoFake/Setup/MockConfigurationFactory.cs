using System;
using AutoFake.Setup.Configurations;
using AutoFake.Setup.Mocks;
using DryIoc;

namespace AutoFake.Setup
{
	internal class MockConfigurationFactory : IMockConfigurationFactory
	{
		private readonly IContainer _serviceLocator;

		public MockConfigurationFactory(IContainer serviceLocator) => _serviceLocator = serviceLocator;

		public T GetAppendMockConfiguration<T>(Action<IMock> setMock, Action closure) where T : AppendMockConfiguration
			=> _serviceLocator.Resolve<Func<Action<IMock>, Action, T>>().Invoke(setMock, closure);

		public T GetPrependMockConfiguration<T>(Action<IMock> setMock, Action closure) where T : PrependMockConfiguration
			=> _serviceLocator.Resolve<Func<Action<IMock>, Action, T>>().Invoke(setMock, closure);

		public VerifyMockConfiguration GetVerifyMockConfiguration(VerifyMock mock)
			=> _serviceLocator.Resolve<Func<VerifyMock, VerifyMockConfiguration>>().Invoke(mock);

		public T GetReplaceMockConfiguration<T>(ReplaceMock mock)
			=> _serviceLocator.Resolve<Func<ReplaceMock, T>>().Invoke(mock);

		public SourceMemberInsertMockConfiguration GetSourceMemberInsertMockConfiguration(SourceMemberInsertMock mock)
			=> _serviceLocator.Resolve<Func<SourceMemberInsertMock, SourceMemberInsertMockConfiguration>>().Invoke(mock);
	}
}
