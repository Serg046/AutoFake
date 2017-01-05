using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AutoFake.Setup;

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
            return new ReplaceableMockInstaller<TReturn>(Mocks, ExpressionUtils.GetMethodInfo(instanceSetupFunc), GetSetupArguments(instanceSetupFunc.Body));
        }

        public ReplaceableMockInstaller Replace(Expression<Action<T>> voidInstanceSetupFunc)
        {
            Guard.NotNull(voidInstanceSetupFunc, nameof(voidInstanceSetupFunc));
            return new ReplaceableMockInstaller(Mocks, ExpressionUtils.GetMethodInfo(voidInstanceSetupFunc), GetSetupArguments(voidInstanceSetupFunc.Body));
        }

        //---------------------------------------------------------------------------------------------------------

        public VerifiableMockInstaller Verify<TReturn>(Expression<Func<T, TReturn>> instanceSetupFunc)
        {
            Guard.NotNull(instanceSetupFunc, nameof(instanceSetupFunc));
            return new VerifiableMockInstaller(Mocks, ExpressionUtils.GetMethodInfo(instanceSetupFunc), GetSetupArguments(instanceSetupFunc.Body));
        }

        public VerifiableMockInstaller Verify(Expression<Action<T>> voidInstanceSetupFunc)
        {
            Guard.NotNull(voidInstanceSetupFunc, nameof(voidInstanceSetupFunc));
            return new VerifiableMockInstaller(Mocks, ExpressionUtils.GetMethodInfo(voidInstanceSetupFunc), GetSetupArguments(voidInstanceSetupFunc.Body));
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

        internal List<FakeArgument> GetSetupArguments(Expression expression)
        {
            var result = new List<FakeArgument>();

            if (expression is UnaryExpression)
            {
                result = GetSetupArguments(((UnaryExpression)expression).Operand);
            }
            else if (expression is MethodCallExpression)
            {
                FillSetupArguments(expression, result);
            }

            return result;
        }

        private static void FillSetupArguments(Expression expression, List<FakeArgument> result)
        {
            using (var setupContext = new SetupContext())
            {
                foreach (var argument in ExpressionUtils.GetArguments((MethodCallExpression)expression))
                {
                    var argumentChecker = setupContext.IsCheckerSet
                        ? setupContext.PopChecker()
                        : new EqualityArgumentChecker(argument);
                    var fakeArgument = new FakeArgument(argumentChecker);
                    result.Add(fakeArgument);
                }
            }
        }

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
            return new ReplaceableMockInstaller<TReturn>(Mocks, ExpressionUtils.GetMethodInfo(setupFunc), GetSetupArguments(setupFunc.Body));
        }

        public ReplaceableMockInstaller<TReturn> Replace<TInput, TReturn>(Expression<Func<TInput, TReturn>> instanceSetupFunc)
        {
            Guard.NotNull(instanceSetupFunc, nameof(instanceSetupFunc));
            return new ReplaceableMockInstaller<TReturn>(Mocks, ExpressionUtils.GetMethodInfo(instanceSetupFunc), GetSetupArguments(instanceSetupFunc.Body));
        }

        public ReplaceableMockInstaller Replace<TInput>(Expression<Action<TInput>> voidInstanceSetupFunc)
        {
            Guard.NotNull(voidInstanceSetupFunc, nameof(voidInstanceSetupFunc));
            return new ReplaceableMockInstaller(Mocks, ExpressionUtils.GetMethodInfo(voidInstanceSetupFunc), GetSetupArguments(voidInstanceSetupFunc.Body));
        }

        public ReplaceableMockInstaller Replace(Expression<Action> voidInstanceSetupFunc)
        {
            Guard.NotNull(voidInstanceSetupFunc, nameof(voidInstanceSetupFunc));
            return new ReplaceableMockInstaller(Mocks, ExpressionUtils.GetMethodInfo(voidInstanceSetupFunc), GetSetupArguments(voidInstanceSetupFunc.Body));
        }

        //---------------------------------------------------------------------------------------------------------

        public VerifiableMockInstaller Verify<TReturn>(Expression<Func<TReturn>> setupFunc)
        {
            Guard.NotNull(setupFunc, nameof(setupFunc));
            return new VerifiableMockInstaller(Mocks, ExpressionUtils.GetMethodInfo(setupFunc), GetSetupArguments(setupFunc.Body));
        }

        public VerifiableMockInstaller Verify<TInput, TReturn>(Expression<Func<TInput, TReturn>> instanceSetupFunc)
        {
            Guard.NotNull(instanceSetupFunc, nameof(instanceSetupFunc));
            return new VerifiableMockInstaller(Mocks, ExpressionUtils.GetMethodInfo(instanceSetupFunc), GetSetupArguments(instanceSetupFunc.Body));
        }

        public VerifiableMockInstaller Verify<TInput>(Expression<Action<TInput>> voidInstanceSetupFunc)
        {
            Guard.NotNull(voidInstanceSetupFunc, nameof(voidInstanceSetupFunc));
            return new VerifiableMockInstaller(Mocks, ExpressionUtils.GetMethodInfo(voidInstanceSetupFunc), GetSetupArguments(voidInstanceSetupFunc.Body));
        }

        public VerifiableMockInstaller Verify(Expression<Action> voidInstanceSetupFunc)
        {
            Guard.NotNull(voidInstanceSetupFunc, nameof(voidInstanceSetupFunc));
            return new VerifiableMockInstaller(Mocks, ExpressionUtils.GetMethodInfo(voidInstanceSetupFunc), GetSetupArguments(voidInstanceSetupFunc.Body));
        }

        //---------------------------------------------------------------------------------------------------------

        protected Executor RewriteImpl(LambdaExpression expression)
        {
            _fakeGenerator.Generate(Mocks, ExpressionUtils.GetMethodInfo(expression));
            return new Executor(_generatedObject, expression);
        }

        protected Executor<T> RewriteImpl<T>(LambdaExpression expression)
        {
            _fakeGenerator.Generate(Mocks, ExpressionUtils.GetMethodInfo(expression));
            return new Executor<T>(_generatedObject, expression);
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
            var executor = new Executor<T>(_generatedObject, expression);
            return executor.Execute();
        }

        protected void ExecuteImpl(LambdaExpression expression)
        {
            var executor = new Executor(_generatedObject, expression);
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
