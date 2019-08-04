using System;
using System.Linq.Expressions;
using System.Reflection;
using AutoFake.Setup;

namespace AutoFake.Expression
{
    internal class GetSourceMemberVisitor : IMemberVisitor
    {
        private ISourceMember _sourceMember;
        private bool _isRuntimeValueSet;

        public ISourceMember SourceMember
        {
            get
            {
                if (!_isRuntimeValueSet)
                    throw new InvalidOperationException($"{nameof(SourceMember)} is not set. Please run {nameof(Visit)}() method.");
                return _sourceMember;
            }
            private set
            {
                _sourceMember = value;
                _isRuntimeValueSet = true;
            }
        }

        public void Visit(NewExpression newExpression, ConstructorInfo constructorInfo) => SourceMember = new SourceMethod(constructorInfo);

        public void Visit(MethodCallExpression methodExpression, MethodInfo methodInfo) => SourceMember = new SourceMethod(methodInfo);

        public void Visit(PropertyInfo propertyInfo) => SourceMember = new SourceMethod(propertyInfo.GetGetMethod(true));

        public void Visit(FieldInfo fieldInfo) => SourceMember = new SourceField(fieldInfo);
    }
}
