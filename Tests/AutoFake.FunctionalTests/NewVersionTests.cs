using AutoFake.Abstractions;
using DryIoc;
using FluentAssertions;
using Sut;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Xunit;

namespace AutoFake.FunctionalTests;

public class NewVersionTests
{
	[Fact]
	public void Test()
	{
		FakeContext.Run(() =>
		{
			var date = new DateTime(2024, 3, 13);
			var fake = new Fake<SystemUnderTest>();

			var sut = fake.Rewrite(f => f.GetCurrentDate());
			sut.Replace(() => DateTime.Now).Return(date);
			sut.Execute().Should().Be(date);

			var sys = new SystemUnderTest();
			sys.GetCurrentDate().Should().Be(date);
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
			fake.Rewrite(f => f.GetDateVirtual()).Replace(() => DateTime.Now).Return(date);
			sut.Execute();
		}

		public static void Run(Action action)
		{
			var actType = action.Target.GetType();
			var assembly = _host.Assemblies.Single(a => a.FullName == actType.Assembly.FullName);
			var type = assembly.GetType(actType.FullName);
			var instance = Activator.CreateInstance(type);
			var method = type.GetMethod(action.Method.Name, BindingFlags.Instance | BindingFlags.NonPublic);

			// todo: get rid of that
			using (_host.EnterContextualReflection())
			{
				method.Invoke(instance, null);
			}
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
