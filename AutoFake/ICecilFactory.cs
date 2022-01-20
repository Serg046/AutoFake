using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake
{
	internal interface ICecilFactory
	{
		VariableDefinition CreateVariable(TypeReference variableType);
	}
}