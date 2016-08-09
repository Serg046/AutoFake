using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace AutoFake
{
    public class Fake
    {
        public static Fake<T> For<T>(params object[] contructorArgs) => new Fake<T>(contructorArgs);
    }

    public class Fake<T>
    {
        private readonly object[] _contructorArgs;
        private readonly FakeGenerator<T> _fakeGenerator;
        private string _assemblyFileName;

        internal Fake(object[] contructorArgs)
        {
            _contructorArgs = contructorArgs;
            _fakeGenerator = new FakeGenerator<T>(contructorArgs);

            Setups = new List<FakeSetupPack>();
        }

        internal List<FakeSetupPack> Setups { get; }

        public FakeSetup<T, TReturn> Setup<TReturn>(Expression<Func<TReturn>> setupFunc)
        {
            return new FakeSetup<T, TReturn>(this, ExpressionUtils.GetMethodInfo(setupFunc));
        }

        public FakeSetup<T, TReturn> Setup<TInput, TReturn>(Expression<Func<TInput, TReturn>> setupFunc)
        {
            return new FakeSetup<T, TReturn>(this, ExpressionUtils.GetMethodInfo(setupFunc));
        }

        public void SaveFakeAssembly(string fileName) => _assemblyFileName = fileName;

        public TReturn Execute<TReturn>(Expression<Func<T, TReturn>> executeFunc)
        {
            var instance = _fakeGenerator.Generate(Setups);
            if (_assemblyFileName != null)
                _fakeGenerator.Save(_assemblyFileName);

            var counter = 0;
            var generatedType = instance.GetType();
            foreach (var setup in Setups)
            {
                var fieldName = _fakeGenerator.GetFieldName(setup, ++counter);
                var field = generatedType.GetField(fieldName);
                field.SetValue(instance, setup.ReturnObject);
            }

            return (TReturn)GetInvocationResult(executeFunc.Body, instance, generatedType);
        }

        private object GetInvocationResult(Expression executeFunc, object instance, Type generatedType)
        {
            object result;
            if (executeFunc is MethodCallExpression)
            {
                var methodCallExpression = (MethodCallExpression)executeFunc;
                var methodName = methodCallExpression.Method;
                var arguments = methodCallExpression.Arguments.Select(GetArgument).ToArray();
                var method = generatedType.GetMethod(methodCallExpression.Method.Name,
                    methodCallExpression.Method.GetParameters().Select(p => p.ParameterType).ToArray());
                result = method.Invoke(instance, arguments);
            }
            else if (executeFunc is MemberExpression)
            {
                var propInfo = ((MemberExpression)executeFunc).Member as PropertyInfo;
                var property = generatedType.GetProperty(propInfo.Name);
                result = property.GetValue(instance, null);
            }
            else if (executeFunc is UnaryExpression)
            {
                result = GetInvocationResult(((UnaryExpression)executeFunc).Operand, instance, generatedType);
            }
            else
                throw new InvalidOperationException($"Ivalid expression format. Source: {executeFunc.ToString()}.");
            return result;
        }

        //see http://stackoverflow.com/questions/36861196/how-to-serialize-method-call-expression-with-arguments/36862531
        private object GetArgument(Expression expr)
        {
            switch (expr.NodeType)
            {
                case ExpressionType.Constant:
                    return ((ConstantExpression)expr).Value;
                case ExpressionType.MemberAccess:
                    var me = (MemberExpression)expr;
                    object target = GetArgument(me.Expression);
                    switch (me.Member.MemberType)
                    {
                        case MemberTypes.Field:
                            return ((FieldInfo)me.Member).GetValue(target);
                        case MemberTypes.Property:
                            return ((PropertyInfo)me.Member).GetValue(target, null);
                        default:
                            throw new NotSupportedException(me.Member.MemberType.ToString());
                    }
                case ExpressionType.New:
                    return ((NewExpression)expr).Constructor
                        .Invoke(((NewExpression)expr).Arguments.Select(GetArgument).ToArray());
                default:
                    throw new NotSupportedException(expr.NodeType.ToString());
            }
        }
    }
}
