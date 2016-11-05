using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AutoFake.Exceptions;
using AutoFake.Setup;
using GuardExtensions;

namespace AutoFake
{
    internal class FakeGenerator
    {
        private readonly TypeInfo _typeInfo;
        private readonly MockerFactory _mockerFactory;

        public FakeGenerator(TypeInfo typeInfo, MockerFactory mockerFactory)
        {
            Guard.AreNotNull(typeInfo, mockerFactory);
            _typeInfo = typeInfo;
            _mockerFactory = mockerFactory;
        }

        public void Save(string fileName)
        {
            Guard.IsNotNull(fileName);
            using (var fileStream = File.Create(fileName))
            {
                _typeInfo.WriteAssembly(fileStream);
            }
        }

        public GeneratedObject Generate(SetupCollection setups, MethodInfo executeFunc)
        {
            Guard.AreNotNull(setups, executeFunc);

            if (setups.Any(s => !s.IsVerification && !s.IsVoid && !s.IsReturnObjectSet))
                throw new SetupException("At least one non-void installed member does not have a return value.");

            _typeInfo.Load();

            var generatedObject = new GeneratedObject();
            generatedObject.MockedMembers = MockSetups(setups, executeFunc).ToList();

            using (var memoryStream = new MemoryStream())
            {
                _typeInfo.WriteAssembly(memoryStream);
                var assembly = Assembly.Load(memoryStream.ToArray());
                generatedObject.Type = assembly.GetType(_typeInfo.FullTypeName);
                generatedObject.Instance = IsStatic(_typeInfo.SourceType)
                    ? null
                    : _typeInfo.CreateInstance(generatedObject.Type);
                return generatedObject;
            }
        }

        private bool IsStatic(Type type) => type.IsAbstract && type.IsSealed;

        private IEnumerable<MockedMemberInfo> MockSetups(SetupCollection setups, MethodInfo executeFunc)
        {
            foreach (var setup in setups)
            {
                var mocker = _mockerFactory.CreateMocker(_typeInfo, setup);
                mocker.GenerateCallsCounter();

                if (!setup.IsVoid)
                {
                    mocker.GenerateRetValueField();
                }


                var methodInjector = _mockerFactory.CreateMethodInjector(mocker);
                var method = _typeInfo.Methods.Single(m => m.Name == executeFunc.Name);
                ReplaceInstructions(method, methodInjector);

                yield return mocker.MemberInfo;
            }
        }

        private void ReplaceInstructions(MethodDefinition currentMethod, IMethodInjector methodInjector)
        {
            MethodDefinition asyncMethod;
            if (methodInjector.IsAsyncMethod(currentMethod, out asyncMethod))
                ReplaceInstructions(asyncMethod, methodInjector);
            
            foreach (var instruction in currentMethod.Body.Instructions.ToList())
            {
                if (methodInjector.IsMethodInstruction(instruction))
                {
                    var method = (MethodReference)instruction.Operand;
                    
                    if (methodInjector.IsInstalledMethod(method))
                    {
                        var proc = currentMethod.Body.GetILProcessor();
                        methodInjector.Process(proc, instruction);
                    }
                    else if (IsClientSourceCode(method))
                    {
                        ReplaceInstructions(method.Resolve(), methodInjector);
                    }
                }
            }
        }

        private bool IsClientSourceCode(MethodReference methodReference) => methodReference.IsDefinition;
    }
}
