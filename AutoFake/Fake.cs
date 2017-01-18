using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AutoFake.Expression;
using AutoFake.Setup;
using InvocationExpression = AutoFake.Expression.InvocationExpression;

namespace AutoFake
{
    public class Fake<T> : Fake
    {
        public Fake(params object[] contructorArgs) : base(typeof(T), contructorArgs)
        {
        }

        public ReplaceableMockInstaller<TReturn> Replace<TReturn>(Expression<Func<T, TReturn>> instanceSetupFunc)
        {
            Guard.NotNull(instanceSetupFunc, nameof(instanceSetupFunc));
            var invocationExpression = new InvocationExpression(instanceSetupFunc);
            return new ReplaceableMockInstaller<TReturn>(Mocks, invocationExpression);
        }

        public ReplaceableMockInstaller Replace(Expression<Action<T>> voidInstanceSetupFunc)
        {
            Guard.NotNull(voidInstanceSetupFunc, nameof(voidInstanceSetupFunc));
            var invocationExpression = new InvocationExpression(voidInstanceSetupFunc);
            return new ReplaceableMockInstaller(Mocks, invocationExpression);
        }

        //---------------------------------------------------------------------------------------------------------

        public VerifiableMockInstaller Verify<TReturn>(Expression<Func<T, TReturn>> instanceSetupFunc)
        {
            Guard.NotNull(instanceSetupFunc, nameof(instanceSetupFunc));
            var invocationExpression = new InvocationExpression(instanceSetupFunc);
            return new VerifiableMockInstaller(Mocks, invocationExpression);
        }

        public VerifiableMockInstaller Verify(Expression<Action<T>> voidInstanceSetupFunc)
        {
            Guard.NotNull(voidInstanceSetupFunc, nameof(voidInstanceSetupFunc));
            var invocationExpression = new InvocationExpression(voidInstanceSetupFunc);
            return new VerifiableMockInstaller(Mocks, invocationExpression);
        }

        //---------------------------------------------------------------------------------------------------------

        public Executor<TReturn> Rewrite<TReturn>(Expression<Func<T, TReturn>> instanceRewriteFunc)
        {
            Guard.NotNull(instanceRewriteFunc, nameof(instanceRewriteFunc));
            return RewriteImpl<TReturn>(instanceRewriteFunc);
        }

        public Executor Rewrite(Expression<Action<T>> voidInstanceRewriteFunc)
        {
            Guard.NotNull(voidInstanceRewriteFunc, nameof(voidInstanceRewriteFunc));
            return RewriteImpl(voidInstanceRewriteFunc);
        }

        //---------------------------------------------------------------------------------------------------------

        public TReturn Execute<TReturn>(Expression<Func<T, TReturn>> instanceExecuteFunc)
        {
            Guard.NotNull(instanceExecuteFunc, nameof(instanceExecuteFunc));
            return ExecuteImpl<TReturn>(instanceExecuteFunc);
        }

        public void Execute(Expression<Action<T>> voidInstanceExecuteFunc)
        {
            Guard.NotNull(voidInstanceExecuteFunc, nameof(voidInstanceExecuteFunc));
            ExecuteImpl(voidInstanceExecuteFunc);
        }
    }

    public class Fake
    {
        private readonly FakeGenerator _fakeGenerator;
        private readonly GeneratedObject _generatedObject;

        public Fake(Type type, params object[] contructorArgs)
        {
            Guard.NotNull(type, nameof(type));
            Guard.NotNull(contructorArgs, nameof(contructorArgs));

            var dependencies = contructorArgs.Select(c =>
            {
                var dependecy = c as FakeDependency;
                return dependecy ?? new FakeDependency(c?.GetType(), c);
            }).ToList();

            var typeInfo = new TypeInfo(type, dependencies);
            var mockerFactory = new MockerFactory();
            _generatedObject = new GeneratedObject(typeInfo);
            _fakeGenerator = new FakeGenerator(typeInfo, mockerFactory, _generatedObject);

            Mocks = new List<Mock>();
        }

        internal ICollection<Mock> Mocks { get; }

        //---------------------------------------------------------------------------------------------------------

        public void SaveFakeAssembly(string fileName)
        {
            Guard.NotNull(fileName, nameof(fileName));
            _fakeGenerator.Save(fileName);
        }

        //---------------------------------------------------------------------------------------------------------

        public ReplaceableMockInstaller<TReturn> Replace<TReturn>(Expression<Func<TReturn>> setupFunc)
        {
            Guard.NotNull(setupFunc, nameof(setupFunc));
            var invocationExpression = new InvocationExpression(setupFunc);
            return new ReplaceableMockInstaller<TReturn>(Mocks, invocationExpression);
        }

        public ReplaceableMockInstaller<TReturn> Replace<TInput, TReturn>(Expression<Func<TInput, TReturn>> instanceSetupFunc)
        {
            Guard.NotNull(instanceSetupFunc, nameof(instanceSetupFunc));
            var invocationExpression = new InvocationExpression(instanceSetupFunc);
            return new ReplaceableMockInstaller<TReturn>(Mocks, invocationExpression);
        }

        public ReplaceableMockInstaller Replace<TInput>(Expression<Action<TInput>> voidInstanceSetupFunc)
        {
            Guard.NotNull(voidInstanceSetupFunc, nameof(voidInstanceSetupFunc));
            var invocationExpression = new InvocationExpression(voidInstanceSetupFunc);
            return new ReplaceableMockInstaller(Mocks, invocationExpression);
        }

