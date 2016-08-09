using System;
using System.Linq;
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

        //see http://stackoverflow.com/questions/36861196/how-to-serialize-method-call-expression-with-arguments/36862531
        public static object GetArgument(Expression expr)
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
