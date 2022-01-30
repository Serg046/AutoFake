using System;
using System.Linq.Expressions;
using System.Reflection;
using AutoFake.Abstractions.Expression;
using AutoFake.Abstractions.Setup;
using AutoFake.Setup;

namespace AutoFake.Expression
{
    internal class GetSourceMemberVisitor : IGetSourceMemberVisitor
    {
	    private readonly Func<MethodBase, SourceMethod> _getSourceMethod;
	    private readonly Func<FieldInfo, SourceField> _getSourceField;
	    private ISourceMember? _sourceMember;

        public GetSourceMemberVisitor(
	        Func<MethodBase, SourceMethod> getSourceMethod,
	        Func<FieldInfo, SourceField> getSourceField)
        {
	        _getSourceMethod = getSourceMethod;
	        _getSourceField = getSourceField;
        }

        public ISourceMember SourceMember => _sourceMember ?? throw new InvalidOperationException($"{nameof(SourceMember)} is not set. Please run {nameof(Visit)}() method.");

        public void Visit(NewExpression newExpression, ConstructorInfo constructorInfo) => _sourceMember = _getSourceMethod(constructorInfo);

        public void Visit(MethodCallExpression methodExpression, MethodInfo methodInfo) => _sourceMember = _getSourceMethod(methodInfo);

        public void Visit(PropertyInfo propertyInfo)
	        => _sourceMember = _getSourceMethod(propertyInfo.GetGetMethod(true) ?? throw new InvalidOperationException("Cannot find a getter"));

        public void Visit(FieldInfo fieldInfo) => _sourceMember = _getSourceField(fieldInfo);
    }
}
