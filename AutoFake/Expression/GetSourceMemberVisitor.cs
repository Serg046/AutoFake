using System;
using System.Linq.Expressions;
using System.Reflection;
using AutoFake.Setup;

namespace AutoFake.Expression
{
    internal class GetSourceMemberVisitor : IMemberVisitor
    {
        private ISourceMember? _sourceMember;

        public ISourceMember SourceMember => _sourceMember ?? throw new InvalidOperationException($"{nameof(SourceMember)} is not set. Please run {nameof(Visit)}() method.");

        public void Visit(NewExpression newExpression, ConstructorInfo constructorInfo) => _sourceMember = new SourceMethod(constructorInfo);

        public void Visit(MethodCallExpression methodExpression, MethodInfo methodInfo) => _sourceMember = new SourceMethod(methodInfo);

        public void Visit(PropertyInfo propertyInfo)
	        => _sourceMember = new SourceMethod(propertyInfo.GetGetMethod(true)
	                                            ?? throw new InvalidOperationException("Cannot find a getter"));

        public void Visit(FieldInfo fieldInfo) => _sourceMember = new SourceField(fieldInfo);
    }
}
