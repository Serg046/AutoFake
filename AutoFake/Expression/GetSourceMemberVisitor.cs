using System;
using System.Linq.Expressions;
using System.Reflection;
using AutoFake.Abstractions.Expression;
using AutoFake.Abstractions.Setup;

namespace AutoFake.Expression
{
	internal class GetSourceMemberVisitor : IGetSourceMemberVisitor
	{
		private readonly Func<MethodBase, ISourceMethod> _getSourceMethod;
		private readonly Func<FieldInfo, ISourceField> _getSourceField;

		public GetSourceMemberVisitor(
			Func<MethodBase, ISourceMethod> getSourceMethod,
			Func<FieldInfo, ISourceField> getSourceField)
		{
			_getSourceMethod = getSourceMethod;
			_getSourceField = getSourceField;
		}

		public ISourceMember Visit(NewExpression newExpression, ConstructorInfo constructorInfo) => _getSourceMethod(constructorInfo);

		public ISourceMember Visit(MethodCallExpression methodExpression, MethodInfo methodInfo) => _getSourceMethod(methodInfo);

		public ISourceMember Visit(PropertyInfo propertyInfo)
			=> _getSourceMethod(propertyInfo.GetGetMethod(true) ?? throw new NotSupportedException("Cannot find a getter"));

		public ISourceMember Visit(FieldInfo fieldInfo) => _getSourceField(fieldInfo);
	}
}