        public ReplaceableMockInstaller Replace(Expression<Action> voidInstanceSetupFunc)
        {
            Guard.NotNull(voidInstanceSetupFunc, nameof(voidInstanceSetupFunc));
            var invocationExpression = new InvocationExpression(voidInstanceSetupFunc);
            return new ReplaceableMockInstaller(Mocks, invocationExpression);
        }

        //---------------------------------------------------------------------------------------------------------

        public VerifiableMockInstaller Verify<TReturn>(Expression<Func<TReturn>> setupFunc)
        {
            Guard.NotNull(setupFunc, nameof(setupFunc));
            var invocationExpression = new InvocationExpression(setupFunc);
            return new VerifiableMockInstaller(Mocks, invocationExpression);
        }

        public VerifiableMockInstaller Verify<TInput, TReturn>(Expression<Func<TInput, TReturn>> instanceSetupFunc)
        {
            Guard.NotNull(instanceSetupFunc, nameof(instanceSetupFunc));
            var invocationExpression = new InvocationExpression(instanceSetupFunc);
            return new VerifiableMockInstaller(Mocks, invocationExpression);
        }

        public VerifiableMockInstaller Verify<TInput>(Expression<Action<TInput>> voidInstanceSetupFunc)
        {
            Guard.NotNull(voidInstanceSetupFunc, nameof(voidInstanceSetupFunc));
            var invocationExpression = new InvocationExpression(voidInstanceSetupFunc);
            return new VerifiableMockInstaller(Mocks, invocationExpression);
        }

        public VerifiableMockInstaller Verify(Expression<Action> voidInstanceSetupFunc)
        {
            Guard.NotNull(voidInstanceSetupFunc, nameof(voidInstanceSetupFunc));
            var invocationExpression = new InvocationExpression(voidInstanceSetupFunc);
            return new VerifiableMockInstaller(Mocks, invocationExpression);
        }

        //---------------------------------------------------------------------------------------------------------

        protected Executor RewriteImpl(LambdaExpression expression)
        {
            var invocationExpression = new InvocationExpression(expression);
            var visitor = new GetTestMethodVisitor();
            invocationExpression.AcceptMemberVisitor(visitor);
            _fakeGenerator.Generate(Mocks, visitor.Method);
            return new Executor(_generatedObject, invocationExpression);
        }

        protected Executor<T> RewriteImpl<T>(LambdaExpression expression)
        {
            var invocationExpression = new InvocationExpression(expression);
            var visitor = new GetTestMethodVisitor();
            invocationExpression.AcceptMemberVisitor(visitor);
            _fakeGenerator.Generate(Mocks, visitor.Method);
            return new Executor<T>(_generatedObject, invocationExpression);
        }

        //---

        public Executor<TReturn> Rewrite<TReturn>(Expression<Func<TReturn>> rewriteFunc)
        {
            Guard.NotNull(rewriteFunc, nameof(rewriteFunc));
            return RewriteImpl<TReturn>(rewriteFunc);
        }

        public Executor<TReturn> Rewrite<TInput, TReturn>(Expression<Func<TInput, TReturn>> instanceRewriteFunc)
        {
            Guard.NotNull(instanceRewriteFunc, nameof(instanceRewriteFunc));
            return RewriteImpl<TReturn>(instanceRewriteFunc);
        }

        public Executor Rewrite<TInput>(Expression<Action<TInput>> voidInstanceRewriteFunc)
        {
            Guard.NotNull(voidInstanceRewriteFunc, nameof(voidInstanceRewriteFunc));
            return RewriteImpl(voidInstanceRewriteFunc);
        }

        public Executor Rewrite(Expression<Action> rewriteFunc)
        {
            Guard.NotNull(rewriteFunc, nameof(rewriteFunc));
            return RewriteImpl(rewriteFunc);
        }

        //---------------------------------------------------------------------------------------------------------

        protected T ExecuteImpl<T>(LambdaExpression expression)
        {
            var invocationExpression = new InvocationExpression(expression);
            var executor = new Executor<T>(_generatedObject, invocationExpression);
            return executor.Execute();
        }

        protected void ExecuteImpl(LambdaExpression expression)
        {
            var invocationExpression = new InvocationExpression(expression);
            var executor = new Executor(_generatedObject, invocationExpression);
            executor.Execute();
        }

        //---

        public TReturn Execute<TReturn>(Expression<Func<TReturn>> executeFunc)
        {
            Guard.NotNull(executeFunc, nameof(executeFunc));
            return ExecuteImpl<TReturn>(executeFunc);
        }

        public TReturn Execute<TInput, TReturn>(Expression<Func<TInput, TReturn>> instanceExecuteFunc)
        {
            Guard.NotNull(instanceExecuteFunc, nameof(instanceExecuteFunc));
            return ExecuteImpl<TReturn>(instanceExecuteFunc);
        }

        public void Execute<TInput>(Expression<Action<TInput>> voidInstanceExecuteFunc)
        {
            Guard.NotNull(voidInstanceExecuteFunc, nameof(voidInstanceExecuteFunc));
            ExecuteImpl(voidInstanceExecuteFunc);
        }

        public void Execute(Expression<Action> executeFunc)
        {
            Guard.NotNull(executeFunc, nameof(executeFunc));
            ExecuteImpl(executeFunc);
        }

        //---------------------------------------------------------------------------------------------------------

        public void Reset() => Mocks.Clear();
    }
}
