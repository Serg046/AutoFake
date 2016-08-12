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
        private TypeDefinition _typeDefinition;

        public FakeGenerator(object[] constructorArgs)
        {
            _constructorArgs = constructorArgs;
        }

        public object Generate(IList<FakeSetupPack> setups)
        {
            var type = typeof(T);
            _assemblyDefinition = AssemblyDefinition.ReadAssembly(type.Assembly.GetFiles().Single());
            _typeDefinition = _assemblyDefinition.MainModule.Types.Single(t => t.FullName == type.FullName);
            _typeDefinition.Name = _typeDefinition.Name + "Fake";
            _typeDefinition.Namespace = FAKE_NAMESPACE;

            MockSetups(setups);

            using (var memoryStream = new MemoryStream())
            {
                _assemblyDefinition.Write(memoryStream);
                var assembly = System.Reflection.Assembly.Load(memoryStream.ToArray());
                var newType = assembly.GetType(_typeDefinition.FullName);
                return Activator.CreateInstance(newType, _constructorArgs);
            }
        }

        public void Save(string fileName)
        {
            using (var fileStream = File.Create(fileName))
            {
                _assemblyDefinition.Write(fileStream);
            }
        }

        public string GetFieldName(FakeSetupPack setup, int counter)
            => setup.Method.Name + counter;

        public string GetArgumentFieldName(FakeSetupPack setup, int counter)
            => setup.Method.Name + "Argument" + counter;

        private void MockSetups(IList<FakeSetupPack> setups)
        {
            var counter = 0;
            foreach (var setup in setups)
            {
                var field = new FieldDefinition(GetFieldName(setup, counter++), FieldAttributes.Public | FieldAttributes.Static,
                    _assemblyDefinition.MainModule.Import(setup.Method.ReturnType));
                _typeDefinition.Fields.Add(field);
                
                var reachableWithMethodNames = setup.ReachableWithCollection.Select(m => m.Name).ToList();
                var reachableWithMethods = _typeDefinition.Methods.Where(m => reachableWithMethodNames.Contains(m.Name));

                var callsCount = 0;
                foreach (var method in reachableWithMethods)
                {
                    ReplaceInstructions(method, setup, field, ref callsCount);
                }
                if (setup.ExpectedCallsCount != -1 && callsCount != setup.ExpectedCallsCount)
                {
                    throw new InvalidOperationException($"Setup and actual calls count are different. Expected: { setup.ExpectedCallsCount }. Actual: { callsCount}.");
                }
                setup.ActualCallsCount = callsCount;
            }
        }

        private void ReplaceInstructions(MethodDefinition currentMethod, FakeSetupPack setup, FieldDefinition field, ref int callsCount)
        {
            var methodToReplace = setup.Method;
            foreach (var instruction in currentMethod.Body.Instructions.ToList())
            {
                if (instruction.OpCode.OperandType == OperandType.InlineMethod)
                {
                    var methodReference = (MethodReference)instruction.Operand;
                    
                    if (AreSameInCurrentType(methodReference, methodToReplace) || AreSameInExternalType(methodReference, methodToReplace))
                    {
                        Inject(currentMethod.Body.GetILProcessor(), methodToReplace.GetParameters().Count(), field, instruction, setup, callsCount++);
                    }
                    else if (methodReference.IsDefinition)
                    {
                        ReplaceInstructions(methodReference.Resolve(), setup, field, ref callsCount);
                    }
                }
            }
        }

        private bool AreSameInCurrentType(MethodReference methodReference, System.Reflection.MethodInfo methodToReplace)
            => methodToReplace.DeclaringType == typeof(T)
                        && methodReference.DeclaringType.FullName == _typeDefinition.FullName
                        && methodReference.Name == methodToReplace.Name;

        private bool AreSameInExternalType(MethodReference methodReference, System.Reflection.MethodInfo methodToReplace)
            => methodReference.DeclaringType.FullName == methodToReplace.DeclaringType.FullName
                        && methodReference.Name == methodToReplace.Name;

        private void Inject(ILProcessor processor, int parametersCount, FieldDefinition field, Instruction instruction, FakeSetupPack setup, int callsCount)
        {
            var methodReference = (MethodReference)instruction.Operand;

            if (parametersCount > 0)
            {
                for (var i = 0; i < parametersCount; i++)
                {
                    if (setup.IsVerifiable)
                    {
                        var idx = parametersCount - i - 1;
                        var currentArg = setup.SetupArguments[idx];
                        var fldName = GetArgumentFieldName(setup, parametersCount * callsCount + idx);
                        var argField = new FieldDefinition(fldName, FieldAttributes.Public | FieldAttributes.Static,
                            _assemblyDefinition.MainModule.Import(currentArg.GetType()));
                        _typeDefinition.Fields.Add(argField);

                        processor.InsertBefore(instruction, processor.Create(OpCodes.Stsfld, argField));
                    }
                    else
                    {
                        processor.InsertBefore(instruction, processor.Create(OpCodes.Pop));
                    }
                }
            }
            if (!methodReference.Resolve().IsStatic)
                processor.InsertBefore(instruction, processor.Create(OpCodes.Pop));

            processor.Replace(instruction, processor.Create(OpCodes.Ldsfld, field));
        }
    }
}
