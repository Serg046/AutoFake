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

        public FuncMockConfiguration<T, TReturn> Rewrite<TReturn>(Expression<Func<T, TReturn>> expression) => base.Rewrite(expression);
        
        public ActionMockConfiguration<T> Rewrite(Expression<Action<T>> expression) => base.Rewrite(expression);
        
        public void Execute(Action<T> action) => Execute(action, gen => gen.Instance);
        
        public void Execute(Action<T, IList<object>> action) => Execute(action, gen => new[] { gen.Instance, gen.Parameters });
        
        public Task ExecuteAsync(Func<T, Task> action) => (Task)Execute(action, gen => gen.Instance);
        
        public Task ExecuteAsync(Func<T, IList<object>, Task> action) => (Task)Execute(action, gen => new[] { gen.Instance, gen.Parameters});
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

        public MockConfiguration Rewrite(Expression<Action> expression)
        {
            var invocationExpression = new InvocationExpression(expression ?? throw new ArgumentNullException(nameof(expression)));
            var mocks = GetMocksContainer(invocationExpression);
            return new ActionMockConfiguration(mocks, new ProcessorFactory(TypeInfo),
                new Executor(this, invocationExpression));
        }

        public void Execute(Action action) => ExecuteWithoutParameters(action);

        public void Execute(Action<TypeWrapper> action) => Execute(action, gen => new TypeWrapper(gen));

        public void Execute(Action<TypeWrapper, IList<object>> action) => Execute(action, gen => new object[] { new TypeWrapper(gen), gen.Parameters });

        public Task ExecuteAsync(Func<Task> action) => (Task)ExecuteWithoutParameters(action);
        
        public Task ExecuteAsync(Func<TypeWrapper, Task> action) => (Task)Execute(action, gen => new TypeWrapper(gen));

        public Task ExecuteAsync(Func<TypeWrapper, IList<object>, Task> action)
            => (Task)Execute(action, gen => new object[] {new TypeWrapper(gen), gen.Parameters});

        internal object Execute(Delegate action, Func<FakeObjectInfo, object> fake) => Execute(action, gen => new[] { fake(gen) });

        internal object ExecuteWithoutParameters(Delegate action)
        {
            var fields = action.GetCapturedMembers(TypeInfo.Module);
            var fakeObject = TypeInfo.CreateFakeObject(Mocks);
            return Execute(fakeObject.Type, action, new object[0], fields);
        }

        internal object Execute(Delegate action, Func<FakeObjectInfo, object[]> fake)
        {
            var fields = action.GetCapturedMembers(TypeInfo.Module);
            var fakeObject = TypeInfo.CreateFakeObject(Mocks);
            return Execute(fakeObject.Type, action, fake(fakeObject), fields);
        }

        private object Execute(Type type, Delegate action, object[] parameters, IEnumerable<CapturedMember> fields)
        {
            var delegateType = type.Assembly.GetType(action.Method.DeclaringType.FullName, true);
            var generatedMethod = delegateType.GetMethod(action.Method.Name, BindingFlags.Instance | BindingFlags.NonPublic);
            var instance = Activator.CreateInstance(delegateType);

            foreach (var fieldDef in fields)
            {
                var field = delegateType.GetField(fieldDef.Field.Name);
                field.SetValue(instance, fieldDef.Instance);
            }

            try
            {
                return generatedMethod.Invoke(instance, parameters);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }

        internal FakeObjectInfo CreateFakeObject() => TypeInfo.CreateFakeObject(Mocks);

        private IList<IMock> GetMocksContainer(InvocationExpression invocationExpression)
        {
            var visitor = new GetTestMethodVisitor();
            invocationExpression.AcceptMemberVisitor(visitor);
            var mocks = new List<IMock>();
            Mocks.Add(visitor.Method, mocks);
            return mocks;
        }
    }
}
