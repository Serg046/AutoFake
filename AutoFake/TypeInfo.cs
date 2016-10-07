using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AutoFake.Exceptions;
using AutoFake.Setup;
using GuardExtensions;
using Mono.Cecil;

namespace AutoFake
{
    internal class TypeInfo
    {
        private const string FAKE_NAMESPACE = "AutoFake.Fakes";

        private AssemblyDefinition _assemblyDefinition;
        private TypeDefinition _typeDefinition;
        private MethodReference _addToListMethodInfo;
        private readonly IList<FakeDependency> _dependencies;

        public TypeInfo(Type sourceType, IList<FakeDependency> dependencies)
        {
            Guard.AreNotNull(sourceType, dependencies);
            SourceType = sourceType;

            _dependencies = dependencies;
        }

        public Type SourceType { get; }
        
        public string FullTypeName => _typeDefinition.FullName.Replace('/', '+');
        public MethodReference AddToListMethodInfo => _addToListMethodInfo;

        public void Load()
        {
            //TODO: remove reading mode parameter when a new version of mono.cecil will be available, see https://github.com/jbevain/cecil/issues/295
            _assemblyDefinition = AssemblyDefinition.ReadAssembly(SourceType.Module.FullyQualifiedName, new ReaderParameters(ReadingMode.Immediate));

            var type = _assemblyDefinition.MainModule.GetType(SourceType.FullName, true);
            _typeDefinition = type.Resolve();
            _typeDefinition.Name = _typeDefinition.Name + "Fake";
            _typeDefinition.Namespace = FAKE_NAMESPACE;

            _addToListMethodInfo = Import(typeof(List<int>).GetMethod("Add"));
        }

        public string GetInstalledMethodTypeName(FakeSetupPack setup)
        {
            Guard.IsNotNull(setup);

            string result = null;
            var type = setup.Method.DeclaringType;

            Func<string, string> combineFunc = typeName => result == null ? typeName : typeName + "/" + result;

            var tmpType = type;
            do
            {
                if (tmpType == SourceType)
                    return combineFunc(_typeDefinition.FullName);
                else
                    result = combineFunc(tmpType.Name);
            } while ((tmpType = tmpType.DeclaringType) != null);

            return type.Namespace + "." + result;
        }

        public TypeReference Import(Type type) => _assemblyDefinition.MainModule.Import(type);
        public MethodReference Import(MethodBase method) => _assemblyDefinition.MainModule.Import(method);

        public void AddField(FieldDefinition field) => _typeDefinition.Fields.Add(field);
        public void AddMethod(MethodDefinition method) => _typeDefinition.Methods.Add(method);

        public IEnumerable<FieldDefinition> Fields => _typeDefinition.Fields; 
        public IEnumerable<MethodDefinition> Methods => _typeDefinition.Methods;

        public void WriteAssembly(Stream stream)
        {
            _assemblyDefinition.Write(stream);
        }

        public object CreateInstance(Type type)
        {
            var constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, _dependencies.Select(d => d.Type).ToArray(), null);

            if (constructor == null)
                throw new FakeGeneretingException("Constructor is not found");

            return constructor.Invoke(_dependencies.Select(d => d.Instance).ToArray());
        }
    }
}
