using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup;
using AutoFake.Abstractions.Setup.Mocks;
using FluentAssertions;
using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace AutoFake.FunctionalTests
{
	public class FakeTests
	{
		[Fact]
		public void When_null_as_fake_type_Should_fail()
		{
			Action act = () => new Fake(null);

			act.Should().Throw<ArgumentNullException>();
		}

		[Fact]
		public void When_generic_instance_void_action_Should_execute()
		{
			var fake = new Fake<TestClass>();

			fake.Execute(f => f.SetProp());

			fake.Execute(f => f.Prop).Should().Be(5);
		}

		[Fact]
		public void When_instance_void_action_Should_execute()
		{
			var fake = new Fake(typeof(TestClass));

			fake.Execute((TestClass f) => f.SetProp());

			fake.Execute((TestClass f) => f.Prop).Should().Be(5);
		}

		[Fact]
		public void When_static_void_action_Should_execute()
		{
			var fake = new Fake(typeof(TestClass));

			fake.Execute(() => TestClass.SetStaticProp());

			fake.Execute(() => TestClass.StaticProp).Should().Be(5);
		}

		[Fact]
		public void When_instance_void_action_executor_Should_execute()
		{
			var fake = new Fake(typeof(TestClass));

			var sut = fake.Rewrite((TestClass f) => f.SetProp());
			sut.Replace((TestClass t) => t.GetFive()).Return(6).When(HandleArgument);

			sut.Execute();
			fake.Execute((TestClass f) => f.Prop).Should().Be(6);

			static bool HandleArgument(IExecutor<object> executor)
			{
				executor.Execute((TestClass t) => t.SetProp(3));
				return executor.Execute((TestClass t) => t.Prop) == 3;
			}
		}

		[Fact]
		public void When_static_void_action_executor_Should_execute()
		{
			var fake = new Fake(typeof(TestClass));

			var sut = fake.Rewrite(() => TestClass.SetStaticProp());
			sut.Replace(() => TestClass.GetStaticFive()).Return(6).When(HandleArgument);

			sut.Execute();
			fake.Execute(() => TestClass.StaticProp).Should().Be(6);

			static bool HandleArgument(IExecutor<object> executor)
			{
				executor.Execute(() => TestClass.SetStaticProp(3));
				return executor.Execute(() => TestClass.StaticProp) == 3;
			}
		}

		[Fact]
		public void When_object_func_executor_Should_execute()
		{
			var fake = new Fake(typeof(TestClass));

			var sut = fake.Rewrite((TestClass f) => f.SetProp());
			sut.Replace((TestClass t) => t.GetFive()).Return(6)
				.When(exe => exe.Execute(obj => obj.GetType()).FullName == typeof(TestClass).FullName);

			sut.Execute();
			fake.Execute((TestClass f) => f.Prop).Should().Be(6);
		}

		[Fact]
		public void When_object_action_executor_Should_execute()
		{
			var fake = new Fake(typeof(TestClass));

			var sut = fake.Rewrite((TestClass f) => f.SetProp());
			sut.Replace((TestClass t) => t.GetFive()).Return(6).When(HandleArgument);
			Action act = () => sut.Execute();

			act.Should().Throw<MissingMethodException>().WithMessage("*ThrowNotImplementedException*not found*");

			static bool HandleArgument(IExecutor<object> executor)
			{
				executor.Execute(obj => ThrowNotImplementedException(obj));
				return true;
			}
		}

		[Fact]
		public void When_scoped_service_overriden_Should_apply()
		{
			var fake = new Fake<TestClass>();
			fake.OnScopedServiceRegistration.Add(typeof(IMockCollection), m => new FakeMockCollection(new EmptyList<IMock>()));

			var sut = fake.Rewrite(f => f.SetProp());
			sut.Replace(f => f.GetFive()).Return(7);

			sut.Execute();
			fake.Execute(f => f.Prop).Should().Be(5);
		}

		private static void ThrowNotImplementedException(object obj) => throw new NotImplementedException();

		private class TestClass
		{
			public static int StaticProp { get; private set; }

			public int Prop { get; private set; }

			public void SetProp() => Prop = GetFive();
			public void SetProp(int prop) => Prop = prop;
			public static void SetStaticProp() => StaticProp = GetStaticFive();
			public static void SetStaticProp(int prop) => StaticProp = prop;
			public int GetFive() => 5;
			public static int GetStaticFive() => 5;
		}

		private class FakeMockCollection : IMockCollection
		{
			private readonly IList<IMock> _mocks;
			public FakeMockCollection(IList<IMock> mocks) => _mocks = mocks;
			public IList<IMock> Mocks => _mocks;
			public ISet<IMockInjector> ContractMocks { get; } = new HashSet<IMockInjector>();
		}

		private class EmptyList<T> : IList<T>
		{
			private readonly List<T> _list = new();
			public T this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
			public int Count => throw new NotImplementedException();
			public bool IsReadOnly => throw new NotImplementedException();
			public void Clear() => _list.Clear();
			public bool Contains(T item) => _list.Contains(item);
			public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
			public int IndexOf(T item) => _list.IndexOf(item);
			public void Insert(int index, T item) => _list.Insert(index, item);
			public bool Remove(T item) => _list.Remove(item);
			public void RemoveAt(int index) => _list.RemoveAt(index);
			public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
			IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

			public void Add(T item) { }
		}
	}
}
