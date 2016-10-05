using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AutoFake.Exceptions;
using GuardExtensions;
using Microsoft.CSharp.RuntimeBinder;

namespace AutoFake
{
    internal static class ExpressionUtils
    {
        public static MethodInfo GetMethodInfo(LambdaExpression expression)
        {
            Guard.IsNotNull(expression);
            return GetMethodInfo(expression.Body);
        }

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
                    throw new InvalidOperationException(
                        $"MemberExpression must be a property expression. Source: {expression}.");
                method = member.GetGetMethod() ?? member.GetGetMethod(true);
            }
            else if (expression is UnaryExpression)
            {
                method = GetMethodInfo(((UnaryExpression)expression).Operand);
            }
            else
                throw new NotSupportedExpressionException(
                    $"Ivalid expression format. Type '{expression.GetType().FullName}'. Source: {expression}.");

            return method;
        }

        //------------------------------------------------------------------------------------------------------------

        public static IEnumerable<object> GetArguments(MethodCallExpression expression)
        {
            Guard.IsNotNull(expression);
            return expression.Arguments.Select(expr => GetArgument(() => expr));
        }

        private static object GetArgument(Func<Expression> expressionFunc)
        {
            var expression = expressionFunc();
            try
            {
                return GetArgument((dynamic)expression);
            }
            catch (RuntimeBinderException)
            {
                throw new NotSupportedExpressionException(
                    $"Ivalid expression format. Type '{expression.GetType().FullName}'. Source: {expression}.");
            }
        }

        private static object GetArgument(ConstantExpression expression) => expression.Value;

        private static object GetArgument(UnaryExpression expression) => GetArgument(() => expression.Operand);

        private static object GetArgument(MemberExpression expression) => CompileAndRun(expression);

        private static object GetArgument(NewExpression expression) => CompileAndRun(expression);

        private static object GetArgument(MethodCallExpression expression) => CompileAndRun(expression);

        private static object CompileAndRun(Expression expression)
        {
            var convertExpr = Expression.Convert(expression, typeof(object));
            var lambda = Expression.Lambda<Func<object>>(convertExpr);
            return lambda.Compile().Invoke();
        }

        //------------------------------------------------------------------------------------------------------------

        public static object ExecuteExpression(GeneratedObject generatedObject, Expression executeFunc)
        {
            Guard.AreNotNull(generatedObject, executeFunc);
            return ExecuteExpressionImpl(generatedObject, () => executeFunc);
        }

        private static object ExecuteExpressionImpl(GeneratedObject generatedObject, Func<Expression> executeFunc)
        {
            var expression = executeFunc();
            try
            {
                return ExecuteExpressionImpl(generatedObject, (dynamic)expression);
            }
            catch (RuntimeBinderException)
            {
                throw new NotSupportedExpressionException(
                    $"Ivalid expression format. Type '{expression.GetType().FullName}'. Source: {expression}.");
            }
        }

        private static object ExecuteExpressionImpl(GeneratedObject generatedObject, UnaryExpression executeFunc)
        {
            return ExecuteExpressionImpl(generatedObject, () => executeFunc.Operand);
        }

        private static object ExecuteExpressionImpl(GeneratedObject generatedObject, MethodCallExpression executeFunc)
        {
            var method = generatedObject.Type.GetMethod(executeFunc.Method.Name,
                executeFunc.Method.GetParameters().Select(p => p.ParameterType).ToArray());

            var instanceExpr = generatedObject.Instance == null || method.IsStatic ? null : Expression.Constant(generatedObject.Instance);
            var callExpression = Expression.Call(instanceExpr, method, executeFunc.Arguments);

            return Expression.Lambda(callExpression).Compile().DynamicInvoke();
        }

        private static object ExecuteExpressionImpl(GeneratedObject generatedObject, MemberExpression executeFunc)
        {
            try
            {
                return ExecuteMemberExpressionImpl(generatedObject, (dynamic)executeFunc.Member);
            }
            catch (RuntimeBinderException)
            {
                throw new NotSupportedExpressionException(
                    $"Ivalid MemberExpression format. Type '{executeFunc.Member.GetType().FullName}'. Source: {executeFunc}.");
            }
        }

        private static object ExecuteMemberExpressionImpl(GeneratedObject generatedObject, PropertyInfo propertyInfo)
        {
            var property = generatedObject.Type.GetProperty(propertyInfo.Name);
            return property.GetValue(generatedObject.Instance, null);
        }

        private static object ExecuteMemberExpressionImpl(GeneratedObject generatedObject, FieldInfo fieldInfo)
        {
            var field = generatedObject.Type.GetField(fieldInfo.Name);
            return field.GetValue(generatedObject.Instance);
        }
    }
}
