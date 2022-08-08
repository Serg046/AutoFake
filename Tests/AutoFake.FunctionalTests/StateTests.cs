using AutoFake.Abstractions;
using AutoFake.Exceptions;
using DryIoc;
using FluentAssertions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
			new Fake<CtorTestClass>(1 , Arg.IsNull<int?>()).Execute(f => f.ReturnCtorArg()).Should().Be(1);
			new Fake<CtorTestClass>(1 , null).Execute(f => f.ReturnCtorArg()).Should().Be(1);
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
			var fake = new Fake<object>();
			fake.Options.Debug = DebugMode.Enabled;

			Action act = () => fake.Execute(f => f.GetType());

			act.Should().Throw<SymbolsNotFoundException>().WithMessage("No symbol found*");
		}

		private class AmbiguousCtorTestClass
		{
			private object _arg;

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
			private object _arg;

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
		}
	}
}
