using System;
using AutoFake.Abstractions.Setup;
using AutoFake.Abstractions.Setup.Configurations;
using AutoFake.Abstractions.Setup.Mocks;
using AutoFake.Setup.Mocks;
using DryIoc;

namespace AutoFake.Setup
{
	internal class MockConfigurationFactory : IMockConfigurationFactory
	{
		private readonly IContainer _serviceLocator;

		public MockConfigurationFactory(IContainer serviceLocator) => _serviceLocator = serviceLocator;

		public T GetInsertMockConfiguration<T>(IMockConfiguration mockConfiguration, Action<IMock> setMock, Action closure)
			=> _serviceLocator.Resolve<Func<IMockConfiguration, Action<IMock>, Action, T>>().Invoke(mockConfiguration, setMock, closure);

		public IVerifyMockConfiguration GetVerifyMockConfiguration(VerifyMock mock)
			=> _serviceLocator.Resolve<Func<VerifyMock, IVerifyMockConfiguration>>().Invoke(mock);

		public T GetReplaceMockConfiguration<T>(ReplaceMock mock)
			=> _serviceLocator.Resolve<Func<ReplaceMock, T>>().Invoke(mock);

		public ISourceMemberInsertMockConfiguration<T> GetSourceMemberInsertMockConfiguration<T>(SourceMemberInsertMock mock)
			=> _serviceLocator.Resolve<Func<SourceMemberInsertMock, ISourceMemberInsertMockConfiguration<T>>>().Invoke(mock);
	}
}
