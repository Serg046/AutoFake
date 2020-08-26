using System;
using System.Collections.Generic;
using System.IO;
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

        public TReturn Execute<TReturn>(Expression<Func<T, TReturn>> expression) => base.Execute(expression);

        public void Execute(Expression<Action<T>> expression) => base.Execute(expression);
    }

    public class Fake
    {
        private FakeObjectInfo _fakeObjectInfo;

        public Fake(Type type, params object[] constructorArgs)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (constructorArgs == null) throw new ArgumentNullException(nameof(constructorArgs));

            var dependencies = constructorArgs.Select(c => c as FakeDependency ?? new FakeDependency(c?.GetType(), c)).ToList();
            TypeInfo = new TypeInfo(type, dependencies);
            Mocks = new MockCollection();
            Options = new FakeOptions();
        }

        public FakeOptions Options { get; }

        internal ITypeInfo TypeInfo { get; }

        internal MockCollection Mocks { get; }

        public void SaveFakeAssembly(string fileName)
        {
            using (var fileStream = File.Create(fileName))
            {
                TypeInfo.WriteAssembly(fileStream);
            }
        }

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

        public void Release()
        {
            if (_fakeObjectInfo == null) throw new InvalidOperationException("Nothing to release yet");
            _fakeObjectInfo.Dispose();
        }

        internal FakeObjectInfo CreateFakeObject()
        {
            if (_fakeObjectInfo == null) _fakeObjectInfo = TypeInfo.CreateFakeObject(Mocks, Options);
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
