using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace AutoFake.FunctionalTests.TypeMemberMocks.InstanceTests
{
	public class PropertyMockTests
	{
		[ExcludedFact]
		public void OwnInstanceTest()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.GetDynamicValue());
			sut.Replace(t => t.DynamicValue).Return(7);

			Assert.Equal(7, sut.Execute());
		}

		[ExcludedFact]
		public void ExternalInstanceTest()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.GetHelperDynamicValue());
			sut.Replace((HelperClass h) => h.DynamicValue).Return(7);

			Assert.Equal(7, sut.Execute());
		}

		[ExcludedFact]
		public void OwnStaticTest()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.GetDynamicStaticValue());
			sut.Replace(() => TestClass.DynamicStaticValue).Return(7);

			Assert.Equal(7, sut.Execute());
		}

		[ExcludedFact]
		public void ExternalStaticTest()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.GetHelperDynamicStaticValue());
			sut.Replace(() => HelperClass.DynamicStaticValue).Return(7);

			Assert.Equal(7, sut.Execute());
		}

		[ExcludedFact]
		public void FrameworkInstanceTest()
		{
			var fake = new Fake<TestClass>();

			const string cmd = "select * from Test";
			var sut = fake.Rewrite(f => f.GetFrameworkValue());
			sut.Replace((SqlCommand c) => c.CommandText).Return(cmd);

			Assert.Equal(cmd, sut.Execute());
		}

		[ExcludedFact]
		public void FrameworkStaticTest()
		{
			var fake = new Fake<TestClass>();

			var date = new DateTime(2016, 9, 25);
			var sut = fake.Rewrite(f => f.GetFrameworkStaticValue());
			sut.Replace(() => DateTime.Now).Return(date);

			Assert.Equal(date, sut.Execute());
		}

		[ExcludedFact]
		public void GenericTest()
		{
			var fake = new Fake<GenericTestClass<int>>();

			var sut = fake.Rewrite(f => f.GetValue(0, "0"));
			sut.Replace(s => s.Pair).Return(new KeyValuePair<int, string>(1, "1"));

			var actual = sut.Execute();
			Assert.Equal(1, actual.Key);
			Assert.Equal("1", actual.Value);
		}

		[ExcludedFact]
		public void WhenTest()
		{
			var fake = new Fake<WhenTestClass>();

			var sut = fake.Rewrite(f => f.SomeMethod());
			sut.Replace(() => TestClass.DynamicStaticValue).Return(1)
				.When(x => x.Execute(f => f.Prop) == -1);
			sut.Replace(() => TestClass.DynamicStaticValue).Return(2)
				.When(x => x.Execute(f => f.Prop) == 1);

			sut.Execute().Should().Be(3);
		}

		[ExcludedFact]
		public void EnumerableMethodTest()
		{
			var fake = new Fake<EnumerableTestClass>();

			var sut = fake.Rewrite(f => f.GetValue());
			sut.Replace(f => f.GetDynamicValue).Return(7);

			sut.Execute().Should().OnlyContain(i => i == 7);
		}

		[ExcludedFact]
		public void PropertySetterInstanceTest()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(f => f.SetReadWriteProperty(7));
			sut.Remove(Property.Of((TestClass t) => t.ReadWriteProperty).Set(() => 7));
			sut.Execute();

			fake.Execute(f => f.ReadWriteProperty).Should().Be(5);
		}

		[ExcludedFact]
		public void PropertySetterStaticTest()
		{
			var fake = new Fake<TestClass>();

			var sut = fake.Rewrite(() => TestClass.SetReadWriteStaticProperty(7));
			sut.Remove(Property.Of(() => TestClass.ReadWriteStaticProperty).Set(() => 7));
			sut.Execute();

			fake.Execute(() => TestClass.ReadWriteStaticProperty).Should().Be(5);
		}

		[ExcludedFact]
		public void NoPropertyTest()
		{
			Action act1 = () => Property.Of((TestClass t) => t.DynamicValue).Set(() => 5);
			Action act2 = () => Property.Of(() => TestClass.DynamicStaticValue).Set(() => 5);
			Action act3 = () => Property.Of((TestClass t) => t.GetDynamicValue()).Set(() => 5);

			act1.Should().Throw<MissingMemberException>();
			act2.Should().Throw<MissingMemberException>();
			act3.Should().Throw<MissingMemberException>();
		}

#if NETCOREAPP3_0
		[ExcludedFact]
		public async Task AsyncEnumerableMethodTest()
		{
			var fake = new Fake<AsyncEnumerableTestClass>();

			var sut = fake.Rewrite(f => f.GetValue());
			sut.Replace(f => f.GetDynamicValue).Return(7);

			await foreach (var value in sut.Execute())
			{
				value.Should().Be(7);
			}
		}
#endif

#if NETCOREAPP3_0
		private class AsyncEnumerableTestClass
		{
			public int GetDynamicValue => 5;

			public async IAsyncEnumerable<int> GetValue()
			{
				await Task.Yield();
				yield return GetDynamicValue;
			}
		}
#endif

		private class EnumerableTestClass
		{
			public int GetDynamicValue => 5;

			public IEnumerable<int> GetValue()
			{
				yield return GetDynamicValue;
			}
		}

		private class WhenTestClass
		{
			public int Prop { get; set; }

			public int SomeMethod()
			{
				Prop = -1;
				var x = TestClass.DynamicStaticValue;
				Prop = 1;
				return x + TestClass.DynamicStaticValue;
			}
		}

		private class GenericTestClass<T>
		{
			public KeyValuePair<T, string> Pair => new KeyValuePair<T, string>(default, "test");
			public KeyValuePair<T, string> GetValue(T x, string y) => Pair;
		}

		private class TestClass
		{
			public int DynamicValue => 5;
			public static int DynamicStaticValue => 5;
			public int ReadWriteProperty { get; set; } = 5;
			public static int ReadWriteStaticProperty { get; set; } = 5;

			public void SetReadWriteProperty(int value)
			{
				ReadWriteProperty = value;
			}

			public static void SetReadWriteStaticProperty(int value)
			{
				ReadWriteStaticProperty = value;
			}

			public int GetDynamicValue()
			{
				Debug.WriteLine("Started");
				var value = DynamicValue;
				Debug.WriteLine("Finished");
				return value;
			}

			public int GetHelperDynamicValue()
			{
				Debug.WriteLine("Started");
				var helper = new HelperClass();
				var value = helper.DynamicValue;
				Debug.WriteLine("Finished");
				return value;
			}

			public int GetDynamicStaticValue()
			{
				Debug.WriteLine("Started");
				var value = DynamicStaticValue;
				Debug.WriteLine("Finished");
				return value;
			}

			public int GetHelperDynamicStaticValue()
			{
				Debug.WriteLine("Started");
				var value = HelperClass.DynamicStaticValue;
				Debug.WriteLine("Finished");
				return value;
			}

			public string GetFrameworkValue()
			{
				Debug.WriteLine("Started");
				var cmd = new SqlCommand();
				var vaue = cmd.CommandText;
				Debug.WriteLine("Finished");
				return vaue;
			}
			public DateTime GetFrameworkStaticValue()
			{
				Debug.WriteLine("Started");
				var vaue = DateTime.Now;
				Debug.WriteLine("Finished");
				return vaue;
			}
		}

		private class HelperClass
		{
			public int DynamicValue => 5;
			public static int DynamicStaticValue => 5;
		}
	}
}
