﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AutoFake.Expression;
using AutoFake.Setup;
using AutoFake.Setup.Configurations;
using AutoFake.Setup.Mocks;
using DryIoc;
using InvocationExpression = AutoFake.Expression.InvocationExpression;

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
        private FakeObjectInfo? _fakeObjectInfo;
        private readonly List<FakeDependency> _dependencies;

        public Fake(Type type, params object?[] constructorArgs)
        {
	        if (type == null) throw new ArgumentNullException(nameof(type));
            if (constructorArgs == null) throw new ArgumentNullException(nameof(constructorArgs));

            _dependencies = constructorArgs.Select(c => c as FakeDependency ?? new FakeDependency(c?.GetType(), c)).ToList();
            Options = new FakeOptions();
            Services = new Container(rules => rules.WithFuncAndLazyWithoutRegistration());
            Services.AddServices(type, Options);
            Mocks = Services.Resolve<MockCollection>();
        }

        public Container Services { get; }

        public FakeOptions Options { get; }

        internal ITypeInfo TypeInfo => Services.Resolve<ITypeInfo>();

        internal MockCollection Mocks { get; }
        
        public FuncMockConfiguration<TInput, TReturn> Rewrite<TInput, TReturn>(Expression<Func<TInput, TReturn>> expression)
        {
	        using var scope = Services.OpenScope(expression);
	        Services.AddInvocationExpression(expression);
	        var invocationExpression = scope.Resolve<IInvocationExpression>();
            var mocks = GetMocksContainer(invocationExpression);
            return new FuncMockConfiguration<TInput, TReturn>(mocks, Services.Resolve<IProcessorFactory>(), new Executor<TReturn>(this, invocationExpression));
        }

        public ActionMockConfiguration<TInput> Rewrite<TInput>(Expression<Action<TInput>> expression)
        {
	        using var scope = Services.OpenScope(expression);
	        Services.AddInvocationExpression(expression);
	        var invocationExpression = scope.Resolve<IInvocationExpression>();
            var mocks = GetMocksContainer(invocationExpression);
            return new ActionMockConfiguration<TInput>(mocks, Services.Resolve<IProcessorFactory>(), new Executor(this, invocationExpression));
        }

        public FuncMockConfiguration<TReturn> Rewrite<TReturn>(Expression<Func<TReturn>> expression)
        {
	        using var scope = Services.OpenScope(expression);
	        Services.AddInvocationExpression(expression);
	        var invocationExpression = scope.Resolve<IInvocationExpression>();
            var mocks = GetMocksContainer(invocationExpression);
            return new FuncMockConfiguration<TReturn>(mocks, Services.Resolve<IProcessorFactory>(), new Executor<TReturn>(this, invocationExpression));
        }

        public ActionMockConfiguration Rewrite(Expression<Action> expression)
        {
	        using var scope = Services.OpenScope(expression);
	        Services.AddInvocationExpression(expression);
	        var invocationExpression = scope.Resolve<IInvocationExpression>();
            var mocks = GetMocksContainer(invocationExpression);
            return new ActionMockConfiguration(mocks, Services.Resolve<IProcessorFactory>(), new Executor(this, invocationExpression));
        }

        public TReturn Execute<TInput, TReturn>(Expression<Func<TInput, TReturn>> expression)
        {
            var invocationExpression = new InvocationExpression(expression ?? throw new ArgumentNullException(nameof(expression)));
            var executor = new Executor<TReturn>(this, invocationExpression);
            return executor.Execute();
        }

        public void Execute<TInput>(Expression<Action<TInput>> expression)
        {
            var invocationExpression = new InvocationExpression(expression ?? throw new ArgumentNullException(nameof(expression)));
            var executor = new Executor(this, invocationExpression);
            executor.Execute();
        }

        public TReturn Execute<TReturn>(Expression<Func<TReturn>> expression)
        {
            var invocationExpression = new InvocationExpression(expression ?? throw new ArgumentNullException(nameof(expression)));
            var executor = new Executor<TReturn>(this, invocationExpression);
            return executor.Execute();
        }

        public void Execute(Expression<Action> expression)
        {
            var invocationExpression = new InvocationExpression(expression ?? throw new ArgumentNullException(nameof(expression)));
            var executor = new Executor(this, invocationExpression);
            executor.Execute();
        }

        internal FakeObjectInfo GetFakeObject()
        {
	        if (_fakeObjectInfo == null)
	        {
                var asmWriter = Services.Resolve<IAssemblyWriter>();
                var fakeProcessor = new FakeProcessor(TypeInfo, asmWriter, Options);
		        foreach (var mock in Mocks)
		        {
			        fakeProcessor.ProcessMethod(mock.Mocks, mock.InvocationExpression);
		        }

                _fakeObjectInfo = asmWriter.CreateFakeObject(Mocks, _dependencies);
	        }
            return _fakeObjectInfo;
        }

        private IList<IMock> GetMocksContainer(IInvocationExpression invocationExpression)
        {
            var mocks = new List<IMock>();
            Mocks.Add(invocationExpression, mocks);
            return mocks;
        }
    }
}
