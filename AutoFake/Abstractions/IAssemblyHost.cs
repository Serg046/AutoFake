using System.IO;
using System.Reflection;

namespace AutoFake.Abstractions;

public interface IAssemblyHost
{
	Assembly Load(MemoryStream asmStream, MemoryStream symbolsStream);
}
