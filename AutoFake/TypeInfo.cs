using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AutoFake.Exceptions;
using AutoFake.Setup;
using Mono.Cecil;

namespace AutoFake
{
    internal class TypeInfo
    {
        private const string FAKE_NAMESPACE = "AutoFake.Fakes";
        private const BindingFlags CONSTRUCTOR_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private readonly Lazy<AssemblyDefinition> _assemblyDefinition;
        private readonly Lazy<TypeDefinition> _typeDefinition;
        private readonly IList<FakeDependency> _dependencies;

        public TypeInfo(Type sourceType, IList<FakeDependency> dependencies)
        {
            SourceType = sourceType;
            _dependencies = dependencies;

            //TODO: remove reading mode parameter when a new version of mono.cecil will be available, see https://github.com/jbevain/cecil/issues/295
            _assemblyDefinition = new Lazy<AssemblyDefinition>(() =>
                AssemblyDefinition.ReadAssembly(SourceType.Module.FullyQualifiedName, new ReaderParameters(ReadingMode.Immediate)));

            _typeDefinition = new Lazy<TypeDefinition>(() =>
            {
                var type = _assemblyDefinition.Value.MainModule.GetType(SourceType.FullName, true);
                var typeDefinition = type.Resolve();
                typeDefinition.Name = typeDefinition.Name + "Fake";
                typeDefinition.Namespace = FAKE_NAMESPACE;
                return typeDefinition;
            });
        }

        public Type SourceType { get; }
        
        public string FullTypeName => _typeDefinition.Value.FullName.Replace('/', '+');

        public string GetInstalledMethodTypeName(FakeSetupPack setup)
        {
            string result = null;
            var type = setup.Method.DeclaringType;

            Func<string, string> combineFunc = typeName => result == null ? typeName : typeName + "/" + result;

            var tmpType = type;
            do
            {
                if (tmpType == SourceType)
                    return combineFunc(_typeDefinition.Value.FullName);
                else
                    result = combineFunc(tmpType.Name);
            } while ((tmpType = tmpType.DeclaringType) != null);

            return type.Namespace + "." + result;
        }

        public TypeReference Import(Type type) => _assemblyDefinition.Value.MainModule.Import(type);
        public MethodReference Import(MethodBase method) => _assemblyDefinition.Value.MainModule.Import(method);

        public void AddField(FieldDefinition field) => _typeDefinition.Value.Fields.Add(field);
        public void AddMethod(MethodDefinition method) => _typeDefinition.Value.Methods.Add(method);

        public IEnumerable<FieldDefinition> Fields => _typeDefinition.Value.Fields; 
        public IEnumerable<MethodDefinition> Methods => _typeDefinition.Value.Methods;

        public void WriteAssembly(Stream stream)
        {
            _assemblyDefinition.Value.Write(stream);
        }

        public object CreateInstance(Type type)
        {
            if (_dependencies.Any(d => d.Type == null))
            {
                try
                {
                    var args = _dependencies.Select(d => d.Instance).ToArray();
                    return Activator.CreateInstance(type, CONSTRUCTOR_FLAGS, null, args, null);
                }
                catch (AmbiguousMatchException)
                {
                    throw new FakeGeneretingException(
                        $"Ambiguous null-invocation. Please use {nameof(Arg)}.{nameof(Arg.IsNull)}<T>() instead of null.");
                }
            }

            var constructor = type.GetConstructor(CONSTRUCTOR_FLAGS,
                null, _dependencies.Select(d => d.Type).ToArray(), null);

            if (constructor == null)
                throw new FakeGeneretingException("Constructor is not found");

            return constructor.Invoke(_dependencies.Select(d => d.Instance).ToArray());
        }
    }
}
