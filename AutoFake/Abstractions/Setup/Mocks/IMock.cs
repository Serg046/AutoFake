using System;
using Mono.Cecil;

namespace AutoFake.Abstractions.Setup.Mocks;

public interface IMock : IMockInjector
{
	void BeforeInjection(MethodDefinition method);
	void AfterInjection(IEmitter emitter);
	void Initialize(Type? type);
}
