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

            var type = _assemblyDefinition.MainModule.GetType(SourceType.FullName, true);
            _typeDefinition = type.Resolve();

            CreateInstanceByReflectionFunc = new FieldDefinition("CreateInstanceByReflectionFunc",
                Mono.Cecil.FieldAttributes.Assembly | Mono.Cecil.FieldAttributes.Static,
                Module.Import(typeof(Func<Type, IEnumerable<object>, object>)));
            AddField(CreateInstanceByReflectionFunc);
        }

        public FieldDefinition CreateInstanceByReflectionFunc { get; }

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

        public IEnumerable<FieldDefinition> Fields => _typeDefinition.Fields; 
        public IEnumerable<MethodDefinition> Methods => _typeDefinition.Methods;

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

        public MethodReference ConvertToSourceAssembly(MethodReference constructor)
        {
            var typeName = GetClrName(constructor.DeclaringType.FullName);
            var type = SourceType.Assembly.GetType(typeName);
            var ctor = type.GetConstructors().Single(c => c.GetParameters().Select(p => p.ParameterType.FullName)
                .SequenceEqual(constructor.Parameters.Select(p => GetClrName(p.ParameterType.FullName))));
            return Module.Import(ctor);
        }

        private string GetClrName(string monoCecilTypeName) => monoCecilTypeName.Replace('/', '+');
    }
}
