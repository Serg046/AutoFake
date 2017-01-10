using System;
using System.Linq.Expressions;
using System.Reflection;

namespace AutoFake.Expression
{
    internal class GetSourceMemberVisitor : IMemberVisitor
    {
        private MethodInfo _sourceMember;
        private bool _isRuntimeValueSet;

        public MethodInfo SourceMember
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

        public void Visit(MethodCallExpression methodExpression, MethodInfo methodInfo) => SourceMember = methodInfo;

        public void Visit(PropertyInfo propertyInfo) => SourceMember = propertyInfo.GetGetMethod(true);

        public void Visit(FieldInfo fieldInfo)
        {
            throw new NotImplementedException();
        }
    }
}
