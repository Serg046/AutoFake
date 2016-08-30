using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace AutoFake
{
    internal class TypeInfo
    {
        private const string FAKE_NAMESPACE = "AutoFake.Fakes";

        private AssemblyDefinition _assemblyDefinition;
        private TypeDefinition _typeDefinition;

        public TypeInfo(Type sourceType, object[] contructorArgs)
        {
            SourceType = sourceType;
            ContructorArguments = contructorArgs;
        }

        public Type SourceType { get; }
        public object[] ContructorArguments { get; }

        public ModuleDefinition ModuleDefinition => _assemblyDefinition.MainModule;
        public string FullTypeName => _typeDefinition.FullName;

        public void Load()
        {
            _assemblyDefinition = AssemblyDefinition.ReadAssembly(SourceType.Assembly.GetFiles().Single());

            _typeDefinition = ModuleDefinition.Types.Single(t => t.FullName == SourceType.FullName);
            _typeDefinition.Name = _typeDefinition.Name + "Fake";
            _typeDefinition.Namespace = FAKE_NAMESPACE;
        }

        public void AddField(FieldDefinition field) => _typeDefinition.Fields.Add(field);

        public MethodDefinition SearchMethod(string methodName)
        {
            var method = _typeDefinition.Methods.SingleOrDefault(m => m.Name == methodName);
            if (method == null)
                throw new MissingMethodException($"Method '{methodName}' is not found");
            return method;
        }

        public IEnumerable<MethodDefinition> SearchMethods(string methodName)
        {
            return _typeDefinition.Methods.Where(m => m.Name == methodName);
        }

        public void WriteAssembly(Stream stream)
        {
            _assemblyDefinition.Write(stream);
        }
    }
}
