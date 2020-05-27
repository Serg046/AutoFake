using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using AutoFake.Exceptions;
using AutoFake.Expression;
using AutoFake.Setup;
using AutoFake.Setup.Configurations;
using AutoFake.Setup.Mocks;
using Mono.Cecil;
using Mono.Cecil.Cil;
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

        public void Execute(Action action) => ExecuteWithoutParameters(action.Method);

        public void Execute(Action<TypeWrapper> action) => Execute(action.Method, gen => new TypeWrapper(gen));

        public void Execute(Action<TypeWrapper, IList<object>> action) => Execute(action.Method, gen => new object[] { new TypeWrapper(gen), gen.Parameters });

        public Task ExecuteAsync(Func<Task> action) => (Task)ExecuteWithoutParameters(action.Method);
        
        public Task ExecuteAsync(Func<TypeWrapper, Task> action) => (Task)Execute(action.Method, gen => new TypeWrapper(gen));

        public Task ExecuteAsync(Func<TypeWrapper, IList<object>, Task> action)
            => (Task)Execute(action.Method, gen => new object[] {new TypeWrapper(gen), gen.Parameters});

        internal object Execute(MethodInfo method, Func<FakeObjectInfo, object> fake) => Execute(method, gen => new[] { fake(gen) });

        internal object ExecuteWithoutParameters(MethodInfo method)
        {
            // Should be materialized before .CreateFakeObject(...) call
            var fields = GetDelegateFields(method).ToList();
            var fakeObject = TypeInfo.CreateFakeObject(Mocks);
            return Execute(fakeObject.Type, method, new object[0], fields);
        }

        internal object Execute(MethodInfo method, Func<FakeObjectInfo, object[]> fake)
        {
            // Should be materialized before .CreateFakeObject(...) call
            var fields = GetDelegateFields(method).ToList();
            var fakeObject = TypeInfo.CreateFakeObject(Mocks);
            return Execute(fakeObject.Type, method, fake(fakeObject), fields);
        }

        private object Execute(Type type, MethodInfo method, object[] parameters, IEnumerable<DelegateField> fields)
        {
            var delegateType = type.Assembly.GetType(method.DeclaringType.FullName, true);
            var generatedMethod = delegateType.GetMethod(method.Name, BindingFlags.Instance | BindingFlags.NonPublic);
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

        private IEnumerable<DelegateField> GetDelegateFields(MethodInfo method)
        {
            var delegateType = TypeInfo.Module.GetType(method.DeclaringType.FullName, true).Resolve();
            var delegateRef = delegateType.Methods.Single(m => m.Name == method.Name);
            var fieldGroups = delegateRef.Body.Instructions
                .Where(c => c.OpCode == OpCodes.Ldfld || c.OpCode == OpCodes.Ldflda)
                .Select(c => (FieldDefinition)c.Operand)
                .Distinct()
                .Where(field => field.DeclaringType == delegateType &&
                    !delegateRef.Body.Instructions.Any(c => c.OpCode == OpCodes.Stfld && c.Operand == field))
                .GroupBy(f => f.FieldType);

            foreach (var fieldGroup in fieldGroups)
            {
                var fields = fieldGroup.ToList();
                if (fields.Count > 1) throw new InitializationException("Multiple captured members with the same type are not supported. Use Replace(() => new SomeType()) instead of Replace(someInstance).");

                var field = fields[0];
                var replaceMocks = Mocks.SelectMany(m => m.Mocks)
                    .OfType<ReplaceMock>()
                    .Where(m => m.ReturnObject?.Instance != null && m.ReturnObject.Instance
                        .GetType().FullName == AutoFake.TypeInfo.GetClrName(field.FieldType.FullName))
                    .ToList();
                if (replaceMocks.Count == 0) throw new InitializationException("There is no any return instances configured. Use Replace(someInstance) instead of Replace(() => new SomeType()).");
                if (replaceMocks.Count > 1) throw new InitializationException($"There are more than one return instance configured with the type {field.FieldType}");

                var captured = replaceMocks[0];
                captured.ProcessInstruction(Instruction.Create(OpCodes.Ldfld, field));
                yield return new DelegateField(field, captured.ReturnObject.Instance);
            }
        }

        private class DelegateField
        {
            public DelegateField(FieldDefinition field, object instance)
            {
                Field = field;
                Instance = instance;
            }
            public FieldDefinition Field { get; }
            public object Instance { get; }
        }
    }
}
