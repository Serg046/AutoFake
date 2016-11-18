using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AutoFake.Exceptions;
using Microsoft.CSharp.RuntimeBinder;

namespace AutoFake
{
    internal class GeneratedObject
    {
        private readonly TypeInfo _typeInfo;

        public GeneratedObject(TypeInfo typeInfo)
        {
            _typeInfo = typeInfo;
        }

        public object Instance { get; internal set; }
        public Type Type { get; internal set; }
        public IList<MockedMemberInfo> MockedMembers { get; } = new List<MockedMemberInfo>();
        public bool IsBuilt { get; private set; }

        public void AcceptMemberVisitor(Expression expression, IMemberVisitor visitor) => VisitExpression(expression, visitor);

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

        public void Build()
        {
            using (var memoryStream = new MemoryStream())
            {
                _typeInfo.WriteAssembly(memoryStream);
                var assembly = Assembly.Load(memoryStream.ToArray());
                Type = assembly.GetType(_typeInfo.FullTypeName, true);
                Instance = IsStatic(_typeInfo.SourceType)
                    ? null
                    : _typeInfo.CreateInstance(Type);
                IsBuilt = true;
            }
        }

        private bool IsStatic(Type type) => type.IsAbstract && type.IsSealed;
    }
}
