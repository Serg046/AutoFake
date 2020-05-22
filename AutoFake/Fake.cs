using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
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

        private MockConfiguration<T> RewriteImpl(LambdaExpression expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            var invocationExpression = new InvocationExpression(expression);
            var visitor = new GetTestMethodVisitor();
            invocationExpression.AcceptMemberVisitor(visitor);
            var mocks = new List<IMock>();
            Mocks.Add(visitor.Method, mocks);
            return new MockConfiguration<T>(mocks, new ProcessorFactory(TypeInfo));
        }

        public MockConfiguration<T> Rewrite<TReturn>(Expression<Func<T, TReturn>> instanceRewriteFunc) => RewriteImpl(instanceRewriteFunc);

        public MockConfiguration<T> Rewrite(Expression<Action<T>> action) => RewriteImpl(action);

        public void Execute(Action<T> action) => Execute(action.Method, gen => gen.Instance);

        public void Execute(Action<T, IList<object>> action) => Execute(action.Method, gen => new[] { gen.Instance, gen.Parameters });

        public Task ExecuteAsync(Func<T, Task> action) => (Task)Execute(action.Method, gen => gen.Instance);

        public Task ExecuteAsync(Func<T, IList<object>, Task> action) => (Task)Execute(action.Method, gen => new[] { gen.Instance, gen.Parameters});
    }

    public class Fake
    {
        public Fake(Type type, params object[] constructorArgs)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (constructorArgs == null) throw new ArgumentNullException(nameof(constructorArgs));

            var dependencies = constructorArgs.Select(c => c as FakeDependency ?? new FakeDependency(c?.GetType(), c)).ToList();
            TypeInfo = new TypeInfo(type, dependencies);
            Mocks = new MockCollection();
        }

        internal ITypeInfo TypeInfo { get; }

        internal MockCollection Mocks { get; }

        public void SaveFakeAssembly(string fileName)
        {
            using (var fileStream = File.Create(fileName))
            {
                TypeInfo.WriteAssembly(fileStream);
            }
        }

        private MockConfiguration RewriteImpl(LambdaExpression expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            var invocationExpression = new InvocationExpression(expression);
            var visitor = new GetTestMethodVisitor();
            invocationExpression.AcceptMemberVisitor(visitor);
            var mocks = new List<IMock>();
            Mocks.Add(visitor.Method, mocks);
            return new MockConfiguration(mocks, new ProcessorFactory(TypeInfo));
        }

        public MockConfiguration Rewrite<TInput, TReturn>(Expression<Func<TInput, TReturn>> instanceRewriteFunc) => RewriteImpl(instanceRewriteFunc);

        public MockConfiguration Rewrite<TInput>(Expression<Action<TInput>> voidInstanceRewriteFunc) => RewriteImpl(voidInstanceRewriteFunc);

        public MockConfiguration Rewrite<TReturn>(Expression<Func<TReturn>> staticRewriteFunc) => RewriteImpl(staticRewriteFunc);

        public MockConfiguration Rewrite(Expression<Action> voidStaticRewriteFunc) => RewriteImpl(voidStaticRewriteFunc);

        public void Execute(Action<TypeWrapper> action) => Execute(action.Method, gen => new TypeWrapper(gen));

        public void Execute(Action<TypeWrapper, IList<object>> action) => Execute(action.Method, gen => new object[] { new TypeWrapper(gen), gen.Parameters });

        public Task ExecuteAsync(Func<TypeWrapper, Task> action) => (Task)Execute(action.Method, gen => new TypeWrapper(gen));

        public Task ExecuteAsync(Func<TypeWrapper, IList<object>, Task> action)
            => (Task)Execute(action.Method, gen => new object[] {new TypeWrapper(gen), gen.Parameters});

        internal object Execute(MethodInfo method, Func<FakeObjectInfo, object> fake) => Execute(method, gen => new[] { fake(gen) });

        internal object Execute(MethodInfo method, Func<FakeObjectInfo, object[]> fake)
        {
            var fakeObject = TypeInfo.CreateFakeObject(Mocks);

            var delegateType = fakeObject.Type.Assembly.GetType(method.DeclaringType.FullName, true);
            var generatedMethod = delegateType.GetMethod(method.Name, BindingFlags.Instance | BindingFlags.NonPublic);
            var instance = Activator.CreateInstance(delegateType);

            try
            {
                return generatedMethod.Invoke(instance, fake(fakeObject));
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }
    }
}
