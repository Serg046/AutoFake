using FluentAssertions;
using System;
using Xunit;

namespace AutoFake.FunctionalTests.TypeMemberMocks.StaticTests
{
	public class EventTests
	{
		[Fact]
		public void When_add_event_handler_Should_be_intercepted()
		{
			var fake = new Fake(typeof(TestClass));

			var sut = fake.Rewrite(() => TestClass.AddHandler(x => Console.WriteLine(x)));
			sut.Verify(Event.Of(typeof(TestClass), nameof(TestClass.Event)).Add(() => Arg.IsAny<Action<int>>()));
			sut.Verify(Event.Of(typeof(TestClass), nameof(TestClass.Event)).Remove(() => Arg.IsAny<Action<int>>())).ExpectedCalls(0);

			sut.Execute();
		}

		[Fact]
		public void When_incorrect_event_name_Should_fail()
		{
			Action act = () => Event.Of(typeof(TestClass), "IncorrectEvent").Add(() => Arg.IsAny<Action>());

			act.Should().Throw<MissingMethodException>();
		}

		[Fact]
		public void When_remove_event_handler_Should_be_intercepted()
		{
			var fake = new Fake(typeof(TestClass));

			var sut = fake.Rewrite(() => TestClass.RemoveHandler(x => Console.WriteLine(x)));
			sut.Verify(Event.Of(typeof(TestClass), nameof(TestClass.Event)).Remove(() => Arg.IsAny<Action<int>>()));
			sut.Verify(Event.Of(typeof(TestClass), nameof(TestClass.Event)).Add(() => Arg.IsAny<Action<int>>())).ExpectedCalls(0);

			sut.Execute();
		}

		[Fact]
		public void When_check_event_arg_Should_be_intercepted()
		{
			var fake = new Fake(typeof(TestClass));
			var x = 0;

			var sut = fake.Rewrite(() => TestClass.AddHandler(arg => EvendHandler(ref x, arg)));
			sut.Verify(Event.Of(typeof(TestClass), nameof(TestClass.Event)).Add(() => Arg.Is<Action<int>>(CheckArgument)));

			sut.Execute();
			x.Should().Be(5);
		}

		private void EvendHandler(ref int x, int arg) => x = arg;

		private bool CheckArgument(Action<int> handler)
		{
			handler(5);
			return true;
		}

		[Fact]
		public void When_add_event_handler_to_external_event_Should_be_intercepted()
		{
			var fake = new Fake(typeof(TestClass));

			var sut = fake.Rewrite(() => TestClass.AddHandlerToExternalEvent());
			sut.Verify(Event.Of(typeof(Console), nameof(Console.CancelKeyPress)).Add(() => Arg.Is<ConsoleCancelEventHandler>(CheckArgument)));
			sut.Execute();

			var data = fake.Execute(() => TestClass.CancelKeyPressData);
			data.Sender.Should().Be("sender");
			data.EventArgs.Should().BeNull();
		}

		private bool CheckArgument(ConsoleCancelEventHandler handler)
		{
			handler("sender", null);
			return true;
		}

		[Fact]
		public void When_event_called_Should_be_intercepted()
		{
			var fake = new Fake(typeof(TestClass));

			var sut = fake.Rewrite(() => TestClass.InvokeHandler());
			sut.Verify((Action<int> handler) => handler.Invoke(5));

			sut.Execute();
		}

		private static class TestClass
		{
			public static event Action<int> Event;
			public static (object Sender, ConsoleCancelEventArgs EventArgs) CancelKeyPressData;

			public static void AddHandler(Action<int> action)
			{
				Event += action;
			}

			public static void RemoveHandler(Action<int> action)
			{
				Event -= action;
			}

			public static void AddHandlerToExternalEvent()
			{
				Console.CancelKeyPress += (sender, eventArgs) => CancelKeyPressData = (sender, eventArgs);
			}

			public static void InvokeHandler()
			{
				Event += TestClass_Event;
				Event.Invoke(5);
				Event -= TestClass_Event;

				static void TestClass_Event(int obj)
				{
				}
			}
		}
	}
}
