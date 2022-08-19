using System;
using Mono.Cecil.Cil;

namespace AutoFake.Abstractions
{
	internal interface IEmitterPool : IDisposable
	{
		IEmitter GetEmitter(MethodBody methodBody);
	}
}
