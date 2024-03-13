using FluentAssertions;
using System;
using System.Globalization;
using System.Reflection;
using Xunit;
using LinqExpression = System.Linq.Expressions.Expression;

namespace AutoFake.FunctionalTests.TypeMemberMocks.InstanceTests;

public class IndexerMockTests
{
	[ExcludedFact]
	public void GetterTest()
	{
		var fake = new Fake<TestClass>();

		var sut = fake.Rewrite(f => f.SetAndGetIndexer(1, "2", true, "test"));
		sut.Replace(f => f[1, Arg.IsAny<string>(), true]).Return("newTestValue");

		sut.Execute().Should().Be("newTestValue");
	}

	[ExcludedFact]
	public void SetterTest()
	{
		var fake = new Fake<TestClass>();

		var sut = fake.Rewrite(f => f.SetAndGetIndexer(1, "2", true, "test"));
		sut.Remove(Indexer.Of((TestClass t) => t[1, "2", true]).Set(() => "test"));

		sut.Execute().Should().Be("0False");
	}

	[ExcludedFact]
	public void NoIndexerTest()
	{
		Action act1 = () => Indexer.Of((TestClass t) => t.ReadOnlyProperty).Set(() => "test");
		Action act2 = () => Indexer.Of((TestClass t) => t.SetAndGetIndexer(1, "2", true, "test")).Set(() => "test");

		act1.Should().Throw<MissingMemberException>();
		act2.Should().Throw<MissingMemberException>();
	}

	[ExcludedFact]
	public void NoIndexerDeclaringTypeTest()
	{
		var member = LinqExpression.Call(null, new MethodInfoFake<string>());
		var lambda = LinqExpression.Lambda<Func<TestClass, string>>(member, LinqExpression.Parameter(typeof(TestClass)));
		Action act = () => Indexer.Of(lambda).Set(() => "test");

		act.Should().Throw<MissingMemberException>();
	}

	private class TestClass
	{
		private string _indexerValue;
		private int _x; private string _y; private bool _z;

		public string ReadOnlyProperty { get; }

		public string this[int x, string y, bool z]
		{
			get => $"{_indexerValue}{_x}{_y}{_z}";
			set
			{
				_x = x;
				_y = y;
				_z = z;
				_indexerValue = value;
			}
		}

		public string SetAndGetIndexer(int x, string y, bool z, string value)
		{
			this[x, y, z] = value;
			return this[x, y, z];
		}
	}

	private class MethodInfoFake<T> : MethodInfo
	{
		public override Type DeclaringType => null;
		public override string Name => "get_Item";
		public override Type ReturnType => typeof(T);
		public override MethodAttributes Attributes => MethodAttributes.Public | MethodAttributes.Static;
		public override ParameterInfo[] GetParameters() => new ParameterInfo[0];
		public override ICustomAttributeProvider ReturnTypeCustomAttributes => throw new NotImplementedException();
		public override RuntimeMethodHandle MethodHandle => throw new NotImplementedException();
		public override Type ReflectedType => throw new NotImplementedException();
		public override MethodInfo GetBaseDefinition() => throw new NotImplementedException();
		public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();
		public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotImplementedException();
		public override MethodImplAttributes GetMethodImplementationFlags() => throw new NotImplementedException();
		public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) => throw new NotImplementedException();
		public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();
	}
}
