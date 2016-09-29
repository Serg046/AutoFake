using Mono.Cecil;
using Mono.Cecil.Cil;
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
        private const string ASYNC_STATE_MACHINE_ATTRIBUTE = "AsyncStateMachineAttribute";

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
                    : Activator.CreateInstance(generatedObject.Type, _typeInfo.ContructorArguments);
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

                var method = _typeInfo.Methods.Single(m => m.Name == executeFunc.Name);
                ReplaceInstructions(method, mocker);

                yield return mocker.MemberInfo;
            }
        }

        private void ReplaceInstructions(MethodDefinition currentMethod, IMocker mocker)
        {
            ProcessAsyncMethod(currentMethod, mocker);

            foreach (var instruction in currentMethod.Body.Instructions.ToList())
            {
                if (instruction.OpCode.OperandType == OperandType.InlineMethod)
                {
                    var method = (MethodReference)instruction.Operand;
                    
                    var methodInjector = _mockerFactory.CreateMethodInjector(mocker);
                    if (methodInjector.IsInstalledMethod(method))
                    {
                        var proc = currentMethod.Body.GetILProcessor();
                        methodInjector.Process(proc, instruction);
                    }
                    else if (IsClientSourceCode(method))
                    {
                        ReplaceInstructions(method.Resolve(), mocker);
                    }
                }
            }
        }

        private void ProcessAsyncMethod(MethodDefinition currentMethod, IMocker mocker)
        {
            //for .net 4, it is available in .net 4.5
            dynamic asyncAttribute = currentMethod.CustomAttributes
                .SingleOrDefault(a => a.AttributeType.Name == ASYNC_STATE_MACHINE_ATTRIBUTE);
            if (asyncAttribute != null)
            {
                if (asyncAttribute.ConstructorArguments.Count != 1)
                    throw new FakeGeneretingException("Unexpected exception. AsyncStateMachine have several arguments or 0.");
                TypeReference generatedAsyncType = asyncAttribute.ConstructorArguments[0].Value;
                var asyncMethod = generatedAsyncType.Resolve().Methods.Single(m => m.Name == "MoveNext");
                ReplaceInstructions(asyncMethod, mocker);
            }
        }

        private bool IsClientSourceCode(MethodReference methodReference) => methodReference.IsDefinition;
    }
}
