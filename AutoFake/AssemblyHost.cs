using System.IO;
using System.Reflection;
using AutoFake.Abstractions;

namespace AutoFake;

internal class AssemblyHost : IAssemblyHost
{
	public Assembly Load(MemoryStream asmStream, MemoryStream symbolsStream)
	{
#if NETCOREAPP3_0
		asmStream.Position = symbolsStream.Position = 0;
		return symbolsStream.Length == 0
			? _host.LoadFromStream(asmStream)
			: _host.LoadFromStream(asmStream, symbolsStream);
#else
		return symbolsStream.Length == 0
			? Assembly.Load(asmStream.ToArray())
			: Assembly.Load(asmStream.ToArray(), symbolsStream.ToArray());
#endif
	}

#if NETCOREAPP3_0
	private readonly CollectibleAssemblyLoadContext _host = new();

	private class CollectibleAssemblyLoadContext : System.Runtime.Loader.AssemblyLoadContext
	{
		public CollectibleAssemblyLoadContext() : base(isCollectible: true)
		{
		}
	}
#endif
}
