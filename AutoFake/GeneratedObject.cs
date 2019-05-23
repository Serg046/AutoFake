using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AutoFake
{
    internal class GeneratedObject
    {
        internal readonly TypeInfo _typeInfo;

        public GeneratedObject(TypeInfo typeInfo)
        {
            _typeInfo = typeInfo;
        }

        public Assembly Assembly { get; private set; }
        public object Instance { get; internal set; }
        public Type Type { get; internal set; }
        public IList<MockedMemberInfo> MockedMembers { get; } = new List<MockedMemberInfo>();
        public bool IsBuilt { get; private set; }
        public List<object> Parameters { get; } = new List<object>();

        public void Build()
        {
            using (var memoryStream = new MemoryStream())
            {
                _typeInfo.WriteAssembly(memoryStream);
                Assembly = Assembly.Load(memoryStream.ToArray());
                Type = Assembly.GetType(_typeInfo.FullTypeName, true);

                Initialize();

                Instance = !IsStatic(_typeInfo.SourceType) ? _typeInfo.CreateInstance(Type) : null;
                IsBuilt = true;
            }
        }

        private void Initialize()
        {
            var field = GetField(this, _typeInfo.CreateInstanceByReflectionFunc.Name);
            Func<Type, IEnumerable<object>, object> creator = CreateInstanceByReflection; 
            field.SetValue(null, creator);

            foreach (var mockedMemberInfo in MockedMembers)
            {
                mockedMemberInfo.Mock.Initialize(mockedMemberInfo, this);
            }
        }

        private object CreateInstanceByReflection(Type ctorType, IEnumerable<object> ctorArgs)
        {
            if (ctorArgs == null) throw new ArgumentNullException(nameof(ctorArgs));
            var ctor = ctorType.GetConstructors().SingleOrDefault(c => c.GetParameters()
                .Select(p => p.ParameterType.FullName).SequenceEqual(ctorArgs
                .Select(p => p.GetType().FullName)));
            return ctor?.Invoke(ctorArgs.ToArray())
                   ?? throw new ArgumentException($"Constructor for the type '{ctorType}' is not found");
        }

        protected FieldInfo GetField(GeneratedObject generatedObject, string fieldName)
            => generatedObject.Type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);

        private bool IsStatic(Type type) => type.IsAbstract && type.IsSealed;
    }
}
