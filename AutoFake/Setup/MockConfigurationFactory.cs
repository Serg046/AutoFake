using System;
using AutoFake.Abstractions.Setup;
using AutoFake.Abstractions.Setup.Configurations;
using AutoFake.Abstractions.Setup.Mocks;
using DryIoc;

namespace AutoFake.Setup
{
	internal class MockConfigurationFactory : IMockConfigurationFactory
	{
		private readonly IContainer _serviceLocator;

		public MockConfigurationFactory(IContainer serviceLocator) => _serviceLocator = serviceLocator;

		public T GetInsertMockConfiguration<T>(IMockConfiguration mockConfiguration, Action<IMock> setMock, Action closure)
			=> _serviceLocator.Resolve<Func<IMockConfiguration, Action<IMock>, Action, T>>().Invoke(mockConfiguration, setMock, closure);

		public IVerifyMockConfiguration GetVerifyMockConfiguration(IVerifyMock mock)
			=> _serviceLocator.Resolve<Func<IVerifyMock, IVerifyMockConfiguration>>().Invoke(mock);

		public T GetReplaceMockConfiguration<T>(IReplaceMock mock)
			=> _serviceLocator.Resolve<Func<IReplaceMock, T>>().Invoke(mock);

		public ISourceMemberInsertMockConfiguration<T> GetSourceMemberInsertMockConfiguration<T>(ISourceMemberInsertMock mock)
			=> _serviceLocator.Resolve<Func<ISourceMemberInsertMock, ISourceMemberInsertMockConfiguration<T>>>().Invoke(mock);
	}
}
