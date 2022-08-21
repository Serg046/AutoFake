using AutoFake.Abstractions;
using AutoFake.Exceptions;
using DryIoc;
using FluentAssertions;
using MultipleReturnTest;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AutoFake.FunctionalTests
{
	public class StateTests
	{
		[Fact]
		public void When_multiple_mocks_Should_apply_all()
		{
			var enumerable = Enumerable.Range(1, 100);
			var fake = new Fake<TestClass>();

			var i = 0;
			var sut = fake.Rewrite(f => f.Count(enumerable));
			sut.Replace((IEnumerator<int> enumerator) => enumerator.MoveNext()).Return(true).When(() => i++ < 3);
			sut.Replace((IEnumerator<int> enumerator) => enumerator.MoveNext()).Return(false).When(() => i > 3);

			sut.Execute().Should().Be(3);
		}

		[Fact]
		public void When_null_ctor_arg_Should_be_passed()
		{
			new Fake<CtorTestClass>("someObj").Execute(f => f.ReturnCtorArg()).Should().Be("someObj");
			new Fake<CtorTestClass>(Arg.IsNull<object>()).Execute(f => f.ReturnCtorArg()).Should().BeNull();
			new Fake<CtorTestClass>(null).Execute(f => f.ReturnCtorArg()).Should().BeNull();
		}

		[Fact]
		public void When_value_type_ctor_arg_Should_be_passed()
		{
			new Fake<CtorTestClass>(1, 1).Execute(f => f.ReturnCtorArg()).Should().Be(2);
			new Fake<CtorTestClass>(1, Arg.IsNull<int?>()).Execute(f => f.ReturnCtorArg()).Should().Be(1);
			new Fake<CtorTestClass>(1, null).Execute(f => f.ReturnCtorArg()).Should().Be(1);
			new Action(() => new Fake<CtorTestClass>(Arg.IsNull<int>(), 1).Execute(f => f.ReturnCtorArg()))
				.Should().Throw<InvalidOperationException>().WithMessage("*cannot be null*");
		}

		[Fact]
		public void When_ambiguous_ctor_arg_Should_throw()
		{
			new Action(() => new Fake<AmbiguousCtorTestClass>(1, null).Execute(f => f.ReturnCtorArg()))
				.Should().Throw<InitializationException>().WithMessage("*use Arg.IsNull<T>()*");
			new Fake<AmbiguousCtorTestClass>(1, Arg.IsNull<CtorTestClass>()).Rewrite(f => f.ReturnCtorArg())
				.Execute().Should().Be(2);
			new Fake<AmbiguousCtorTestClass>(1, Arg.IsNull<AmbiguousCtorTestClass>()).Rewrite(f => f.ReturnCtorArg())
				.Execute().Should().Be(3);
		}

		[Fact]
		public void When_debug_mode_with_symbols_Should_load_symbols()
		{
			var fake = new Fake<StateTests>();
			fake.Options.Debug = DebugMode.Enabled;

			var type = fake.Execute(f => f.GetType());

			type.Should().NotBeNull();
		}

		[Fact]
		public void When_debug_mode_without_symbols_Should_fail()
		{
			var fake = new Fake<SystemUnderTest>();
			fake.Options.Debug = DebugMode.Enabled;

			Action act = () => fake.Execute(f => f.ConditionalReturn(0));

			act.Should().Throw<InvalidOperationException>().WithMessage("No symbols found");
		}

		[Fact]
		public void When_auto_debug_mode_without_symbols_Should_not_fail()
		{
			var fake = new Fake<SystemUnderTest>();
			fake.Services.Resolve<IAssemblyReader>().SourceTypeDefinition.Should().NotBeNull();
			fake.Options.Debug = DebugMode.Enabled;

			fake.Execute(f => f.ConditionalReturn(-1)).Should().Be(-1);
		}

		[Fact]
		public void When_incorrect_type_Should_fail()
		{
			var fake = new Fake<TestClass>();
			fake.Services.Resolve<ITypeInfo>();
			var assemblyReader = fake.Services.Resolve<IAssemblyReader>();
			var typeDef = assemblyReader.SourceTypeDefinition.Module.Types.Single(t => t.FullName == typeof(StateTests).FullName);
			assemblyReader.SourceTypeDefinition.Module.Types.Remove(typeDef);

			Action act = () => fake.Execute(f => f.GetType());

			act.Should().Throw<InvalidOperationException>().WithMessage("Cannot find a type");
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

		[Fact]
		public void When_invalid_attribute_Should_skip()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.GetDateWithInvalidMachine());
			sut.Replace((StateMachine m) => m.GetAug21()).Return(new DateTime(2022, 8, 20));

			sut.Execute().Day.Should().Be(21);
		}

		private class AmbiguousCtorTestClass
		{
			private readonly object _arg;

			public AmbiguousCtorTestClass(int arg1, CtorTestClass _)
			{
				_arg = arg1 + 1;
			}

			public AmbiguousCtorTestClass(int arg1, AmbiguousCtorTestClass _)
			{
				_arg = arg1 + 2;
			}

			public object ReturnCtorArg() => _arg;
		}

		private class CtorTestClass
		{
			private readonly object _arg;

			public CtorTestClass(object arg)
			{
				_arg = arg;
			}

			public CtorTestClass(int arg1, int? arg2)
			{
				_arg = arg1 + (arg2 ?? 0);
			}

			public object ReturnCtorArg() => _arg;
		}

		private class TestClass
		{
			public int Count(IEnumerable<int> numbers)
			{
				var i = 0;
				foreach (var number in numbers)
				{
					i++;
				}
				return i;
			}

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

			[StateMachine(typeof(Attribute))]
			public DateTime GetDateWithoutdMachine()
			{
				var stateMachine = new StateMachine();
				stateMachine.CallMoveNext();
				return stateMachine.Date;
			}
		}

		private class StateMachineAttribute : Attribute
		{
			public StateMachineAttribute(Type type) { }
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
