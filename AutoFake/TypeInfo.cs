using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AutoFake.Exceptions;
using Mono.Cecil;

namespace AutoFake
{
    internal class TypeInfo : ITypeInfo
    {
        private const BindingFlags CONSTRUCTOR_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private readonly AssemblyDefinition _assemblyDefinition;
        private readonly TypeDefinition _typeDefinition;
        private readonly IList<FakeDependency> _dependencies;

        public TypeInfo(Type sourceType, IList<FakeDependency> dependencies)
        {
            SourceType = sourceType;
            _dependencies = dependencies;

            //TODO: remove reading mode parameter when a new version of mono.cecil will be available, see https://github.com/jbevain/cecil/issues/295
            _assemblyDefinition = AssemblyDefinition.ReadAssembly(SourceType.Module.FullyQualifiedName, new ReaderParameters(ReadingMode.Immediate));

            var type = _assemblyDefinition.MainModule.GetType(SourceType.FullName, runtimeName: true);
            _typeDefinition = type.Resolve();
        }

        public Type SourceType { get; }
        
        public string FullTypeName => GetClrName(_typeDefinition.FullName);

        public ModuleDefinition Module => _assemblyDefinition.MainModule;

        public string GetMonoCecilTypeName(Type declaringType)
            => declaringType == SourceType
                ? _typeDefinition.FullName
                : declaringType.Namespace + "." + GetAllTypes(declaringType)
                    .Aggregate(string.Empty, (current, next) => next + "/" + current)
                    .TrimEnd('/');

        private IEnumerable<string> GetAllTypes(Type type)
        {
            var tmpType = type;
            do
            {
                yield return tmpType.Name;
            } while ((tmpType = tmpType.DeclaringType) != null);
        } 

        public void AddField(FieldDefinition field) => _typeDefinition.Fields.Add(field);
        public void AddMethod(MethodDefinition method) => _typeDefinition.Methods.Add(method);

        public ICollection<FieldDefinition> Fields => _typeDefinition.Fields; 
        public ICollection<MethodDefinition> Methods => _typeDefinition.Methods;

        public void WriteAssembly(Stream stream)
        {
            _assemblyDefinition.Write(stream);
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

        private string GetClrName(string monoCecilTypeName) => monoCecilTypeName.Replace('/', '+');

        public FakeObjectInfo CreateFakeObject(IEnumerable<MockedMemberInfo> mockedMembers)
        {
            using (var memoryStream = new MemoryStream())
            {
                WriteAssembly(memoryStream);
                var assembly = Assembly.Load(memoryStream.ToArray());
                var type = assembly.GetType(FullTypeName, true);

                var parameters = new List<object>();
                foreach (var mockedMemberInfo in mockedMembers)
                {
                    parameters.AddRange(mockedMemberInfo.Mock.Initialize(mockedMemberInfo, type));
                }

                var instance = !IsStatic(SourceType) ? CreateInstance(type) : null;
                return new FakeObjectInfo(parameters, type, instance);
            }
        }

        private bool IsStatic(Type type) => type.IsAbstract && type.IsSealed;
    }
}
