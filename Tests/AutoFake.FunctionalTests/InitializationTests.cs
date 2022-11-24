using AutoFake.Abstractions;
using AutoFake.Abstractions.Expression;
using DryIoc;
using FluentAssertions;
using System;
using System.Reflection;
using Xunit;

namespace AutoFake.FunctionalTests
{
	public class InitializationTests
	{
		[Fact]
		public void When_incorrect_args_Should_fail()
		{
			var fake = new Fake<TestClass>(Arg.IsNull<object>());

			Action act = () => fake.Execute(f => f.GetType());

			act.Should().Throw<MissingMethodException>().WithMessage("Constructor is not found");
		}

		[Fact]
		public void When_ambiguous_null_arg_Should_fail()
		{
			var fake = new Fake<TestClass>(Arg.IsNull<object>(), null);

			Action act = () => fake.Execute(f => f.GetType());

			act.Should().Throw<AmbiguousMatchException>().WithMessage("Ambiguous null-invocation*");
		}

		[Fact]
		public void When_method_call_as_arg_Should_Succeed()
		{
			var fake = new Fake<TestClass>();
			var testClass = new TestClass();

			var sut = fake.Rewrite(f => f.GetNumber("1"));
			sut.Verify(f => f.SomeMethod(GetOne()));

			sut.Execute();
		}

		[Fact]
		public void When_is_arg_with_different_type_Should_Succeed()
		{
			var fake = new Fake<TestClass>();
			var testClass = new TestClass();

			var sut = fake.Rewrite(f => f.GetNumber("1"));
			sut.Verify(f => f.SomeMethod(Arg.Is<string>(x => x == "1")));

			sut.Execute();
		}

		string GetOne() => "1";

		[Fact]
		public void When_no_closure_field_Should_fail()
		{
			var fake = new Fake<TestClass>();
			var type = typeof(TestClass);
			fake.Services.RegisterInstance<IAssemblyLoader>(new FakeAsseblyLoader(type.Assembly, type), ifAlreadyRegistered: IfAlreadyRegistered.Replace);

			var sut = fake.Rewrite(f => f.GetNumber("test"));
			sut.Append(() => { });
			Action act = () => sut.Execute();

			act.Should().Throw<MissingFieldException>();
		}

		[Fact]
		public void When_no_return_field_Should_fail()
		{
			var fake = new Fake<TestClass>();
			var type = typeof(TestClass);
			fake.Services.RegisterInstance<IAssemblyLoader>(new FakeAsseblyLoader(type.Assembly, type), ifAlreadyRegistered: IfAlreadyRegistered.Replace);

			var sut = fake.Rewrite(f => f.GetNumber("test"));
			sut.Replace(t => t.GetNumber(Arg.IsAny<string>())).Return("str");
			Action act = () => sut.Execute();

			act.Should().Throw<MissingFieldException>();
		}

		[Fact]
		public void When_no_setup_body_field_Should_fail()
		{
			var fake = new Fake<InitializationTests>();
			var type = typeof(InitializationTests);
			fake.Services.RegisterInstance<IAssemblyLoader>(new FakeAsseblyLoader(type.Assembly, type), ifAlreadyRegistered: IfAlreadyRegistered.Replace);

			var sut = fake.Rewrite(f => f.When_no_setup_body_field_Should_fail());
			sut.Replace(() => DateTime.Now).Return(DateTime.MaxValue);
			Action act = () => sut.Execute();

			act.Should().Throw<MissingFieldException>();
		}

		[Fact]
		public void When_no_execution_context_field_Should_fail()
		{
			var fake = new Fake<TestClassWithSetupBodyField>();
			var type = typeof(TestClassWithSetupBodyField);
			fake.Services.RegisterInstance<IAssemblyLoader>(new FakeAsseblyLoader(type.Assembly, type), ifAlreadyRegistered: IfAlreadyRegistered.Replace);

			var sut = fake.Rewrite(f => f.GetHashCode());
			sut.Replace(() => DateTime.Now).Return(DateTime.MaxValue);
			Action act = () => sut.Execute();

			act.Should().Throw<MissingFieldException>();
		}

		private class TestClassWithSetupBodyField
		{
			public static IInvocationExpression GetHashCodeSystemDateTime_get_Now_SetupBodyField;
		}

		private class TestClass
		{
			public static IInvocationExpression GetNumberSystemString_GetNumber_SystemString_SetupBodyField;
			public static IExecutionContext GetNumberSystemString_GetNumber_SystemString_ExecutionContext;
			public string GetNumber(string arg) => SomeMethod(arg);
			public string SomeMethod(object arg) => (string)arg;
		}

		private class FakeAsseblyLoader : IAssemblyLoader
		{
			private readonly Assembly _assembly;
			private readonly Type _type;

			public FakeAsseblyLoader(Assembly assembly, Type type)
			{
				_assembly = assembly;
				_type = type;
			}

			public Tuple<Assembly, Type> LoadAssemblies(IFakeOptions options, bool loadFieldsAsm) => new(_assembly, _type);
		}
	}
}
