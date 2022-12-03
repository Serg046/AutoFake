using System;
using Mono.Cecil.Cil;

namespace AutoFake.Abstractions;

public interface IEmitterPool : IDisposable
{
	IEmitter GetEmitter(MethodBody methodBody);
}
