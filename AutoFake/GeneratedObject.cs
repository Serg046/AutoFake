using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public object Instance { get; internal set; }
        public Type Type { get; internal set; }
        public IList<MockedMemberInfo> MockedMembers { get; } = new List<MockedMemberInfo>();
        public bool IsBuilt { get; private set; }

        public void Build()
        {
            using (var memoryStream = new MemoryStream())
            {
                _typeInfo.WriteAssembly(memoryStream);
                var assembly = Assembly.Load(memoryStream.ToArray());
                Type = assembly.GetType(_typeInfo.FullTypeName, true);

                Initialize();

                Instance = !IsStatic(_typeInfo.SourceType) ? _typeInfo.CreateInstance(Type) : null;
                IsBuilt = true;
            }
        }

        private void Initialize()
        {
            var field = GetField(this, _typeInfo.CreateInstanceByReflectionFunc.Name);
            Func<Type, IEnumerable<string>, object> creator = CreateInstanceByReflection; 
            field.SetValue(null, creator);

            foreach (var mockedMemberInfo in MockedMembers)
            {
                mockedMemberInfo.Mock.Initialize(mockedMemberInfo, this);
            }
        }

        private object CreateInstanceByReflection(Type ctorType, IEnumerable<string> ctorArgs)
        {
            var ctor = ctorType.GetConstructors().SingleOrDefault(c => c.GetParameters()
                .Select(p => p.ParameterType.FullName)
                .SequenceEqual(ctorArgs));
            return ctor?.Invoke(null)
                   ?? throw new ArgumentException($"Constructor for the type '{ctorType}' is not found");
        }

        protected FieldInfo GetField(GeneratedObject generatedObject, string fieldName)
            => generatedObject.Type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);

        private bool IsStatic(Type type) => type.IsAbstract && type.IsSealed;
    }
}
