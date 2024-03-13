using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace AutoFake.FunctionalTests.TypeMemberMocks.InstanceTests
{
	public class FieldMockTests
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
		public void FrameworkTest()
		{
			var fake = new Fake<TestClass>();

			const float value = 78;
			var sut = fake.Rewrite(f => f.GetFrameworkValue());
			sut.Replace((Quaternion quaternion) => quaternion.X).Return(value);

			Assert.Equal(value, sut.Execute());
		}

		[ExcludedFact]
		public void FrameworkStaticTest()
		{
			var fake = new Fake<TestClass>();

			var sr = new StringReader(string.Empty);
			var sut = fake.Rewrite(f => f.GetFrameworkStaticValue());
			sut.Replace(() => TextReader.Null).Return(sr);

			var actual = sut.Execute();
			Assert.Equal(sr, actual);
			Assert.NotEqual(TextReader.Null, actual);
		}

		[ExcludedFact]
		public void StructFieldByAddress()
		{
			var fake = new Fake<TestClass>();

			var data = new HelperStruct { Value = 5 };
			var sut = fake.Rewrite(f => f.GetStructValueByAddress());
			sut.Replace(f => f.StructValue).Return(data);

			Assert.Equal(data.Value, sut.Execute());
		}

		[ExcludedFact]
		public void StaticStructFieldByAddress()
		{
			var fake = new Fake<TestClass>();

			var data = new HelperStruct { Value = 5 };
			var sut = fake.Rewrite(f => f.GetStaticStructValueByAddress());
			sut.Replace(() => TestClass.StaticStructValue).Return(data);

			Assert.Equal(data.Value, sut.Execute());
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
		public void AnotherGenericTest()
		{
			var fake = new Fake<TestClass>();
			const string stringValue = "testValue";
			const int intValue = 7;

			var sut = fake.Rewrite(f => f.GetGenericValue());
			sut.Replace((ValueTuple<string> tuple) => tuple.Item1).Return(stringValue);
			sut.Replace((ValueTuple<object> tuple) => tuple.Item1).Return(intValue);

			sut.Execute().Should().Be(stringValue + intValue);
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
			public int GetDynamicValue = 5;

			public async IAsyncEnumerable<int> GetValue()
			{
				await Task.Yield();
				yield return GetDynamicValue;
			}
		}
#endif

		private class EnumerableTestClass
		{
			public int GetDynamicValue = 5;

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
			public KeyValuePair<T, string> Pair = new KeyValuePair<T, string>(default, "test");
			public KeyValuePair<T, string> GetValue(T x, string y) => Pair;
		}

#pragma warning disable 0649
		private class TestClass
		{
			public int DynamicValue = 5;
			public static int DynamicStaticValue = 5;
			public HelperStruct StructValue;
			public static HelperStruct StaticStructValue;

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

			public float GetFrameworkValue()
			{
				Debug.WriteLine("Started");
				var quaternion = new Quaternion();
				var value = quaternion.X;
				Debug.WriteLine("Finished");
				return value;
			}
			public TextReader GetFrameworkStaticValue()
			{
				Debug.WriteLine("Started");
				var value = TextReader.Null;
				Debug.WriteLine("Finished");
				return value;
			}

			public int GetStructValueByAddress()
			{
				Debug.WriteLine("Started");
				var value = StructValue.Value;
				Debug.WriteLine("Finished");
				return value;
			}

			public int GetStaticStructValueByAddress()
			{
				Debug.WriteLine("Started");
				var value = StaticStructValue.Value;
				Debug.WriteLine("Finished");
				return value;
			}

			public string GetGenericValue()
			{
				Debug.WriteLine("Started");
				var tuple1 = new ValueTuple<string>();
				var tuple2 = new ValueTuple<object>();
				var value = tuple1.Item1 + tuple2.Item1;
				Debug.WriteLine("Finished");
				return value;
			}
		}

		private class HelperClass
		{
			public int DynamicValue = 5;
			public static int DynamicStaticValue = 5;
		}

		public struct HelperStruct
		{
			public int Value;
		}
	}
}
