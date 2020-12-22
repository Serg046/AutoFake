using System.Collections.Generic;
using Mono.Cecil.Rocks;
using MethodBody = Mono.Cecil.Cil.MethodBody;

namespace AutoFake
{
	internal class EmitterPool : IEmitterPool
	{
		private readonly Dictionary<MethodBody, IEmitter> _emitters = new Dictionary<MethodBody, IEmitter>();
		
		public IEmitter GetEmitter(MethodBody methodBody)
		{
			if (!_emitters.TryGetValue(methodBody, out var emitter))
			{
				methodBody.SimplifyMacros();
				emitter = new Emitter(methodBody);
				_emitters[methodBody] = emitter;
			}

			return emitter;
		}

		public void Dispose()
		{
			foreach (var methodBody in _emitters.Keys)
			{
				methodBody.OptimizeMacros();
			}
		}
	}
}
