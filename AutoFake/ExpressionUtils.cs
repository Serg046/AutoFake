using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using InvocationExpression = AutoFake.Expression.InvocationExpression;
using LinqExpression = System.Linq.Expressions.Expression;

namespace AutoFake
{
    internal static class ExpressionUtils
    {
        public static MethodInfo GetMethodInfo(LambdaExpression expression)
        {
            var invocationExpression = new InvocationExpression(expression);
            return invocationExpression.GetSourceMember();
        }

        public static IList<FakeArgument> GetArguments(LinqExpression expression)
        {
            var invocationExpression = new InvocationExpression(expression);
            return invocationExpression.GetArguments();
        }
    }
}
