using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AutoFake.Setup;
using GuardExtensions;

namespace AutoFake
{
    public class Fake<T> : Fake
    {
        public Fake(params object[] contructorArgs) : base(typeof(T), contructorArgs)
        {
        }

        public TReturn Execute<TReturn>(Expression<Func<T, TReturn>> executeFunc)
        {
            Guard.NotNull(executeFunc);
            return ExecuteImpl<TReturn>(executeFunc);
        }

        public void Execute(Expression<Action<T>> executeFunc)
        {
            Guard.NotNull(executeFunc);
            ExecuteImpl(executeFunc);
        }

        public Executor<TReturn> Rewrite<TReturn>(Expression<Func<T, TReturn>> executeFunc)
        {
            Guard.NotNull(executeFunc);
            return RewriteImpl<TReturn>(executeFunc);
        }

        public Executor Rewrite(Expression<Action<T>> executeFunc)
        {
            Guard.NotNull(executeFunc);
            return RewriteImpl(executeFunc);
        }
    }

    public class Fake
    {
        private readonly FakeGenerator _fakeGenerator;
        private readonly GeneratedObject _generatedObject;

        public Fake(Type type, params object[] contructorArgs)
        {
            if (contructorArgs == null)
                contructorArgs = new object[] {null};

            var dependencies = contructorArgs.Select(c =>
            {
                var dependecy = c as FakeDependency;
                return dependecy ?? new FakeDependency(c?.GetType(), c);
            }).ToList();

            var typeInfo = new TypeInfo(type, dependencies);
            var mockerFactory = new MockerFactory();
            _generatedObject = new GeneratedObject(typeInfo);
            _fakeGenerator = new FakeGenerator(typeInfo, mockerFactory, _generatedObject);

            Setups = new SetupCollection();
        }

        internal SetupCollection Setups { get; }

        private List<FakeArgument> GetSetupArguments(Expression expression)
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
            Guard.NotNull(fileName);
            _fakeGenerator.Save(fileName);
        }

        //---------------------------------------------------------------------------------------------------------

        public ReplaceableMockInstaller<TReturn> Replace<TReturn>(Expression<Func<TReturn>> setupFunc)
        {
            Guard.NotNull(setupFunc);
            return new ReplaceableMockInstaller<TReturn>(Setups, ExpressionUtils.GetMethodInfo(setupFunc), GetSetupArguments(setupFunc.Body));
        }

        public ReplaceableMockInstaller<TReturn> Replace<TInput, TReturn>(Expression<Func<TInput, TReturn>> instanceSetupFunc)
        {
            Guard.NotNull(instanceSetupFunc);
            return new ReplaceableMockInstaller<TReturn>(Setups, ExpressionUtils.GetMethodInfo(instanceSetupFunc), GetSetupArguments(instanceSetupFunc.Body));
        }

        public ReplaceableMockInstaller Replace<TInput>(Expression<Action<TInput>> voidInstanceSetupFunc)
        {
            Guard.NotNull(voidInstanceSetupFunc);
            return new ReplaceableMockInstaller(Setups, ExpressionUtils.GetMethodInfo(voidInstanceSetupFunc), GetSetupArguments(voidInstanceSetupFunc.Body));
        }

        public ReplaceableMockInstaller Replace(Expression<Action> voidInstanceSetupFunc)
        {
            Guard.NotNull(voidInstanceSetupFunc);
            return new ReplaceableMockInstaller(Setups, ExpressionUtils.GetMethodInfo(voidInstanceSetupFunc), GetSetupArguments(voidInstanceSetupFunc.Body));
        }

        //---------------------------------------------------------------------------------------------------------

        public VerifiableMockInstaller Verify<TReturn>(Expression<Func<TReturn>> setupFunc)
        {
            Guard.NotNull(setupFunc);
            return new VerifiableMockInstaller(Setups, ExpressionUtils.GetMethodInfo(setupFunc), GetSetupArguments(setupFunc.Body), false);
        }

        public VerifiableMockInstaller Verify<TInput, TReturn>(Expression<Func<TInput, TReturn>> instanceSetupFunc)
        {
            Guard.NotNull(instanceSetupFunc);
            return new VerifiableMockInstaller(Setups, ExpressionUtils.GetMethodInfo(instanceSetupFunc), GetSetupArguments(instanceSetupFunc.Body), false);
        }

        public VerifiableMockInstaller Verify<TInput>(Expression<Action<TInput>> voidInstanceSetupFunc)
        {
            Guard.NotNull(voidInstanceSetupFunc);
            return new VerifiableMockInstaller(Setups, ExpressionUtils.GetMethodInfo(voidInstanceSetupFunc), GetSetupArguments(voidInstanceSetupFunc.Body), true);
        }

        public VerifiableMockInstaller Verify(Expression<Action> voidInstanceSetupFunc)
        {
            Guard.NotNull(voidInstanceSetupFunc);
            return new VerifiableMockInstaller(Setups, ExpressionUtils.GetMethodInfo(voidInstanceSetupFunc), GetSetupArguments(voidInstanceSetupFunc.Body), true);
        }

        //---------------------------------------------------------------------------------------------------------

        protected Executor RewriteImpl(LambdaExpression expression)
        {
            _fakeGenerator.Generate(Setups, ExpressionUtils.GetMethodInfo(expression));
            return new Executor(_generatedObject, expression);
        }

        protected Executor<T> RewriteImpl<T>(LambdaExpression expression)
        {
            _fakeGenerator.Generate(Setups, ExpressionUtils.GetMethodInfo(expression));
            return new Executor<T>(_generatedObject, expression);
        }

        public Executor<TReturn> Rewrite<TReturn>(Expression<Func<TReturn>> executeFunc)
        {
            Guard.NotNull(executeFunc);
            return RewriteImpl<TReturn>(executeFunc);
        }

        public Executor Rewrite(Expression<Action> executeFunc)
        {
            Guard.NotNull(executeFunc);
            return RewriteImpl(executeFunc);
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

        public TReturn Execute<TReturn>(Expression<Func<TReturn>> executeFunc)
        {
            Guard.NotNull(executeFunc);
            return ExecuteImpl<TReturn>(executeFunc);
        }

        public void Execute(Expression<Action> executeFunc)
        {
            Guard.NotNull(executeFunc);
            ExecuteImpl(executeFunc);
        }

        //---------------------------------------------------------------------------------------------------------

        public void Reset() => Setups.Clear();
    }
}
