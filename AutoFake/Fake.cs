using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AutoFake.Expression;
using AutoFake.Setup;
using AutoFake.Setup.Configurations;
using DryIoc;

namespace AutoFake
{
    public class Fake<T> : Fake
    {
        public Fake(params object[] constructorArgs) : base(typeof(T), constructorArgs)
        {
        }

        public FuncMockConfiguration<T, TReturn> Rewrite<TReturn>(Expression<Func<T, TReturn>> expression) => base.Rewrite(expression);
        
        public ActionMockConfiguration<T> Rewrite(Expression<Action<T>> expression) => base.Rewrite(expression);

        public TReturn Execute<TReturn>(Expression<Func<T, TReturn>> expression) => base.Execute(expression);

        public void Execute(Expression<Action<T>> expression) => base.Execute(expression);
    }

    public class Fake
    {
	    private readonly object?[] _dependencies;
	    private FakeObjectInfo? _fakeObjectInfo;

        public Fake(Type type, params object?[] constructorArgs)
        {
	        if (type == null) throw new ArgumentNullException(nameof(type));
            _dependencies = constructorArgs ?? throw new ArgumentNullException(nameof(constructorArgs));
            Services = new Container(rules => rules.WithFuncAndLazyWithoutRegistration());
            Services = ContainerExtensions.CreateContainer(type, this);
            Options = Services.Resolve<FakeOptions>();
        }

        public Container Services { get; }

        public FakeOptions Options { get; }

        internal ITypeInfo TypeInfo => Services.Resolve<ITypeInfo>();

        public FuncMockConfiguration<TInput, TReturn> Rewrite<TInput, TReturn>(Expression<Func<TInput, TReturn>> expression)
        {
	        using var scope = Services.AddInvocationExpression(expression, addMocks: true);
	        return scope.Resolve<FuncMockConfiguration<TInput, TReturn>>();
        }

        public ActionMockConfiguration<TInput> Rewrite<TInput>(Expression<Action<TInput>> expression)
        {
	        using var scope = Services.AddInvocationExpression(expression, addMocks: true);
            return scope.Resolve<ActionMockConfiguration<TInput>>();
        }

        public FuncMockConfiguration<TReturn> Rewrite<TReturn>(Expression<Func<TReturn>> expression)
        {
	        using var scope = Services.AddInvocationExpression(expression, addMocks: true);
            return scope.Resolve<FuncMockConfiguration<TReturn>>();
        }

        public ActionMockConfiguration Rewrite(Expression<Action> expression)
        {
	        using var scope = Services.AddInvocationExpression(expression, addMocks: true);
            return scope.Resolve<ActionMockConfiguration>();
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
