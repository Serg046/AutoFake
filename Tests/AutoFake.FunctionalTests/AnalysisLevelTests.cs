using System;
using System.Collections.Generic;
using AnotherSut;
using AutoFake.Abstractions;
using AutoFake.Exceptions;
using FluentAssertions;
using Sut;
using Xunit;

namespace AutoFake.FunctionalTests
{
	public class AnalysisLevelTests
	{
		[Fact]
		public void When_SimpleLambda_Should_Pass()
		{
			var fake = new Fake<SystemUnderTest>();

			fake.Rewrite(f => f.SimpleMethod()).Execute();
		}

		[Fact]
		public void When_InternalSut_Should_Pass()
		{
			var fake = new Fake<SystemUnderTest>();

			fake.Rewrite(f => f.InternalMethod()).Execute();
		}

		[Theory]
		[InlineData(true, 2, true)]
		[InlineData(false, 0, false)]
		public void When_ExpectedCallsFunc_Should_Pass(bool equalOp, int arg, bool throws)
		{
			var fake = new Fake<SystemUnderTest>();
			IExecutionContext.CallsCheckerFunc checker;
			if (equalOp) checker = x => x == arg;
			else checker = x => x > arg;

			var sut = fake.Rewrite(f => f.GetCurrentDate());
			sut.Replace(() => DateTime.Now)
				.ExpectedCalls(checker)
				.Return(DateTime.MaxValue);

			if (throws)
			{
				Assert.Throws<ExpectedCallsException>(() => sut.Execute());
			}
			else
			{
				Assert.Equal(DateTime.MaxValue, sut.Execute());
			}
		}

		[Fact]
		public void When_ActionToInsert_Should_Pass()
		{
			var fake = new Fake<SystemUnderTest>();
			var events = new List<int>();

			var sut = fake.Rewrite(f => f.GetCurrentDate());
			sut.Prepend(() => events.Add(0));
			sut.Prepend(() => events.Add(1)).Before(() => DateTime.Now);
			sut.Append(() => events.Add(2)).After(() => DateTime.Now);
			sut.Append(() => events.Add(3));

			sut.Execute();

			Assert.Equal(new[] { 0, 1, 2, 3 }, events);
		}

		[Fact]
		public void When_virtual_implementation_in_another_lib_Should_pass()
		{
			var fake = new Fake<SystemUnderTest>();
			fake.Options.AddReference(typeof(AnotherSystemUnderTest));

			var sut = fake.Rewrite(f => f.GetDateVirtual());
			sut.Replace(() => DateTime.UtcNow).Return(DateTime.MaxValue);
			sut.Replace(() => DateTime.Now).Return(DateTime.MinValue);

			sut.Execute().Should().Be(DateTime.MaxValue - DateTime.MinValue);
		}

		[Fact]
		public void When_virtual_implementation_in_another_lib_with_all_assemblies_Should_pass()
		{
			var fake = new Fake<SystemUnderTest>();
			fake.Options.AnalysisLevel = AnalysisLevels.AllExceptSystemAndMicrosoft;

			var sut = fake.Rewrite(f => f.GetDateVirtual());
			sut.Replace(() => DateTime.UtcNow).Return(DateTime.MaxValue);
			sut.Replace(() => DateTime.Now).Return(DateTime.MinValue);

			sut.Execute().Should().Be(DateTime.MaxValue - DateTime.MinValue);
		}

		[Theory]
		[InlineData(AnalysisLevels.Type, false)]
		[InlineData(AnalysisLevels.Assembly, true)]
		public void When_type_analysis_level_Should_skip_other_types(AnalysisLevels level, bool success)
		{
			var fake = new Fake<TestClass>();
			fake.Options.AnalysisLevel = level;
			var sut = fake.Rewrite(f => f.Throw());
			sut.Remove((TestClass.Nested.Nested2 n) => n.Throw());

			Action act = () => sut.Execute();

			if (success) act.Should().NotThrow<NotImplementedException>();
			else act.Should().Throw<NotImplementedException>();
		}

		private class TestClass
		{
			public void Throw() => new Nested().Throw();

			public class Nested
			{
				public void Throw() => new Nested2().Throw();

				public class Nested2
				{
					public void Throw() => throw new NotImplementedException();
				}
			}
		}
	}
}
