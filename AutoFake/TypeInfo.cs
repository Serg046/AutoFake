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
    internal class TypeInfo : ITypeInfo
    {
        private const BindingFlags CONSTRUCTOR_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private readonly AssemblyDefinition _assemblyDefinition;
        private readonly TypeDefinition _typeDefinition;
        private readonly IList<FakeDependency> _dependencies;
        private readonly Dictionary<string, ushort> _addedFields;

        public TypeInfo(Type sourceType, IList<FakeDependency> dependencies)
        {
            SourceType = sourceType;
            _dependencies = dependencies;
            _addedFields = new Dictionary<string, ushort>();

            _assemblyDefinition = AssemblyDefinition.ReadAssembly(SourceType.Module.FullyQualifiedName);

            var type = _assemblyDefinition.MainModule.GetType(SourceType.FullName, runtimeName: true);
            _typeDefinition = type.Resolve();
        }

        public Type SourceType { get; }
        
        public string FullTypeName => GetClrName(_typeDefinition.FullName);

        public ModuleDefinition Module => _assemblyDefinition.MainModule;

        public void AddField(FieldDefinition field)
        {
            if (!_addedFields.ContainsKey(field.Name))
            {
                _typeDefinition.Fields.Add(field);
                _addedFields.Add(field.Name, 0);
            }
            else
            {
                _addedFields[field.Name]++;
                field.Name += _addedFields[field.Name];
                _typeDefinition.Fields.Add(field);
            }
        }

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

        internal static string GetClrName(string monoCecilTypeName) => monoCecilTypeName.Replace('/', '+');

        public FakeObjectInfo CreateFakeObject(MockCollection mocks)
        {
            using (var memoryStream = new MemoryStream())
            {
                var fakeGenerator = new FakeGenerator(this);
                foreach (var mock in mocks)
                {
                    fakeGenerator.Generate(mock.Mocks, mock.Method);
                }

                WriteAssembly(memoryStream);
                var assembly = Assembly.Load(memoryStream.ToArray());
                var type = assembly.GetType(FullTypeName, true);
                var instance = !IsStatic(SourceType) ? CreateInstance(type) : null;
                var parameters = mocks
                    .SelectMany(m => m.Mocks)
                    .SelectMany(m => m.Initialize(type))
                    .ToList();
                return new FakeObjectInfo(parameters, type, instance);
            }
        }

        private bool IsStatic(Type type) => type.IsAbstract && type.IsSealed;
    }
}
