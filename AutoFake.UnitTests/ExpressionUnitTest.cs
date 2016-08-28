using System;
using System.Linq.Expressions;
using System.Reflection;

namespace AutoFake.UnitTests
{
    public class ExpressionUnitTest
    {
        protected MethodInfo GetMethod(string name)
            => GetMethod(name, new Type[0]);

        protected MethodInfo GetMethod(string name, Type[] types)
            => GetMethod<ExpressionUnitTest>(name, types);

        protected MethodInfo GetMethod<T>(string name)
            => GetMethod<T>(name, new Type[0]);

        protected MethodInfo GetMethod<T>(string name, Type[] types)
            => typeof(T).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic, null, types, null);

        protected PropertyInfo GetProperty(string name)
            => typeof(ExpressionUnitTest).GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic);

        protected int SomeProperty { get; } = 0;

        protected void SomeMethod()
        {
        }

        protected object SomeMethod(object a) => a;

        protected void SomeMethod(int a, string b, string c, Type d)
        {
        }

        protected void SomeMethod(int a, int[] args1, params object[] args2)
        {
        }

        protected MethodCallExpression GetMethodCallExpression(Expression<Action> expression)
            => (MethodCallExpression)expression.Body;

        protected MethodCallExpression GetMethodCallExpression<T>(Expression<Action<T>> expression)
            => (MethodCallExpression)expression.Body;

        protected class SomeType
        {
            public void SomeMethod()
            {
            }

            public void SomeMethod(SomeType self)
            {
            }
        }
    }
}
