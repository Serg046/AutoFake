using System.IO;
using System.Reflection;

namespace AutoFake
{
	internal interface IAssemblyHost
	{
		Assembly Load(MemoryStream asmStream, MemoryStream symbolsStream);
	}
}