using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Expression;
using AutoFake.Abstractions.Setup;
using AutoFake.Abstractions.Setup.Configurations;
using DryIoc;

namespace AutoFake;

#pragma warning disable AF0001 // Public by design
public class Fake<T> : Fake, IExecutor<T>, IFakeObjectInfoSource
#pragma warning restore AF0001
{
	public Fake(params object[] constructorArgs)
		: base(typeof(T), constructorArgs, typeof(IFakeObjectInfoSource), typeof(IExecutor<T>), typeof(IExecutor<object>))
	{
	}

	public IFuncMockConfiguration<T, TReturn> Rewrite<TReturn>(Expression<Func<T, TReturn>> expression)
	{
		using var scope = this.AddInvocationExpression(expression, addMocks: true);
		return scope.Resolve<IFuncMockConfiguration<T, TReturn>>();
	}

	public IActionMockConfiguration<T> Rewrite(Expression<Action<T>> expression)
	{
		using var scope = this.AddInvocationExpression(expression, addMocks: true);
		return scope.Resolve<IActionMockConfiguration<T>>();
	}

	public TReturn Execute<TReturn>(Expression<Func<T, TReturn>> expression) => base.Execute(expression);

	public void Execute(Expression<Action<T>> expression) => base.Execute(expression);
}

#pragma warning disable AF0001 // Public by design
public class Fake : IExecutor<object>, IFakeObjectInfoSource
#pragma warning restore AF0001
{
	private readonly object?[] _dependencies;
	private IFakeObjectInfo? _fakeObjectInfo;

	public Fake(Type type, params object?[] constructorArgs)
		: this(type, constructorArgs, typeof(IFakeObjectInfoSource), typeof(IExecutor<object>))
	{
	}

	protected Fake(Type type, object?[] constructorArgs, params Type[] fakeServiceTypes)
	{
		if (type == null) throw new ArgumentNullException(nameof(type));
		_dependencies = constructorArgs ?? new object?[] { null };
		Services = ContainerExtensions.CreateContainer(type, svc => svc.RegisterInstanceMany(fakeServiceTypes, this));
		Options = Services.Resolve<IFakeOptions>();
	}

	public Dictionary<Type, Func<object, object>> OnScopedServiceRegistration { get; } = new();

	public Container Services { get; }

	public IFakeOptions Options { get; }

	public IFuncMockConfiguration<object, TReturn> Rewrite<TInput, TReturn>(Expression<Func<TInput, TReturn>> expression)
	{
		using var scope = this.AddInvocationExpression(expression, addMocks: true);
		return scope.Resolve<IFuncMockConfiguration<object, TReturn>>();
	}

	public IActionMockConfiguration<object> Rewrite<TInput>(Expression<Action<TInput>> expression)
	{
		using var scope = this.AddInvocationExpression(expression, addMocks: true);
		return scope.Resolve<IActionMockConfiguration<object>>();
	}

	public IFuncMockConfiguration<object, TReturn> Rewrite<TReturn>(Expression<Func<TReturn>> expression)
	{
		using var scope = this.AddInvocationExpression(expression, addMocks: true);
		return scope.Resolve<IFuncMockConfiguration<object, TReturn>>();
	}

	public IActionMockConfiguration<object> Rewrite(Expression<Action> expression)
	{
		using var scope = this.AddInvocationExpression(expression, addMocks: true);
		return scope.Resolve<IActionMockConfiguration<object>>();
	}

	TReturn IExecutor<object>.Execute<TReturn>(Expression<Func<object, TReturn>> expression) => Execute(expression);
	public TReturn Execute<TInput, TReturn>(Expression<Func<TInput, TReturn>> expression)
	{
		using var scope = this.AddInvocationExpression(expression);
		return scope.Resolve<IExpressionExecutor<TReturn>>().Execute();
	}

	void IExecutor<object>.Execute(Expression<Action<object>> expression) => Execute(expression);
	public void Execute<TInput>(Expression<Action<TInput>> expression)
	{
		using var scope = this.AddInvocationExpression(expression);
		scope.Resolve<IExpressionExecutor>().Execute();
	}

	public TReturn Execute<TReturn>(Expression<Func<TReturn>> expression)
	{
		using var scope = this.AddInvocationExpression(expression);
		return scope.Resolve<IExpressionExecutor<TReturn>>().Execute();
	}

	public void Execute(Expression<Action> expression)
	{
		using var scope = this.AddInvocationExpression(expression);
		scope.Resolve<IExpressionExecutor>().Execute();
	}

	IFakeObjectInfo IFakeObjectInfoSource.GetFakeObject() => GetFakeObject();
	internal IFakeObjectInfo GetFakeObject()
	{
		if (_fakeObjectInfo == null)
		{
			var fakeProcessor = Services.Resolve<IFakeProcessor>();
			var setups = Services.Resolve<KeyValuePair<IInvocationExpression, IMockCollection>[]>();
			foreach (var mocks in setups)
			{
				fakeProcessor.ProcessMethod(mocks.Value, mocks.Key, Options);
			}

			var asmWriter = Services.Resolve<IAssemblyWriter>();
			_fakeObjectInfo = asmWriter.CreateFakeObject(setups.SelectMany(s => s.Value.Mocks), _dependencies);
		}
		return _fakeObjectInfo;
	}
}
