using AutoFake.Abstractions.Setup.Mocks;
using Mono.Cecil;
using System.Collections.Generic;

namespace AutoFake.Abstractions;

public interface ITestMethod
{
	IReadOnlyList<MethodDefinition> Rewrite(MethodDefinition originalMethod, IFakeOptions options, IEnumerable<IMockInjector> mocks, IEnumerable<IGenericArgument> genericArgs);
}
