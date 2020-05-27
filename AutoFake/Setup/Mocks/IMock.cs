using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup.Mocks
{
    internal interface IMock
    {
        bool IsSourceInstruction(MethodDefinition method, Instruction instruction);
        void BeforeInjection(MethodDefinition method);
        void ProcessInstruction(Instruction instruction);
        void Inject(IEmitter emitter, Instruction instruction);
        void AfterInjection(IEmitter emitter);
        IList<object> Initialize(Type type);
    }
}