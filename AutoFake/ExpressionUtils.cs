using System;
using System.Linq.Expressions;
using System.Reflection;

namespace AutoFake
{
    public static class ExpressionUtils
    {
        public static MethodInfo GetMethodInfo(LambdaExpression expression)
        {
            MethodInfo method;

            if (expression.Body is MethodCallExpression)
            {
                method = ((MethodCallExpression)expression.Body).Method;
            }
            else if (expression.Body is MemberExpression)
            {
                var member = ((MemberExpression)expression.Body).Member as PropertyInfo;
                if (member == null)
                    throw new InvalidOperationException($"MemberExpression must be a property expression. Source: {expression.ToString()}.");
                method = member.GetGetMethod();
            }
            else
                throw new InvalidOperationException($"Ivalid expression format. Source: {expression.ToString()}.");

            return method;
        }
    }
}
