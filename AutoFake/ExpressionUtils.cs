using System;
using System.Linq.Expressions;
using System.Reflection;

namespace AutoFake
{
    public static class ExpressionUtils
    {
        public static MethodInfo GetMethodInfo(LambdaExpression expression)
            => GetMethodInfo(expression.Body);

        private static MethodInfo GetMethodInfo(Expression expression)
        {
            MethodInfo method;

            if (expression is MethodCallExpression)
            {
                method = ((MethodCallExpression)expression).Method;
            }
            else if (expression is MemberExpression)
            {
                var member = ((MemberExpression)expression).Member as PropertyInfo;
                if (member == null)
                    throw new InvalidOperationException($"MemberExpression must be a property expression. Source: {expression.ToString()}.");
                method = member.GetGetMethod();
            }
            else if (expression is UnaryExpression)
            {
                method = GetMethodInfo(((UnaryExpression)expression).Operand);
            }
            else
                throw new InvalidOperationException($"Ivalid expression format. Source: {expression.ToString()}.");

            return method;
        }
    }
}
