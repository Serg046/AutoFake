using System;
using System.Collections.Generic;
using System.Linq;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup;
using AutoFake.Abstractions.Setup.Mocks;
using DryIoc;
using FluentAssertions;
using Mono.Cecil;
using Xunit;

namespace AutoFake.FunctionalTests
{
	public class InsertTests
	{
		[ExcludedFact]
		public void When_append_Should_add_number_in_the_end()
		{
			var fake = new Fake<TestClass>();
			var numbers = new List<int>();

			var sut = fake.Rewrite(t => t.SomeMethod(numbers));
			sut.Append(() => numbers.Add(-1));

			sut.Execute();
			Assert.Equal(new[] { 3, 5, 7, -1 }, numbers);
		}

		[ExcludedFact]
		public void When_prepend_Should_add_number_in_the_beginning()
		{
			var fake = new Fake<TestClass>();
			var numbers = new List<int>();

			var sut = fake.Rewrite(t => t.SomeMethod(numbers));
			sut.Prepend(() => numbers.Add(-1));

			sut.Execute();
			Assert.Equal(new[] { -1, 3, 5, 7 }, numbers);
		}

		[ExcludedFact]
		public void When_append_with_source_member_Should_add_number_after_cmd()
		{
			var fake = new Fake<TestClass>();
			var numbers = new List<int>();

			var sut = fake.Rewrite(t => t.SomeMethod(numbers));
			sut.Append(() => numbers.Add(-1))
				.After((List<int> list) => list.AddRange(Arg.IsAny<int[]>()));

			sut.Execute();
			Assert.Equal(new[] { 3, 5, -1, 7 }, numbers);
		}

		[ExcludedFact]
		public void When_multiple_callbacks_Should_add_both_number()
		{
			var fake = new Fake<TestClass>();
			var numbers = new List<int>();

			var sut = fake.Rewrite(t => t.SomeMethod(numbers));
			sut.Prepend(() => numbers.Add(-1));
			sut.Append(() => numbers.Add(-2));

			sut.Execute();
			Assert.Equal(new[] { -1, 3, 5, 7, -2 }, numbers);
		}

		[ExcludedFact]
		public void When_prepend_with_source_member_Should_add_number_before_cmd()
		{
			var fake = new Fake<TestClass>();
			var numbers = new List<int>();

			var sut = fake.Rewrite(t => t.SomeMethod(numbers));
			sut.Prepend(() => numbers.Add(-1))
				.Before((List<int> list) => list.AddRange(Arg.IsAny<int[]>()));

			sut.Execute();
			Assert.Equal(new[] { 3, -1, 5, 7 }, numbers);
		}

		[ExcludedFact]
		public void When_append_Should_add_number_to_local_val()
		{
			var numbers = new List<int>();
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(t => t.SomeMethod(new List<int>()));
			sut.Append(() => numbers.Add(-1));

			sut.Execute();
			Assert.Equal(new[] { -1 }, numbers);
		}

		[ExcludedFact]
		public void When_generic_insert_mocks_Should_add_numbers_to_the_appropriate_places()
		{
			var numbers = new List<int>();
			var fake = new Fake<TestClass>();
			var sut = fake.Rewrite(t => t.SomeMethod(numbers));
			sut.Prepend(() => numbers.Add(-6)).Before(t => t.AnotherMethod());
			sut.Append(() => numbers.Add(6)).After(t => t.AnotherMethod());

			sut.Execute();

			numbers.Should().ContainInOrder(3, 5, -6, 6, 7);
		}

		[ExcludedFact]
		public void When_arguments_are_matched_Should_add_numbers()
		{
			var numbers = new List<int>();
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.SomeMethod(numbers));
			sut.Append(() => numbers.Add(0)).After((List<int> list) => list.Add(Arg.IsAny<int>()));
			sut.Append(() => numbers.Add(-1)).After((List<int> list) => list.Add(3)).WhenArgumentsAreMatched();
			sut.Append(() => numbers.Add(-2)).After((List<int> list) => list.Add(7)).WhenArgumentsAreMatched();

			sut.Execute();
			numbers.Should().ContainInOrder(3, -1, 0, 5, 7, -2, 0);
		}

		[ExcludedFact]
		public void When_invariants_are_matched_Should_add_numbers()
		{
			var numbers = new List<int>();
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.SomeMethod(numbers));
			sut.Append(() => numbers.Add(0)).After((List<int> list) => list.Add(Arg.IsAny<int>()));
			sut.Append(() => numbers.Add(-1)).After((List<int> list) => list.Add(Arg.IsAny<int>()))
				.When(f => f.Execute(x => x.Prop) == 0);
			sut.Append(() => numbers.Add(-2)).After((List<int> list) => list.Add(Arg.IsAny<int>()))
				.When(f => f.Execute(x => x.Prop) == 1);

