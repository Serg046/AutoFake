using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FieldAttributes = Mono.Cecil.FieldAttributes;

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

        public GeneratedObject Generate(IList<FakeSetupPack> setups, MethodInfo executeFunc)
        {
            var generatedObject = new GeneratedObject();

            var type = typeof(T);
            _assemblyDefinition = AssemblyDefinition.ReadAssembly(type.Assembly.GetFiles().Single());
            _typeDefinition = _assemblyDefinition.MainModule.Types.Single(t => t.FullName == type.FullName);
            _typeDefinition.Name = _typeDefinition.Name + "Fake";
            _typeDefinition.Namespace = FAKE_NAMESPACE;

            generatedObject.MockedMembers = MockSetups(setups, executeFunc).ToList();

            using (var memoryStream = new MemoryStream())
            {
                _assemblyDefinition.Write(memoryStream);
                var assembly = Assembly.Load(memoryStream.ToArray());
                generatedObject.Type = assembly.GetType(_typeDefinition.FullName);
                generatedObject.Instance = Activator.CreateInstance(generatedObject.Type, _constructorArgs);
                return generatedObject;
            }
        }

        public void Save(string fileName)
        {
            using (var fileStream = File.Create(fileName))
            {
                _assemblyDefinition.Write(fileStream);
            }
        }

        private IEnumerable<MockedMemberInfo> MockSetups(IList<FakeSetupPack> setups, MethodInfo executeFunc)
        {
            var counter = 0;
            foreach (var setup in setups)
            {
                var mockedMemberInfo = new MockedMemberInfo(setup);

                if (!setup.IsVoid)
                {
                    var fieldName = setup.Method.Name + counter++;
                    mockedMemberInfo.ReturnValueField = new FieldDefinition(fieldName, FieldAttributes.Assembly | FieldAttributes.Static,
                        _assemblyDefinition.MainModule.Import(setup.Method.ReturnType));
                    _typeDefinition.Fields.Add(mockedMemberInfo.ReturnValueField);
                }
                
                ReplaceInstructions(_typeDefinition.Methods.Single(m => m.Name == executeFunc.Name), mockedMemberInfo);

                yield return mockedMemberInfo;
            }
        }

        private void ReplaceInstructions(MethodDefinition currentMethod, MockedMemberInfo mockedMemberInfo)
        {
            var methodToReplace = mockedMemberInfo.Setup.Method;
            foreach (var instruction in currentMethod.Body.Instructions.ToList())
            {
                if (instruction.OpCode.OperandType == OperandType.InlineMethod)
                {
                    var methodReference = (MethodReference)instruction.Operand;
                    
                    if (AreSameInCurrentType(methodReference, methodToReplace) || AreSameInExternalType(methodReference, methodToReplace))
                    {
                        Inject(currentMethod.Body.GetILProcessor(), mockedMemberInfo, instruction);
                    }
                    else if (methodReference.IsDefinition)
                    {
                        ReplaceInstructions(methodReference.Resolve(), mockedMemberInfo);
                    }
                }
            }
        }

        private bool AreSameInCurrentType(MethodReference methodReference, MethodInfo methodToReplace)
            => methodToReplace.DeclaringType == typeof(T)
                        && methodReference.DeclaringType.FullName == _typeDefinition.FullName
                        && methodReference.Name == methodToReplace.Name;

        private bool AreSameInExternalType(MethodReference methodReference, MethodInfo methodToReplace)
            => methodReference.DeclaringType.FullName == methodToReplace.DeclaringType.FullName
                        && methodReference.Name == methodToReplace.Name;

        private void Inject(ILProcessor processor, MockedMemberInfo mockedMemberInfo, Instruction instruction)
        {
            var methodReference = (MethodReference)instruction.Operand;
            var parametersCount = mockedMemberInfo.Setup.SetupArguments.Length;

            if (parametersCount > 0)
            {
                if (mockedMemberInfo.Setup.IsVerifiable)
                {
                    var argumentFields = new List<FieldDefinition>();

                    for (var i = 0; i < parametersCount; i++)
                    {
                        var idx = parametersCount - i - 1;
                        var currentArg = mockedMemberInfo.Setup.SetupArguments[idx];
                        var fieldName = mockedMemberInfo.Setup.Method.Name + "Argument" +
                            (parametersCount*mockedMemberInfo.ActualCallsCount + idx).ToString();
                        var field = new FieldDefinition(fieldName, FieldAttributes.Assembly | FieldAttributes.Static,
                            _assemblyDefinition.MainModule.Import(currentArg.GetType()));
                        _typeDefinition.Fields.Add(field);

                        argumentFields.Insert(0, field);
                        processor.InsertBefore(instruction, processor.Create(OpCodes.Stsfld, field));
                    }

                    mockedMemberInfo.ArgumentFields.Add(argumentFields);
                }
                else
                {
                    for (var i = 0; i < parametersCount; i++)
                    {
                        processor.InsertBefore(instruction, processor.Create(OpCodes.Pop));
                    }
                }
            }
            if (!methodReference.Resolve().IsStatic)
                processor.InsertBefore(instruction, processor.Create(OpCodes.Pop));

            if (mockedMemberInfo.Setup.IsVoid)
                processor.Remove(instruction);
            else
                processor.Replace(instruction, processor.Create(OpCodes.Ldsfld, mockedMemberInfo.ReturnValueField));

            mockedMemberInfo.ActualCallsCount++;
        }
    }
}
