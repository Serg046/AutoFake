using System;
using System.Collections.Generic;
using System.IO;
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

        public VerifiableMockInstaller Verify<TReturn>(Expression<Func<T, TReturn>> instanceSetupFunc)
            => VerifyImpl(instanceSetupFunc);

        public VerifiableMockInstaller Verify(Expression<Action<T>> voidInstanceSetupFunc)
            => VerifyImpl(voidInstanceSetupFunc);

        public void Rewrite<TReturn>(Expression<Func<T, TReturn>> instanceRewriteFunc) => RewriteImpl(instanceRewriteFunc);

        public void Rewrite(Expression<Action<T>> action) => RewriteImpl(action);

        public void Execute(Action<T> action) => Execute(action.Method, gen => gen.Instance);

        public void Execute(Action<T, IList<object>> action) => Execute(action.Method, gen => new[] { gen.Instance, gen.Parameters });

        public Task ExecuteAsync(Func<T, Task> action) => (Task)Execute(action.Method, gen => gen.Instance);

        public Task ExecuteAsync(Func<T, IList<object>, Task> action) => (Task)Execute(action.Method, gen => new[] { gen.Instance, gen.Parameters});

    }

    public class Fake
    {
        private readonly FakeGenerator _fakeGenerator;
        private readonly GeneratedObject _generatedObject;
        private readonly TypeInfo _typeInfo;

        public Fake(Type type, params object[] contructorArgs)
        {
            Guard.NotNull(type, nameof(type));
            Guard.NotNull(contructorArgs, nameof(contructorArgs));

            var dependencies = contructorArgs.Select(c =>
            {
                var dependecy = c as FakeDependency;
                return dependecy ?? new FakeDependency(c?.GetType(), c);
            }).ToList();

            _typeInfo = new TypeInfo(type, dependencies);
            var mockerFactory = new MockerFactory();
            _generatedObject = new GeneratedObject(_typeInfo);
            _fakeGenerator = new FakeGenerator(_typeInfo, mockerFactory, _generatedObject);

            Mocks = new List<IMock>();
        }

        internal ICollection<IMock> Mocks { get; }

        public void SaveFakeAssembly(string fileName)
        {
            using (var fileStream = File.Create(fileName))
            {
                _typeInfo.WriteAssembly(fileStream);
            }
        }

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

        protected void RewriteImpl(LambdaExpression expression)
        {
            Guard.NotNull(expression, nameof(expression));

            var invocationExpression = new InvocationExpression(expression);
            var visitor = new GetTestMethodVisitor();
            invocationExpression.AcceptMemberVisitor(visitor);
            _fakeGenerator.Generate(Mocks, visitor.Method);
        }

        public void Rewrite<TInput, TReturn>(Expression<Func<TInput, TReturn>> instanceRewriteFunc) => RewriteImpl(instanceRewriteFunc);

        public void Rewrite<TInput>(Expression<Action<TInput>> voidInstanceRewriteFunc) => RewriteImpl(voidInstanceRewriteFunc);

        public void Rewrite<TReturn>(Expression<Func<TReturn>> staticRewriteFunc) => RewriteImpl(staticRewriteFunc);

        public void Rewrite(Expression<Action> voidStaticRewriteFunc) => RewriteImpl(voidStaticRewriteFunc);

        public void Reset() => Mocks.Clear();

        public void Execute(Action<TypeWrapper> action) => Execute(action.Method, gen => new TypeWrapper(gen));

        public void Execute(Action<TypeWrapper, IList<object>> action) => Execute(action.Method, gen => new object[] { new TypeWrapper(gen), gen.Parameters });

        public Task ExecuteAsync(Func<TypeWrapper, Task> action) => (Task)Execute(action.Method, gen => new TypeWrapper(gen));

        public Task ExecuteAsync(Func<TypeWrapper, IList<object>, Task> action)
            => (Task)Execute(action.Method, gen => new object[] {new TypeWrapper(gen), gen.Parameters});

        internal object Execute(MethodInfo method, Func<GeneratedObject, object> fake) => Execute(method, gen => new[] { fake(gen) });

        internal object Execute(MethodInfo method, Func<GeneratedObject, object[]> fake)
        {
            if (!_generatedObject.IsBuilt) _generatedObject.Build();

            var delegateType = _generatedObject.Assembly.GetType(method.DeclaringType.FullName, true);
            var generatedMethod = delegateType.GetMethod(method.Name, BindingFlags.Instance | BindingFlags.NonPublic);
            var instance = Activator.CreateInstance(delegateType);

            try
            {
                return generatedMethod.Invoke(instance, fake(_generatedObject));
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException != null) throw ex.InnerException;
                throw;
            }
        }
    }
}
