using AutoFake.Abstractions;
using AutoFake.Abstractions.Expression;
using AutoFake.Abstractions.Setup;
using AutoFake.Abstractions.Setup.Configurations;
using AutoFake.Abstractions.Setup.Mocks;
using DryIoc;
using FluentAssertions;
using Sut;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Xunit;
using static System.Collections.Specialized.BitVector32;

namespace AutoFake.FunctionalTests;

public class NewVersionTests
{
	[Fact]
	public void Test()
	{
		var fake = new Fake<SystemUnderTest>();

		var sut = fake.Rewrite(f => f.GetCurrentDate());
		sut.Replace(() => DateTime.Now).Return(new DateTime(2024, 3, 13));
		sut.Execute();

		FakeContext.Run(() =>
		{
			var sys = new SystemUnderTest();
			sys.GetCurrentDate().Should().Be(new DateTime(2024, 3, 13));
		});
	}

	// to be generated
	public static class FakeContext
	{
		private static readonly AssemblyLoadContext _host;

		static FakeContext()
		{
			_host = new AssemblyLoadContext("FakeContext", isCollectible: false);
			_host.LoadFromAssemblyPath(Assembly.GetExecutingAssembly().Location);

			var date = new DateTime(2024, 3, 13);
			var fake = new Fake<SystemUnderTest>();
			fake.Services.Register<IAssemblyHost, CustomAssemblyHost>(ifAlreadyRegistered: IfAlreadyRegistered.Replace);

			var sut = fake.Rewrite(f => f.GetCurrentDate());
			sut.Replace(() => DateTime.Now).Return(date);
			sut.Execute();
		}

		public static void Run(Action action)
		{
			var actType = action.Target.GetType();
			var type = Resolve(actType);
			var instance = Activator.CreateInstance(type);
			var method = type.GetMethod(action.Method.Name, BindingFlags.Instance | BindingFlags.NonPublic);
			method.Invoke(instance, null);
		}

		private static Type Resolve(Type originalType)
		{
			var assembly = _host.Assemblies.Single(a => a.FullName == originalType.Assembly.FullName);
			return assembly.GetType(originalType.FullName);
		}

		private class CustomAssemblyHost : IAssemblyHost
		{
			public Assembly Load(MemoryStream asmStream, MemoryStream symbolsStream)
			{
				asmStream.Position = symbolsStream.Position = 0;
				return symbolsStream.Length == 0
					? _host.LoadFromStream(asmStream)
					: _host.LoadFromStream(asmStream, symbolsStream);
			}
		}
	}
}
