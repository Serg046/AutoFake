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

		public T GetInsertMockConfiguration<T>(Action<IMock> setMock, Action closure)
			=> _serviceLocator.Resolve<Func<Action<IMock>, Action, T>>().Invoke(setMock, closure);

		public IVerifyMockConfiguration GetVerifyMockConfiguration(VerifyMock mock)
			=> _serviceLocator.Resolve<Func<VerifyMock, IVerifyMockConfiguration>>().Invoke(mock);

		public T GetReplaceMockConfiguration<T>(ReplaceMock mock)
			=> _serviceLocator.Resolve<Func<ReplaceMock, T>>().Invoke(mock);

		public ISourceMemberInsertMockConfiguration GetSourceMemberInsertMockConfiguration(SourceMemberInsertMock mock)
			=> _serviceLocator.Resolve<Func<SourceMemberInsertMock, ISourceMemberInsertMockConfiguration>>().Invoke(mock);
	}
}
