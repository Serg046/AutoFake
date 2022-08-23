using AutoFake.Abstractions;
using FluentAssertions;
using System;
using Xunit;

namespace AutoFake.FunctionalTests
{
	public class FakeTests
	{
		[Fact]
		public void When_null_as_fake_type_Should_fail()
		{
			Action act = () => new Fake(null);

			act.Should().Throw<ArgumentNullException>();
		}

		[Fact]
		public void When_generic_instance_void_action_Should_execute()
		{
			var fake = new Fake<TestClass>();

			fake.Execute(f => f.SetProp());

			fake.Execute(f => f.Prop).Should().Be(5);
		}

		[Fact]
		public void When_instance_void_action_Should_execute()
		{
			var fake = new Fake(typeof(TestClass));

			fake.Execute((TestClass f) => f.SetProp());

			fake.Execute((TestClass f) => f.Prop).Should().Be(5);
		}

		[Fact]
		public void When_static_void_action_Should_execute()
		{
			var fake = new Fake(typeof(TestClass));

			fake.Execute(() => TestClass.SetStaticProp());

			fake.Execute(() => TestClass.StaticProp).Should().Be(5);
		}

		[Fact]
		public void When_instance_void_action_executor_Should_execute()
		{
			var fake = new Fake(typeof(TestClass));

			var sut = fake.Rewrite((TestClass f) => f.SetProp());
			sut.Replace((TestClass t) => t.GetFive()).Return(6).When(HandleArgument);

			sut.Execute();
			fake.Execute((TestClass f) => f.Prop).Should().Be(6);

			static bool HandleArgument(IExecutor<object> executor)
			{
				executor.Execute((TestClass t) => t.SetProp(3));
				return executor.Execute((TestClass t) => t.Prop) == 3;
			}
		}

		[Fact]
		public void When_static_void_action_executor_Should_execute()
		{
			var fake = new Fake(typeof(TestClass));

			var sut = fake.Rewrite(() => TestClass.SetStaticProp());
			sut.Replace(() => TestClass.GetStaticFive()).Return(6).When(HandleArgument);

			sut.Execute();
			fake.Execute(() => TestClass.StaticProp).Should().Be(6);

			static bool HandleArgument(IExecutor<object> executor)
			{
				executor.Execute(() => TestClass.SetStaticProp(3));
				return executor.Execute(() => TestClass.StaticProp) == 3;
			}
		}

		[Fact]
		public void When_object_func_executor_Should_execute()
		{
			var fake = new Fake(typeof(TestClass));

			var sut = fake.Rewrite((TestClass f) => f.SetProp());
			sut.Replace((TestClass t) => t.GetFive()).Return(6)
				.When(exe => exe.Execute(obj => obj.GetType()).FullName == typeof(TestClass).FullName);

			sut.Execute();
			fake.Execute((TestClass f) => f.Prop).Should().Be(6);
		}

		[Fact]
		public void When_object_action_executor_Should_execute()
		{
			var fake = new Fake(typeof(TestClass));

			var sut = fake.Rewrite((TestClass f) => f.SetProp());
			sut.Replace((TestClass t) => t.GetFive()).Return(6).When(HandleArgument);
			Action act = () => sut.Execute();

			act.Should().Throw<MissingMethodException>().WithMessage("*ThrowNotImplementedException*not found*");

			static bool HandleArgument(IExecutor<object> executor)
			{
				executor.Execute(obj => ThrowNotImplementedException(obj));
				return true;
			}
		}

		private static void ThrowNotImplementedException(object obj) => throw new NotImplementedException();

		private class TestClass
		{
			public static int StaticProp { get; private set; }

			public int Prop { get; private set; }

			public void SetProp() => Prop = GetFive();
			public void SetProp(int prop) => Prop = prop;
			public static void SetStaticProp() => StaticProp = GetStaticFive();
			public static void SetStaticProp(int prop) => StaticProp = prop;
			public int GetFive() => 5;
			public static int GetStaticFive() => 5;
		}
	}
}