			sut.Execute();
			numbers.Should().ContainInOrder(3, -1, 0, 5, 7, -2, 0);
		}

		[ExcludedFact]
		public void When_unsupported_location_Should_return_false()
		{
			var fake = new Fake<TestClass>();
			var method = fake.Services.Resolve<IAssemblyReader>().SourceTypeDefinition.Methods.First();
			var mockFactory = fake.Services.Resolve<IMockFactory>();
			var firstInstruction = method.Body.Instructions.First();
			var lastInstruction = method.Body.Instructions.Last();

			var validPrependMock = mockFactory.GetInsertMock(() => { }, IInsertMock.Location.Before);
			var validApppendMock = mockFactory.GetInsertMock(() => { }, IInsertMock.Location.After);
			var invalidInsertMock = mockFactory.GetInsertMock(() => { }, (IInsertMock.Location)100);

			validPrependMock
				.IsSourceInstruction(method, firstInstruction, Enumerable.Empty<GenericArgument>())
				.Should().BeTrue();

			validApppendMock
				.IsSourceInstruction(method, lastInstruction, Enumerable.Empty<GenericArgument>())
				.Should().BeTrue();

			invalidInsertMock
				.IsSourceInstruction(method, firstInstruction, Enumerable.Empty<GenericArgument>())
				.Should().BeFalse();
			invalidInsertMock
				.IsSourceInstruction(method, lastInstruction, Enumerable.Empty<GenericArgument>())
				.Should().BeFalse();
		}

		[ExcludedFact]
		public void When_no_closure_field_during_injection_Should_fail()
		{
			var fake = new Fake<TestClass>();
			fake.Services.RegisterInstance<IPrePostProcessor>(new FakePrePostProcessor(), ifAlreadyRegistered: IfAlreadyRegistered.Replace);

			var sut = fake.Rewrite(f => f.SomeMethod(new List<int>()));
			sut.Append(() => { }).After(t => t.AnotherMethod());
			Action act = () => sut.Execute();

			act.Should().Throw<InvalidOperationException>();
		}

		[ExcludedFact]
		public void When_append_with_return_type_provided_Should_insert_after()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.CallGetProp());
			sut.Append(() => throw new Exception()).After(f => f.SetPropAndReturn());
			Action act = () => sut.Execute();

			act.Should().Throw<Exception>();
			fake.Execute(f => f.Prop).Should().Be(1);
		}

		[ExcludedFact]
		public void When_prepend_with_return_type_provided_Should_insert_before()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.CallGetProp());
			sut.Prepend(() => throw new Exception()).Before(f => f.SetPropAndReturn());
			Action act = () => sut.Execute();

			act.Should().Throw<Exception>();
			fake.Execute(f => f.Prop).Should().Be(0);
		}

		[ExcludedFact]
		public void When_append_with_input_and_return_type_provided_Should_insert_after()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.CallGetProp());
			sut.Append(() => throw new Exception()).After((int prop) => prop.ToString());
			Action act = () => sut.Execute();

			act.Should().Throw<Exception>();
			fake.Execute(f => f.Prop).Should().Be(1);
		}

		[ExcludedFact]
		public void When_prepend_with_input_and_return_type_provided_Should_insert_before()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.CallGetProp());
			sut.Prepend(() => throw new Exception()).Before((int prop) => prop.GetHashCode());
			Action act = () => sut.Execute();

			act.Should().Throw<Exception>();
			fake.Execute(f => f.Prop).Should().Be(0);
		}

		[ExcludedFact]
		public void When_append_after_static_void_Should_succeed()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.CallGetProp());
			sut.Append(() => throw new Exception()).After(() => Console.WriteLine("end"));
			Action act = () => sut.Execute();

			act.Should().Throw<Exception>();
			fake.Execute(f => f.Prop).Should().Be(1);
		}

		[ExcludedFact]
		public void When_prepend_before_static_void_Should_succeed()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.CallGetProp());
			sut.Prepend(() => throw new Exception()).Before(() => Console.Write("start"));
			Action act = () => sut.Execute();

			act.Should().Throw<Exception>();
			fake.Execute(f => f.Prop).Should().Be(0);
		}

		[ExcludedFact]
		public void When_expected_calls_provided_Should_check()
		{
			var fake = new Fake<TestClass>();
			var list = new List<int>();

			var sut = fake.Rewrite(f => f.SomeMethod(list));
			sut.Append(() => list.Add(0)).After((List<int> l) => l.Add(Arg.IsAny<int>()))
				.ExpectedCalls(2);

			sut.Execute();
			list.Should().ContainInOrder(3, 0, 5, 7, 0);
		}

		[ExcludedFact]
		public void When_expected_calls_with_exact_param_provided_Should_check()
		{
			var fake = new Fake<TestClass>();
			var list = new List<int>();

			var sut = fake.Rewrite(f => f.SomeMethod(list));
			sut.Append(() => list.Add(0)).After((List<int> l) => l.Add(3))
				.WhenArgumentsAreMatched()
				.ExpectedCalls(1);

			sut.Execute();
			list.Should().ContainInOrder(3, 0, 5, 7);
		}

		[ExcludedTheory]
		[InlineData(true, 3, 0, 5, 7)]
		[InlineData(false, 3, 5, 7)]
		public void When_condition_is_provided_Should_check(bool condition, params int[] result)
		{
			var fake = new Fake<TestClass>();
			var list = new List<int>();

			var sut = fake.Rewrite(f => f.SomeMethod(list));
			sut.Append(() => list.Add(0)).After((List<int> l) => l.Add(3))
				.WhenArgumentsAreMatched()
				.When(() => condition);

			sut.Execute();
			list.Should().ContainInOrder(result);
		}

		private class TestClass
		{
			public int Prop { get; private set; }

			public void SomeMethod(List<int> numbers)
			{
				numbers.Add(3);
				numbers.AddRange(new[] { 5 });
				AnotherMethod();
				numbers.Add(7);
			}

			public void AnotherMethod()
			{
				Prop = 1;
			}

			public int SetPropAndReturn()
			{
				AnotherMethod();
				return Prop;
			}

			public int CallGetProp()
			{
				Console.Write("start");
				Console.WriteLine(Prop.GetHashCode());
				var prop = SetPropAndReturn();
				Console.WriteLine(Prop.ToString());
				Console.WriteLine("end");
				return prop;
			}
		}

		private class FakePrePostProcessor : IPrePostProcessor
		{
			public FieldDefinition GenerateField(string name, Type returnType) => null;

			public void InjectVerification(IEmitter emitter, FieldDefinition setupBody, FieldDefinition executionContext)
			{
			}
		}
	}
}
