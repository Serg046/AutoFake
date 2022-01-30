using System.IO;
using System.Reflection;

namespace AutoFake.Abstractions
{
	internal interface IAssemblyHost
	{
		Assembly Load(MemoryStream asmStream, MemoryStream symbolsStream);
	}
}