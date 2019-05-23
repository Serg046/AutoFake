using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
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
            => ReplaceImpl<TReturn>(instanceSetupFunc);

        public ReplaceableMockInstaller Replace(Expression<Action<T>> voidInstanceSetupFunc)
            => ReplaceImpl(voidInstanceSetupFunc);

        //---------------------------------------------------------------------------------------------------------

        public VerifiableMockInstaller Verify<TReturn>(Expression<Func<T, TReturn>> instanceSetupFunc)
            => VerifyImpl(instanceSetupFunc);

        public VerifiableMockInstaller Verify(Expression<Action<T>> voidInstanceSetupFunc)
            => VerifyImpl(voidInstanceSetupFunc);

        //---------------------------------------------------------------------------------------------------------

        public Executor<TReturn> Rewrite<TReturn>(Expression<Func<T, TReturn>> instanceRewriteFunc)
            => RewriteImpl<TReturn>(instanceRewriteFunc);

        public Executor Rewrite(Expression<Action<T>> voidInstanceRewriteFunc)
            => RewriteImpl(voidInstanceRewriteFunc);

        //---------------------------------------------------------------------------------------------------------

        public TReturn Execute<TReturn>(Expression<Func<T, TReturn>> instanceExecuteFunc)
            => ExecuteImpl<TReturn>(instanceExecuteFunc);

        public void Execute(Expression<Action<T>> voidInstanceExecuteFunc)
            => ExecuteImpl(voidInstanceExecuteFunc);

        //---------------------------------------------------------------------------------------------------------

        public void SetValue<TReturn>(Expression<Func<T, TReturn>> instanceSetupFunc, TReturn value)
            => SetValueImpl(instanceSetupFunc, value);

        public void Execute2(Action<T> action)
        {
            if (!_generatedObject.IsBuilt) _generatedObject.Build();
            

            var delegateType = _generatedObject.Assembly.GetType(action.Method.DeclaringType.FullName, true);
            var method = delegateType.GetMethod(action.Method.Name, BindingFlags.Instance | BindingFlags.NonPublic);
            var instance = Activator.CreateInstance(delegateType);
            method.Invoke(instance, new[] {_generatedObject.Instance});

            foreach (var mockedMemberInfo in _generatedObject.MockedMembers)
            {
                mockedMemberInfo.Mock.Verify(mockedMemberInfo, _generatedObject);
            }
        }

        public Task Execute2Async(Func<T, Task> action)
        {
            if (!_generatedObject.IsBuilt) _generatedObject.Build();
            foreach (var mockedMemberInfo in _generatedObject.MockedMembers)
            {
                mockedMemberInfo.Mock.Verify(mockedMemberInfo, _generatedObject);
            }

            var delegateType = _generatedObject.Assembly.GetType(action.Method.DeclaringType.FullName, true);
            var method = delegateType.GetMethod(action.Method.Name, BindingFlags.Instance | BindingFlags.NonPublic);
            var instance = Activator.CreateInstance(delegateType);
            return (Task)method.Invoke(instance, new[] { _generatedObject.Instance });
        }

        public void Execute2(Action<T, IList<object>> action)
        {
            if (!_generatedObject.IsBuilt) _generatedObject.Build();
            foreach (var mockedMemberInfo in _generatedObject.MockedMembers)
            {
                mockedMemberInfo.Mock.Verify(mockedMemberInfo, _generatedObject);
            }

            var delegateType = _generatedObject.Assembly.GetType(action.Method.DeclaringType.FullName, true);
            var method = delegateType.GetMethod(action.Method.Name, BindingFlags.Instance | BindingFlags.NonPublic);
            var instance = Activator.CreateInstance(delegateType);

            method.Invoke(instance, new[] { _generatedObject.Instance, _generatedObject.Parameters });
        }
    }

    public class Fake
    {
        private readonly FakeGenerator _fakeGenerator;
        internal readonly GeneratedObject _generatedObject;

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

        protected ReplaceableMockInstaller<TReturn> ReplaceImpl<TReturn>(LambdaExpression expression)
        {
            Guard.NotNull(expression, nameof(expression));
            var invocationExpression = new InvocationExpression(expression);
            return new ReplaceableMockInstaller<TReturn>(Mocks, invocationExpression);
        }

        protected ReplaceableMockInstaller ReplaceImpl(LambdaExpression expression)
        {
            Guard.NotNull(expression, nameof(expression));
            var invocationExpression = new InvocationExpression(expression);
            return new ReplaceableMockInstaller(Mocks, invocationExpression);
        }

        public ReplaceableMockInstaller<TReturn> Replace<TInput, TReturn>(Expression<Func<TInput, TReturn>> instanceSetupFunc)
            => ReplaceImpl<TReturn>(instanceSetupFunc);

        public ReplaceableMockInstaller Replace<TInput>(Expression<Action<TInput>> voidInstanceSetupFunc)
            => ReplaceImpl(voidInstanceSetupFunc);

        public ReplaceableMockInstaller<TReturn> Replace<TReturn>(Expression<Func<TReturn>> staticSetupFunc)
            => ReplaceImpl<TReturn>(staticSetupFunc);

        public ReplaceableMockInstaller Replace(Expression<Action> voidStaticSetupFunc)
            => ReplaceImpl(voidStaticSetupFunc);

        //---------------------------------------------------------------------------------------------------------

        protected VerifiableMockInstaller VerifyImpl(LambdaExpression expression)
        {
            Guard.NotNull(expression, nameof(expression));
            var invocationExpression = new InvocationExpression(expression);
            return new VerifiableMockInstaller(Mocks, invocationExpression);
        }

        public VerifiableMockInstaller Verify<TInput, TReturn>(Expression<Func<TInput, TReturn>> instanceSetupFunc)
            => VerifyImpl(instanceSetupFunc);

        public VerifiableMockInstaller Verify<TInput>(Expression<Action<TInput>> voidInstanceSetupFunc)
            => VerifyImpl(voidInstanceSetupFunc);

        public VerifiableMockInstaller Verify<TReturn>(Expression<Func<TReturn>> staticSetupFunc)
            => VerifyImpl(staticSetupFunc);

        public VerifiableMockInstaller Verify(Expression<Action> voidStaticSetupFunc)
            => VerifyImpl(voidStaticSetupFunc);

        //---------------------------------------------------------------------------------------------------------

        protected Executor RewriteImpl(LambdaExpression expression)
        {
            Guard.NotNull(expression, nameof(expression));

            var invocationExpression = new InvocationExpression(expression);
            var visitor = new GetTestMethodVisitor();
            invocationExpression.AcceptMemberVisitor(visitor);
            _fakeGenerator.Generate(Mocks, visitor.Method);
            return new Executor(_generatedObject, invocationExpression);
        }

        protected Executor<T> RewriteImpl<T>(LambdaExpression expression)
        {
            Guard.NotNull(expression, nameof(expression));

            var invocationExpression = new InvocationExpression(expression);
            var visitor = new GetTestMethodVisitor();
            invocationExpression.AcceptMemberVisitor(visitor);
            _fakeGenerator.Generate(Mocks, visitor.Method);
            return new Executor<T>(_generatedObject, invocationExpression);
        }

        public Executor<TReturn> Rewrite<TInput, TReturn>(Expression<Func<TInput, TReturn>> instanceRewriteFunc)
            => RewriteImpl<TReturn>(instanceRewriteFunc);

        public Executor Rewrite<TInput>(Expression<Action<TInput>> voidInstanceRewriteFunc)
            => RewriteImpl(voidInstanceRewriteFunc);

        public Executor<TReturn> Rewrite<TReturn>(Expression<Func<TReturn>> staticRewriteFunc)
            => RewriteImpl<TReturn>(staticRewriteFunc);

        public Executor Rewrite(Expression<Action> voidStaticRewriteFunc)
            => RewriteImpl(voidStaticRewriteFunc);

        //---------------------------------------------------------------------------------------------------------

        protected T ExecuteImpl<T>(LambdaExpression expression)
        {
            Guard.NotNull(expression, nameof(expression));

            var invocationExpression = new InvocationExpression(expression);
            var executor = new Executor<T>(_generatedObject, invocationExpression);
            return executor.Execute();
        }

        protected void ExecuteImpl(LambdaExpression expression)
        {
            Guard.NotNull(expression, nameof(expression));

            var invocationExpression = new InvocationExpression(expression);
            var executor = new Executor(_generatedObject, invocationExpression);
            executor.Execute();
        }

        public TReturn Execute<TInput, TReturn>(Expression<Func<TInput, TReturn>> instanceExecuteFunc)
            => ExecuteImpl<TReturn>(instanceExecuteFunc);

        public void Execute<TInput>(Expression<Action<TInput>> voidInstanceExecuteFunc)
            => ExecuteImpl(voidInstanceExecuteFunc);

        public TReturn Execute<TReturn>(Expression<Func<TReturn>> staticExecuteFunc)
            => ExecuteImpl<TReturn>(staticExecuteFunc);

        public void Execute(Expression<Action> voidStaticExecuteFunc)
            => ExecuteImpl(voidStaticExecuteFunc);

        public void Execute()
        {
            if (_generatedObject.IsBuilt)
                throw new InvalidOperationException("Cannot execute contructor because the instance is already built.");

            _generatedObject.Build();
        }

        //---------------------------------------------------------------------------------------------------------

        protected void SetValueImpl<TReturn>(LambdaExpression expression, TReturn value)
        {
            Guard.NotNull(expression, nameof(expression));

            if (!_generatedObject.IsBuilt)
                throw new InvalidOperationException($"Cannot set the value. Instance is not built yet. Please run {nameof(Fake)}::{nameof(Execute)}() method.");

            var invocationExpression = new InvocationExpression(expression);
            var visitor = new SetValueMemberVisitor(_generatedObject, value);
            invocationExpression.AcceptMemberVisitor(new TargetMemberVisitor(visitor, _generatedObject.Type));
        }

        public void SetValue<TInput, TReturn>(Expression<Func<TInput, TReturn>> instanceSetupFunc, TReturn value)
            => SetValueImpl(instanceSetupFunc, value);

        public void SetValue<TReturn>(Expression<Func<TReturn>> staticSetupFunc, TReturn value)
            => SetValueImpl(staticSetupFunc, value);

        //---------------------------------------------------------------------------------------------------------

        public void Reset() => Mocks.Clear();

        public void Execute2(Action<TypeWrapper> action)
        {
            if (!_generatedObject.IsBuilt) _generatedObject.Build();
            foreach (var mockedMemberInfo in _generatedObject.MockedMembers)
            {
                mockedMemberInfo.Mock.Verify(mockedMemberInfo, _generatedObject);
            }

            var delegateType = _generatedObject.Assembly.GetType(action.Method.DeclaringType.FullName, true);
            var method = delegateType.GetMethod(action.Method.Name, BindingFlags.Instance | BindingFlags.NonPublic);
            var instance = Activator.CreateInstance(delegateType);
            method.Invoke(instance, new object[] { new TypeWrapper(_generatedObject) });
        }

        public void Execute2(Action<TypeWrapper, IList<object>> action)
        {
            if (!_generatedObject.IsBuilt) _generatedObject.Build();
            foreach (var mockedMemberInfo in _generatedObject.MockedMembers)
            {
                mockedMemberInfo.Mock.Verify(mockedMemberInfo, _generatedObject);
            }

            var delegateType = _generatedObject.Assembly.GetType(action.Method.DeclaringType.FullName, true);
            var method = delegateType.GetMethod(action.Method.Name, BindingFlags.Instance | BindingFlags.NonPublic);
            var instance = Activator.CreateInstance(delegateType);

            method.Invoke(instance, new object[] { new TypeWrapper(_generatedObject), _generatedObject.Parameters });
        }

        public Task Execute2Async(Func<TypeWrapper, Task> action)
        {
            if (!_generatedObject.IsBuilt) _generatedObject.Build();
            foreach (var mockedMemberInfo in _generatedObject.MockedMembers)
            {
                mockedMemberInfo.Mock.Verify(mockedMemberInfo, _generatedObject);
            }

            var delegateType = _generatedObject.Assembly.GetType(action.Method.DeclaringType.FullName, true);
            var method = delegateType.GetMethod(action.Method.Name, BindingFlags.Instance | BindingFlags.NonPublic);
            var instance = Activator.CreateInstance(delegateType);
            return (Task)method.Invoke(instance, new[] { new TypeWrapper(_generatedObject) });
        }
    }
}
