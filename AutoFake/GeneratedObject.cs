using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AutoFake.Expression;
using InvocationExpression = AutoFake.Expression.InvocationExpression;
using LinqExpression = System.Linq.Expressions.Expression;

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

        public void AcceptMemberVisitor(LinqExpression expression, IMemberVisitor visitor)
        {
            var invocationExpression = new InvocationExpression(expression);
            invocationExpression.AcceptMemberVisitor(new TargetMemberVisitor(visitor, Type));
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
