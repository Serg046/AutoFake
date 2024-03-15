using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;
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
	public Fake([CallerFilePath] string stringKey = "", [CallerLineNumber] int intKey = 0)
		: base(typeof(T), GetKey(stringKey, intKey), typeof(IFakeObjectInfoSource), typeof(IExecutor<T>), typeof(IExecutor<object>))
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
	private IFakeObjectInfo? _fakeObjectInfo;

	public Fake(Type type, [CallerFilePath] string stringKey = "", [CallerLineNumber] int intKey = 0)
		: this(type, GetKey(stringKey, intKey), typeof(IFakeObjectInfoSource), typeof(IExecutor<object>))
	{
	}

	protected Fake(Type type, string key, params Type[] fakeServiceTypes)
	{
		if (type == null) throw new ArgumentNullException(nameof(type));

		Services = ContainerExtensions.CreateContainer(type, key, svc => svc.RegisterInstanceMany(fakeServiceTypes, this));
		Options = Services.Resolve<IOptions>();
	}

	public Dictionary<Type, Func<object, object>> OnScopedServiceRegistration { get; } = new();

	public Container Services { get; }

	public IOptions Options { get; }

	protected static string GetKey(string stringKey, int intKey)
	{
		var dir = Directory.GetCurrentDirectory();
		var i = 0;
		for (; i < stringKey.Length; i++)
		{
			if (stringKey[i] != dir[i]) break;
		}

		stringKey = stringKey.Substring(i);
		return Convert.ToBase64String(Encoding.UTF8.GetBytes(stringKey + intKey)).Replace("=", "");
	}

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

	// todo: extract into a separate type
	IFakeObjectInfo IFakeObjectInfoSource.GetFakeObject() => GetFakeObject();
	internal IFakeObjectInfo GetFakeObject()
	{
		if (_fakeObjectInfo == null)
		{
			if (AssemblyLoadContext.CurrentContextualReflectionContext is AssemblyLoadContext and { Name: "FakeContext" } host)
			{
				_fakeObjectInfo = GetFakeObject(host);
			}
			else
			{
				_fakeObjectInfo = BuildFakeObject();
			}
		}

		return _fakeObjectInfo;
	}

	private IFakeObjectInfo GetFakeObject(AssemblyLoadContext host)
	{
		var setups = Services.Resolve<KeyValuePair<IInvocationExpression, IMockCollection>[]>();
		var assemblyReader = Services.Resolve<IAssemblyReader>();
		
		// todo: should be an extension
		var assembly = host.Assemblies.Single(a => a.FullName == assemblyReader.SourceType.Assembly.FullName);
		var type = assembly.GetType(assemblyReader.SourceType.FullName);
		var instance = Activator.CreateInstance(type);

		foreach (var setup in setups)
		{
			var visitor = Services.Resolve<IMemberVisitorFactory>().GetMemberVisitor<IGetTestMethodVisitor>();
			var method = setup.Key.AcceptMemberVisitor(visitor);
			foreach (var mock in setup.Value.Mocks)
			{
				mock.Initialize(type, method.Name);
			}
		}

		return new FakeObjectInfo(type, instance);
	}

	private IFakeObjectInfo BuildFakeObject()
	{
		var fakeProcessor = Services.Resolve<IFakeProcessor>();
		var setups = Services.Resolve<KeyValuePair<IInvocationExpression, IMockCollection>[]>();
		foreach (var mocks in setups)
		{
			fakeProcessor.ProcessMethod(mocks.Value, mocks.Key, Options);
		}

		var asmWriter = Services.Resolve<IAssemblyWriter>();
		return asmWriter.CreateFakeObject();
	}
}
