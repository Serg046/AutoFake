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
    internal class GeneratedObject
    {
        public object Instance { get; internal set; }
        public Type Type { get; internal set; }
        public IList<MockedMemberInfo> MockedMembers { get; internal set; }

        public void AcceptMemberVisitor(Expression expression, IMemberVisitor visitor)
        {
            Guard.AreNotNull(expression, visitor);
            VisitExpression(expression, visitor);
        }

        private void VisitExpression(Expression expression, IMemberVisitor visitor)
        {
            try
            {
                VisitExpressionImpl((dynamic)expression, visitor);
            }
            catch (RuntimeBinderException)
            {
                throw new NotSupportedExpressionException(
                    $"Ivalid expression format. Type '{expression.GetType().FullName}'. Source: {expression}.");
            }
        }

        private void VisitExpressionImpl(UnaryExpression expression, IMemberVisitor visitor) => VisitExpression(expression.Operand, visitor);

        private void VisitExpressionImpl(MethodCallExpression expression, IMemberVisitor visitor)
        {
            var method = Type.GetMethod(expression.Method.Name,
                expression.Method.GetParameters().Select(p => p.ParameterType).ToArray());

            visitor.Visit(expression, method);
        }

        private void VisitExpressionImpl(MemberExpression expression, IMemberVisitor visitor)
        {
            try
            {
                VisitExpressionImpl((dynamic)expression.Member, visitor);
            }
            catch (RuntimeBinderException)
            {
                throw new NotSupportedExpressionException(
                    $"Ivalid MemberExpression format. Type '{expression.Member.GetType().FullName}'. Source: {expression}.");
            }
        }

        private void VisitExpressionImpl(PropertyInfo propertyInfo, IMemberVisitor visitor)
        {
            var property = Type.GetProperty(propertyInfo.Name);
            visitor.Visit(property);
        }

        private void VisitExpressionImpl(FieldInfo fieldInfo, IMemberVisitor visitor)
        {
            var field = Type.GetField(fieldInfo.Name);
            visitor.Visit(field);
        }
    }
}
