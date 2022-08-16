using FluentAssertions;
using System;
using System.Diagnostics;
using Xunit;

namespace AutoFake.FunctionalTests.TypeMemberMocks.InstanceTests
{
	public class EventTests
	{
		[Fact]
		public void When_add_event_handler_Should_be_intercepted()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.AddHandler(x => Console.WriteLine(x)));
			sut.Verify(Event.Of<TestClass>(nameof(TestClass.Event)).Add(() => Arg.IsAny<Action<int>>()));
			sut.Verify(Event.Of<TestClass>(nameof(TestClass.Event)).Remove(() => Arg.IsAny<Action<int>>())).ExpectedCalls(0);

			sut.Execute();
		}

		[Fact]
		public void When_remove_event_handler_Should_be_intercepted()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.RemoveHandler(x => Console.WriteLine(x)));
			sut.Verify(Event.Of<TestClass>(nameof(TestClass.Event)).Remove(() => Arg.IsAny<Action<int>>()));
			sut.Verify(Event.Of<TestClass>(nameof(TestClass.Event)).Add(() => Arg.IsAny<Action<int>>())).ExpectedCalls(0);

			sut.Execute();
		}

		[Fact]
		public void When_check_event_arg_Should_be_intercepted()
		{
			var fake = new Fake<TestClass>();
			var x = 0;

			var sut = fake.Rewrite(f => f.AddHandler(arg => EvendHandler(ref x, arg)));
			sut.Verify(Event.Of<TestClass>(nameof(TestClass.Event)).Add(() => Arg.Is<Action<int>>(CheckArgument)));

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
		public void When_add_event_handler_through_runtime_type_Should_be_intercepted()
		{
			var fake = new Fake(typeof(TestClass));

			var sut = fake.Rewrite((TestClass f) => f.AddHandler(x => Console.WriteLine(x)));
			sut.Verify(Event.Of(typeof(TestClass), nameof(TestClass.Event)).Add(() => Arg.IsAny<Action<int>>()));
			sut.Verify(Event.Of(typeof(TestClass), nameof(TestClass.Event)).Remove(() => Arg.IsAny<Action<int>>())).ExpectedCalls(0);

			sut.Execute();
		}

		[Fact]
		public void When_add_event_handler_to_external_event_Should_be_intercepted()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.AddHandlerToExternalEvent(new Process()));
			sut.Verify(Event.Of<Process>(nameof(Process.Exited)).Add(() => Arg.Is<EventHandler>(CheckArgument)));
			sut.Execute();

			var data = fake.Execute(f => f.ExitedData);
			data.Sender.Should().Be("sender");
			data.EventArgs.Should().Be(EventArgs.Empty);
		}

		private bool CheckArgument(EventHandler handler)
		{
			handler("sender", EventArgs.Empty);
			return true;
		}

		[Fact]
		public void When_event_called_Should_be_intercepted()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.InvokeHandler());
			sut.Verify((Action<int> handler) => handler.Invoke(5));

			sut.Execute();
		}

		private class TestClass
		{
			public event Action<int> Event;
			public (object Sender, EventArgs EventArgs) ExitedData;

			public void AddHandler(Action<int> action)
			{
				Event += action;
			}

			public void RemoveHandler(Action<int> action)
			{
				Event -= action;
			}

			public void AddHandlerToExternalEvent(Process file)
			{
				file.Exited += (sender, eventArgs) => ExitedData = (sender, eventArgs);
			}

			public void InvokeHandler()
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
