using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AutoFake.Expression;
using AutoFake.Setup;
using AutoFake.Setup.Configurations;
using AutoFake.Setup.Mocks;
using InvocationExpression = AutoFake.Expression.InvocationExpression;

namespace AutoFake
{
    public class Fake<T> : Fake
    {
        public Fake(params object[] contructorArgs) : base(typeof(T), contructorArgs)
        {
        }

        public FuncMockConfiguration<T, TReturn> Rewrite<TReturn>(Expression<Func<T, TReturn>> expression) => base.Rewrite(expression);
        
        public ActionMockConfiguration<T> Rewrite(Expression<Action<T>> expression) => base.Rewrite(expression);

        public void RewriteContract<TReturn>(Expression<Func<T, TReturn>> expression) => RewriteContractImpl(expression);

        public void RewriteContract(Expression<Action<T>> expression) => RewriteContractImpl(expression);

        public TReturn Execute<TReturn>(Expression<Func<T, TReturn>> expression) => base.Execute(expression);

        public void Execute(Expression<Action<T>> expression) => base.Execute(expression);
    }

    public class Fake
    {
        private FakeObjectInfo _fakeObjectInfo;
        private readonly Lazy<ITypeInfo> _typeInfoHelper;

        public Fake(Type type, params object[] constructorArgs)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (constructorArgs == null) throw new ArgumentNullException(nameof(constructorArgs));

            var dependencies = constructorArgs.Select(c => c as FakeDependency ?? new FakeDependency(c?.GetType(), c)).ToList();
            Mocks = new MockCollection();
            Options = new FakeOptions();
            _typeInfoHelper = new Lazy<ITypeInfo>(() => new TypeInfo(type, dependencies, Options));
        }

        public FakeOptions Options { get; }

        internal ITypeInfo TypeInfo => _typeInfoHelper.Value;

        internal MockCollection Mocks { get; }
        
        public FuncMockConfiguration<TInput, TReturn> Rewrite<TInput, TReturn>(Expression<Func<TInput, TReturn>> expression)
        {
            var invocationExpression = new InvocationExpression(expression ?? throw new ArgumentNullException(nameof(expression)));
            var mocks = GetMocksContainer(invocationExpression);
            return new FuncMockConfiguration<TInput, TReturn>(mocks, new ProcessorFactory(TypeInfo),
                new Executor<TReturn>(this, invocationExpression));
        }

        public ActionMockConfiguration<TInput> Rewrite<TInput>(Expression<Action<TInput>> expression)
        {
            var invocationExpression = new InvocationExpression(expression ?? throw new ArgumentNullException(nameof(expression)));
            var mocks = GetMocksContainer(invocationExpression);
            return new ActionMockConfiguration<TInput>(mocks, new ProcessorFactory(TypeInfo),
                new Executor(this, invocationExpression));
        }

        public FuncMockConfiguration<TReturn> Rewrite<TReturn>(Expression<Func<TReturn>> expression)
        {
            var invocationExpression = new InvocationExpression(expression ?? throw new ArgumentNullException(nameof(expression)));
            var mocks = GetMocksContainer(invocationExpression);
            return new FuncMockConfiguration<TReturn>(mocks, new ProcessorFactory(TypeInfo),
                new Executor<TReturn>(this, invocationExpression));
        }

        public ActionMockConfiguration Rewrite(Expression<Action> expression)
        {
            var invocationExpression = new InvocationExpression(expression ?? throw new ArgumentNullException(nameof(expression)));
            var mocks = GetMocksContainer(invocationExpression);
            return new ActionMockConfiguration(mocks, new ProcessorFactory(TypeInfo),
                new Executor(this, invocationExpression));
        }

        public void RewriteContract<TInput, TReturn>(Expression<Func<TInput, TReturn>> expression) => RewriteContractImpl(expression);

        public void RewriteContract<TInput>(Expression<Action<TInput>> expression) => RewriteContractImpl(expression);

        public void RewriteContract<TReturn>(Expression<Func<TReturn>> expression) => RewriteContractImpl(expression);

        public void RewriteContract(Expression<Action> expression) => RewriteContractImpl(expression);

        protected void RewriteContractImpl(System.Linq.Expressions.Expression expression)
        {
	        var invocationExpression = new InvocationExpression(expression ?? throw new ArgumentNullException(nameof(expression)));
	        var visitor = new GetTestMethodVisitor();
	        invocationExpression.AcceptMemberVisitor(visitor);
	        var fakeProcessor = new FakeProcessor(TypeInfo, Options);
            fakeProcessor.ProcessMethod(visitor.Method);
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
            if (_fakeObjectInfo == null) _fakeObjectInfo = TypeInfo.CreateFakeObject(Mocks);
            return _fakeObjectInfo;
        }

        private IList<IMock> GetMocksContainer(IInvocationExpression invocationExpression)
        {
            var visitor = new GetTestMethodVisitor();
            invocationExpression.AcceptMemberVisitor(visitor);
            var mocks = new List<IMock>();
            Mocks.Add(visitor.Method, mocks);
            return mocks;
        }
    }
}
