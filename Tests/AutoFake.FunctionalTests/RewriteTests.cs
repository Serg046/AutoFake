using FluentAssertions;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;
using LinqExpression = System.Linq.Expressions.Expression;

namespace AutoFake.FunctionalTests
{
	public class RewriteTests
	{
		[ExcludedFact]
		public void When_null_expression_Should_fail()
		{
			var fake = new Fake<TestClass>();

			Action act = () => fake.Rewrite(null);

			act.Should().Throw<ArgumentNullException>();
		}

		[ExcludedFact]
		public void When_multiple_suts_Should_succeed()
		{
			var fake = new Fake<TestClass>();

			var sut1 = fake.Rewrite(m => m.FirstMethod()); sut1.Replace(m => m.GetValue()).Return(1);
			var sut2 = fake.Rewrite(m => m.SecondMethod()); sut2.Replace(m => m.GetValue()).Return(1);

			Assert.Equal(1, sut1.Execute());
			Assert.Equal(1, sut2.Execute());
		}

		[ExcludedFact]
		public void When_overloaded_methods_Should_choose_the_right()
		{
			var fake = new Fake<TestClass>();

			var sut1 = fake.Rewrite(m => m.FirstMethod()); sut1.Replace(m => m.GetValue()).Return(1);
			var sut2 = fake.Rewrite(m => m.FirstMethod(1)); sut2.Replace(m => m.GetValue()).Return(2);

			Assert.Equal(1, sut1.Execute());
			Assert.Equal(3, sut2.Execute());
		}

		[ExcludedFact]
		public void When_rewrite_func_with_input_Should_succeed()
		{
			var fake = new Fake(typeof(TestClass));

			var sut1 = fake.Rewrite((TestClass m) => m.FirstMethod()); sut1.Replace((TestClass m) => m.GetValue()).Return(1);
			var sut2 = fake.Rewrite((TestClass m) => m.FirstMethod(1)); sut2.Replace((TestClass m) => m.GetValue()).Return(2);

			Assert.Equal(1, sut1.Execute());
			Assert.Equal(3, sut2.Execute());
		}
		
		[ExcludedFact]
		public void When_no_body_Should_fail()
		{
			var fake = new Fake<TestClass>();

			Action act = () => fake.Rewrite(() => TestClass.NoBodyMethod()).Execute();

			act.Should().Throw<NotSupportedException>();
		}

		[ExcludedFact]
		public void When_no_method_Should_fail()
		{
			var fake = new Fake<RewriteTests>();

			Action act = () => fake.Rewrite(() => TestClass.NoBodyMethod()).Execute();

			act.Should().Throw<MissingMethodException>();
		}

		[ExcludedFact]
		public void When_no_property_Should_fail()
		{
			var fake = new Fake<RewriteTests>();

			Action act = () => fake.Execute((TestClass f) => f.Date);

			act.Should().Throw<MissingMemberException>();
		}

		[ExcludedFact]
		public void When_no_field_Should_fail()
		{
			var fake = new Fake<RewriteTests>();

			Action act = () => fake.Execute(() => TextReader.Null);

			act.Should().Throw<MissingMemberException>();
		}

		[ExcludedFact]
		public void When_rewrite_ctor_Should_succeed()
		{
			var fake = new Fake<TestClass>();
			var date = new DateTime(2022, 9, 3);

			var sut = fake.Rewrite(() => new TestClass());
			sut.Replace(() => DateTime.Now).Return(date);

			fake.Execute(f => f.Date).Should().Be(date);
		}

		[ExcludedFact]
		public void When_rewrite_field_Should_fail()
		{
			var fake = new Fake<RewriteTests>();

			Action act = () => fake.Rewrite(() => TextReader.Null).Execute();

			act.Should().Throw<NotSupportedException>();
		}

		[ExcludedFact]
		public void When_unsupported_expression_Should_fail()
		{
			var fake = new Fake<TestClass>();

			Action act = () => fake.Execute(() => 1);

			act.Should().Throw<NotSupportedException>();
		}

		[ExcludedFact]
		public void When_member_declaring_type_is_null_Should_fail()
		{
			var fake = new Fake<TestClass>();
			var member = LinqExpression.MakeMemberAccess(null, new FakeFieldInfo<int>());
			var lambda = LinqExpression.Lambda<Func<int>>(member);
			var sut = fake.Rewrite(() => DateTime.Now);

			Action act = () => sut.Replace(lambda);

			act.Should().Throw<InvalidOperationException>();
		}

		private class TestClass
		{
			public DateTime PropSetter { set { } }
			public DateTime Date { get; } = DateTime.Now;

			public int GetValue() => -1;

			public int FirstMethod() => GetValue();

			public int SecondMethod() => GetValue();

			public int FirstMethod(int arg) => GetValue() + arg;

			[DllImport("no.dll")]
			public static extern void NoBodyMethod();
		}

		private class FakeFieldInfo<T> : FieldInfo
		{
			public override Type DeclaringType => null;
			public override FieldAttributes Attributes => FieldAttributes.Public | FieldAttributes.Static;
			public override Type FieldType => typeof(T);
			public override string Name => "Field1";
			public override RuntimeFieldHandle FieldHandle => throw new NotImplementedException();
			public override Type ReflectedType => throw new NotImplementedException();
			public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();
			public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotImplementedException();
			public override object GetValue(object obj) => throw new NotImplementedException();
			public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();
			public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture) => throw new NotImplementedException();
		}
	}
}
