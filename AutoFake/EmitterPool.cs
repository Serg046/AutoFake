﻿using System;
using System.Collections.Generic;
using Mono.Cecil.Rocks;
using MethodBody = Mono.Cecil.Cil.MethodBody;

namespace AutoFake
{
	internal class EmitterPool : IEmitterPool
	{
		private readonly Func<MethodBody, Emitter> _createEmitter;
		private readonly Dictionary<MethodBody, IEmitter> _emitters = new();

		public EmitterPool(Func<MethodBody, Emitter> createEmitter)
		{
			_createEmitter = createEmitter;
		}
		
		public IEmitter GetEmitter(MethodBody methodBody)
		{
			if (!_emitters.TryGetValue(methodBody, out var emitter))
			{
				methodBody.SimplifyMacros();
				emitter = _createEmitter(methodBody);
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
