using System;
using Mono.Cecil.Cil;

namespace AutoFake
{
	internal interface IEmitterPool : IDisposable
	{
		IEmitter GetEmitter(MethodBody methodBody);
	}
}