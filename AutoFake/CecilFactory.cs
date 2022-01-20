using System;
using DryIoc;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake
{
	internal class CecilFactory : ICecilFactory
	{
		private readonly IContainer _serviceLocator;

		public CecilFactory(IContainer serviceLocator) => _serviceLocator = serviceLocator;

		public VariableDefinition CreateVariable(TypeReference variableType)
		{
			return _serviceLocator.Resolve<Func<TypeReference, VariableDefinition>>().Invoke(variableType);
		}
	}
}
