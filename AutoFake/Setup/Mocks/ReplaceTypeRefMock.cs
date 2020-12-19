using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup.Mocks
{
    internal class ReplaceTypeRefMock : IMock
    {
        private readonly Type _type;
        private readonly OpCode _opCode;
        private readonly Dictionary<string, Instruction> _instructions;

        public ReplaceTypeRefMock(ITypeInfo typeInfo, Type type)
        {
            _type = type;
            if (type.IsValueType)
            {
                _opCode = OpCodes.Initobj;
                var typeRef = typeInfo.ImportReference(type);
                _instructions = new Dictionary<string, Instruction>
                {
                    {typeRef.ToString(), Instruction.Create(_opCode, typeRef)}
                };
            }
            else
            {
                _opCode = OpCodes.Newobj;
                _instructions = type.GetConstructors()
                    .Select(typeInfo.ImportReference)
                    .ToDictionary(ctor => ctor.ToString(), ctor => Instruction.Create(_opCode, ctor));
            }

            _opCode = type.IsValueType ? OpCodes.Initobj : OpCodes.Newobj;
        }

        public bool IsSourceInstruction(MethodDefinition method, Instruction instruction)
            => instruction != null && instruction.OpCode == _opCode && instruction.Operand != null &&
               _instructions.ContainsKey(instruction.Operand.ToString());

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

        public override int GetHashCode() => _type.GetHashCode();

        public override bool Equals(object obj) => obj is ReplaceTypeRefMock mock && mock._type == _type;
    }
}
