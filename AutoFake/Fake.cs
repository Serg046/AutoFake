using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Expression;
using AutoFake.Abstractions.Setup;
using AutoFake.Abstractions.Setup.Configurations;
using DryIoc;

namespace AutoFake
{
#pragma warning disable AF0001 // Public by design
	public class Fake<T> : Fake
#pragma warning restore AF0001
	{
        public Fake(params object[] constructorArgs) : base(typeof(T), constructorArgs)
        {
        }

        public IFuncMockConfiguration<T, TReturn> Rewrite<TReturn>(Expression<Func<T, TReturn>> expression) => base.Rewrite(expression);
        
        public IActionMockConfiguration<T> Rewrite(Expression<Action<T>> expression) => base.Rewrite(expression);

        public TReturn Execute<TReturn>(Expression<Func<T, TReturn>> expression) => base.Execute(expression);

        public void Execute(Expression<Action<T>> expression) => base.Execute(expression);
    }

#pragma warning disable AF0001 // Public by design
    public class Fake
#pragma warning restore AF0001
    {
	    private readonly object?[] _dependencies;
	    private FakeObjectInfo? _fakeObjectInfo;

        public Fake(Type type, params object?[] constructorArgs)
        {
	        if (type == null) throw new ArgumentNullException(nameof(type));
            _dependencies = constructorArgs ?? throw new ArgumentNullException(nameof(constructorArgs));
            Services = ContainerExtensions.CreateContainer(type, this);
            Options = Services.Resolve<IFakeOptions>();
        }

        public Container Services { get; }

        public IFakeOptions Options { get; }

        public IFuncMockConfiguration<TInput, TReturn> Rewrite<TInput, TReturn>(Expression<Func<TInput, TReturn>> expression)
        {
	        using var scope = Services.AddInvocationExpression(expression, addMocks: true);
	        return scope.Resolve<IFuncMockConfiguration<TInput, TReturn>>();
        }

        public IActionMockConfiguration<TInput> Rewrite<TInput>(Expression<Action<TInput>> expression)
        {
	        using var scope = Services.AddInvocationExpression(expression, addMocks: true);
            return scope.Resolve<IActionMockConfiguration<TInput>>();
        }

        public IFuncMockConfiguration<TReturn> Rewrite<TReturn>(Expression<Func<TReturn>> expression)
        {
	        using var scope = Services.AddInvocationExpression(expression, addMocks: true);
            return scope.Resolve<IFuncMockConfiguration<TReturn>>();
        }

        public IActionMockConfiguration Rewrite(Expression<Action> expression)
        {
	        using var scope = Services.AddInvocationExpression(expression, addMocks: true);
            return scope.Resolve<IActionMockConfiguration>();
        }

        public TReturn Execute<TInput, TReturn>(Expression<Func<TInput, TReturn>> expression)
        {
	        using var scope = Services.AddInvocationExpression(expression);
            return scope.Resolve<Executor<TReturn>>().Execute();
        }

        public void Execute<TInput>(Expression<Action<TInput>> expression)
        {
            using var scope = Services.AddInvocationExpression(expression);
            scope.Resolve<Executor>().Execute();
        }

        public TReturn Execute<TReturn>(Expression<Func<TReturn>> expression)
        {
            using var scope = Services.AddInvocationExpression(expression);
            return scope.Resolve<Executor<TReturn>>().Execute();
        }

        public void Execute(Expression<Action> expression)
        {
            using var scope = Services.AddInvocationExpression(expression);
            scope.Resolve<Executor>().Execute();
        }

        internal FakeObjectInfo GetFakeObject()
        {
	        if (_fakeObjectInfo == null)
	        {
                var fakeProcessor = Services.Resolve<IFakeProcessor>();
                var setups = Services.Resolve<KeyValuePair<IInvocationExpression, IMockCollection>[]>();
				foreach (var mocks in setups)
				{
					fakeProcessor.ProcessMethod(mocks.Value, mocks.Key);
				}

                var asmWriter = Services.Resolve<IAssemblyWriter>();
                _fakeObjectInfo = asmWriter.CreateFakeObject(setups.SelectMany(s => s.Value), _dependencies);
			}
            return _fakeObjectInfo;
        }
    }
}
