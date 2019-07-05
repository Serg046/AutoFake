using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace AutoFake
{
    internal class GeneratedObject
    {
        private readonly TypeInfo _typeInfo;

        public GeneratedObject(TypeInfo typeInfo)
        {
            _typeInfo = typeInfo;
        }

        public Assembly Assembly { get; private set; }
        public object Instance { get; internal set; }
        public Type Type { get; internal set; }
        public IList<MockedMemberInfo> MockedMembers { get; } = new List<MockedMemberInfo>();
        public bool IsBuilt { get; private set; }
        public IList<object> Parameters { get; } = new List<object>();

        public void Build()
        {
            using (var memoryStream = new MemoryStream())
            {
                _typeInfo.WriteAssembly(memoryStream);
                Assembly = Assembly.Load(memoryStream.ToArray());
                Type = Assembly.GetType(_typeInfo.FullTypeName, true);

                foreach (var mockedMemberInfo in MockedMembers)
                {
                    mockedMemberInfo.Mock.Initialize(mockedMemberInfo, this);
                }

                Instance = !IsStatic(_typeInfo.SourceType) ? _typeInfo.CreateInstance(Type) : null;
                IsBuilt = true;
            }
        }

        private bool IsStatic(Type type) => type.IsAbstract && type.IsSealed;
    }
}
