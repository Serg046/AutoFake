using FluentAssertions;
using System;
using System.Linq;
using Xunit;

namespace AutoFake.FunctionalTests.Units
{
	public class ExtensionsTests
	{
		[Fact]
		public void When_no_generic_args_Should_return_null()
		{
			Extensions.FindGenericTypeOrDefault(Enumerable.Empty<GenericArgument>(), string.Empty)
				.Should().BeNull();
		}

		[Fact]
		public void When_valid_state_machine_attribute_Should_skip()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.GetDateWithValidMachine());
			sut.Replace((StateMachine m) => m.GetAug21()).Return(new DateTime(2022, 8, 20));

			sut.Execute().Day.Should().Be(20);
		}

		[Fact]
		public void When_invalid_state_machine_attribute_Should_skip()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.GetDateWithInvalidMachine());
			sut.Replace((StateMachine m) => m.GetAug21()).Return(new DateTime(2022, 8, 20));

			sut.Execute().Day.Should().Be(21);
		}

		private class StateMachineAttribute : Attribute 
		{
			public StateMachineAttribute(Type type) { }
		}

		private class TestClass
		{
			[System.Runtime.CompilerServices.AsyncStateMachine(typeof(StateMachine))]
			public DateTime GetDateWithValidMachine()
			{
				var stateMachine = new StateMachine();
				stateMachine.CallMoveNext();
				return stateMachine.Date;
			}

			[StateMachine(typeof(StateMachine))]
			public DateTime GetDateWithInvalidMachine()
			{
				var stateMachine = new StateMachine();
				stateMachine.CallMoveNext();
				return stateMachine.Date;
			}
		}

		private class StateMachine
		{
			public DateTime Date { get; private set; }

			public void CallMoveNext()
			{
				var method = GetType().GetMethod(nameof(MoveNext));
				method.Invoke(this, null);
			}

			public void MoveNext()
			{
				Date = GetAug21();
			}

			public DateTime GetAug21() => new DateTime(2022, 8, 21);
		}
	}
}
