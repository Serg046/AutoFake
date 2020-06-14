using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup.Mocks
{
    internal class ReplaceTypeRefMock : IMock
    {
        private readonly OpCode _opCode;
        private readonly Dictionary<string, Instruction> _instructions;

        public ReplaceTypeRefMock(ITypeInfo typeInfo, Type type)
        {
            if (type.IsValueType)
            {
                _opCode = OpCodes.Initobj;
                var typeRef = typeInfo.Module.ImportReference(type);
                _instructions = new Dictionary<string, Instruction>
                {
                    {typeRef.ToString(), Instruction.Create(_opCode, typeRef)}
                };
            }
            else
            {
                _opCode = OpCodes.Newobj;
                _instructions = type.GetConstructors()
                    .Select(ctor => typeInfo.Module.ImportReference(ctor))
                    .ToDictionary(ctor => ctor.ToString(), ctor => Instruction.Create(_opCode, ctor));
            }

            _opCode = type.IsValueType ? OpCodes.Initobj : OpCodes.Newobj;
        }

        public bool IsSourceInstruction(MethodDefinition method, Instruction instruction)
            => instruction.OpCode == _opCode && _instructions.ContainsKey(instruction?.Operand?.ToString());

        [ExcludeFromCodeCoverage]
        public void BeforeInjection(MethodDefinition method)
        {
        }

        public void Inject(IEmitter emitter, Instruction instruction)
        {
            var newInstruction = _instructions[instruction.Operand.ToString()];
            emitter.Replace(instruction, newInstruction);
        }

        [ExcludeFromCodeCoverage]
        public void AfterInjection(IEmitter emitter)
        {
        }

        public IList<object> Initialize(Type type) => new List<object>();
    }
}
