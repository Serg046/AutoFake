using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AutoFake
{
    internal class FakeGenerator<T>
    {
        private const string FAKE_NAMESPACE = "AutoFake.Fakes";

        private readonly object[] _constructorArgs;
        private AssemblyDefinition _assemblyDefinition;

        public FakeGenerator(object[] constructorArgs)
        {
            _constructorArgs = constructorArgs;
        }

        public object Generate(IList<FakeSetupPack> setups)
        {
            var type = typeof(T);
            _assemblyDefinition = AssemblyDefinition.ReadAssembly(type.Assembly.GetFiles().Single());
            var typeDefinition = _assemblyDefinition.MainModule.Types.Single(t => t.FullName == type.FullName);
            typeDefinition.Name = typeDefinition.Name + "Fake";
            typeDefinition.Namespace = FAKE_NAMESPACE;

            MockSetups(_assemblyDefinition.MainModule, typeDefinition, setups);

            using (var memoryStream = new MemoryStream())
            {
                _assemblyDefinition.Write(memoryStream);
                var assembly = System.Reflection.Assembly.Load(memoryStream.ToArray());
                var newType = assembly.GetType(typeDefinition.FullName);
                return Activator.CreateInstance(newType, _constructorArgs);
            }
        }

        public void Save(string fileName)
        {
            foreach (var type in _assemblyDefinition.MainModule.Types.Where(t => t.Namespace.Length > 0 && t.Namespace != FAKE_NAMESPACE).ToList())
            {
                _assemblyDefinition.MainModule.Types.Remove(type);
            }

            using (var fileStream = File.Create(fileName))
            {
                _assemblyDefinition.Write(fileStream);
            }
        }

        public string GetFieldName(FakeSetupPack setup, int counter)
            => setup.Method.Name + counter;

        private void MockSetups(ModuleDefinition moduleDefinition, TypeDefinition typeDefinition, IList<FakeSetupPack> setups)
        {
            var counter = 0;
            foreach (var setup in setups)
            {
                var field = new FieldDefinition(GetFieldName(setup, ++counter), FieldAttributes.Public,
                    moduleDefinition.Import(setup.Method.ReturnType));
                typeDefinition.Fields.Add(field);
                
                var reachableWithMethodNames = setup.ReachableWithCollection.Select(m => m.Name).ToList();
                var reachableWithMethods = typeDefinition.Methods.Where(m => reachableWithMethodNames.Contains(m.Name));

                foreach (var method in reachableWithMethods)
                {
                    ReplaceInstructions(method, setup.Method, field);
                }
            }
        }

        private void ReplaceInstructions(MethodDefinition currentMethod, System.Reflection.MethodInfo methodToReplace, FieldDefinition field)
        {
            foreach (var instruction in currentMethod.Body.Instructions.ToList())
            {
                if (instruction.OpCode.OperandType == OperandType.InlineMethod)
                {
                    var methodReference = (MethodReference)instruction.Operand;
                    if (methodReference.DeclaringType.FullName == methodToReplace.DeclaringType.FullName
                        && methodReference.Name == methodToReplace.Name)
                    {
                        var processor = currentMethod.Body.GetILProcessor();
                        processor.InsertBefore(instruction, processor.Create(OpCodes.Ldarg_0));
                        processor.Replace(instruction, processor.Create(OpCodes.Ldfld, field));
                    }
                    else if (methodReference.DeclaringType == currentMethod.DeclaringType)
                    {
                        ReplaceInstructions(methodReference.Resolve(), methodToReplace, field);
                    }
                }
            }
        }
    }
}
