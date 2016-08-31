using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GuardExtensions;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace AutoFake
{
    internal class FakeGenerator
    {
        private const string CONSTRUCTOR_METHOD_NAME = ".cctor";

        private readonly TypeInfo _typeInfo;

        public FakeGenerator(TypeInfo typeInfo)
        {
            Guard.IsNotNull(typeInfo);
            _typeInfo = typeInfo;
        }

        public GeneratedObject Generate(IList<FakeSetupPack> setups, MethodInfo executeFunc)
        {
            Guard.IsNotEmpty(setups);
            Guard.IsNotNull(executeFunc);

            _typeInfo.Load();

            var generatedObject = new GeneratedObject();
            generatedObject.MockedMembers = MockSetups(setups, executeFunc).ToList();

            using (var memoryStream = new MemoryStream())
            {
                _typeInfo.WriteAssembly(memoryStream);
                var assembly = Assembly.Load(memoryStream.ToArray());
                generatedObject.Type = assembly.GetType(_typeInfo.FullTypeName);
                generatedObject.Instance = Activator.CreateInstance(generatedObject.Type, _typeInfo.ContructorArguments);
                return generatedObject;
            }
        }

        public void Save(string fileName)
        {
            Guard.IsNotNull(fileName);
            using (var fileStream = File.Create(fileName))
            {
                _typeInfo.WriteAssembly(fileStream);
            }
        }

        private IEnumerable<MockedMemberInfo> MockSetups(IList<FakeSetupPack> setups, MethodInfo executeFunc)
        {
            var counter = 0;
            foreach (var setup in setups)
            {
                var mockedMemberInfo = new MockedMemberInfo(setup);
                var returnValueFieldName = setup.Method.Name + counter++;

                InitializeFieldFromStaticConstructor(mockedMemberInfo, returnValueFieldName);

                if (!setup.IsVoid)
                {
                    mockedMemberInfo.ReturnValueField = new FieldDefinition(returnValueFieldName, FieldAttributes.Assembly | FieldAttributes.Static,
                        _typeInfo.Import(setup.Method.ReturnType));
                    _typeInfo.AddField(mockedMemberInfo.ReturnValueField);
                }
                
                ReplaceInstructions(_typeInfo.SearchMethod(executeFunc.Name), mockedMemberInfo);

                yield return mockedMemberInfo;
            }
        }

        private void InitializeFieldFromStaticConstructor(MockedMemberInfo mockedMemberInfo, string returnValueFieldName)
        {
            var collectionType = typeof(List<int>);
            mockedMemberInfo.ActualCallsIdsField = new FieldDefinition(returnValueFieldName + "_ActualIds",
                FieldAttributes.Assembly | FieldAttributes.Static,
                _typeInfo.Import(collectionType));
            _typeInfo.AddField(mockedMemberInfo.ActualCallsIdsField);

            var listConstructor = collectionType.GetConstructor(new Type[0]);
            var processor = GetProcessor();

            processor.Append(processor.Create(OpCodes.Newobj, _typeInfo.Import(listConstructor)));
            processor.Append(processor.Create(OpCodes.Stsfld, mockedMemberInfo.ActualCallsIdsField));
            processor.Append(processor.Create(OpCodes.Ret));
        }

        private ILProcessor GetProcessor()
        {
            var ctor = _typeInfo.SearchMethod(CONSTRUCTOR_METHOD_NAME);
            if (ctor == null)
            {
                ctor = AddStaticCtor();
                return ctor.Body.GetILProcessor();
            }
            else
            {
                var processor = ctor.Body.GetILProcessor();
                processor.Remove(processor.Body.Instructions.Last()); //remove Ret
                return processor;
            }
        }

        private MethodDefinition AddStaticCtor()
        {
            var ctor = new MethodDefinition(CONSTRUCTOR_METHOD_NAME,
                MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName |
                MethodAttributes.RTSpecialName,
                _typeInfo.Import(typeof(void)));

            _typeInfo.AddMethod(ctor);
            return ctor;
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
                        var injector = new MockedMemberInjector(_typeInfo, currentMethod.Body.GetILProcessor(), mockedMemberInfo);
                        injector.Process(instruction);
                    }
                    else if (methodReference.IsDefinition)
                    {
                        ReplaceInstructions(methodReference.Resolve(), mockedMemberInfo);
                    }
                }
            }
        }

        private bool AreSameInCurrentType(MethodReference methodReference, MethodInfo methodToReplace)
            => methodToReplace.DeclaringType == _typeInfo.SourceType
                        && methodReference.DeclaringType.FullName == _typeInfo.FullTypeName
                        && methodReference.Name == methodToReplace.Name;

        private bool AreSameInExternalType(MethodReference methodReference, MethodInfo methodToReplace)
            => methodReference.DeclaringType.FullName == methodToReplace.DeclaringType.FullName
                        && methodReference.Name == methodToReplace.Name;
    }
}
