using System.IO;
using System.Reflection;

namespace AutoFake
{
	internal class AssemblyHost
	{
		public Assembly Load(MemoryStream stream)
		{
#if NETCOREAPP3_0
			stream.Position = 0;
			return _host.LoadFromStream(stream);
#else
	        return Assembly.Load(stream.ToArray());
#endif
		}

#if NETCOREAPP3_0
		private readonly CollectibleAssemblyLoadContext _host = new CollectibleAssemblyLoadContext();

		private class CollectibleAssemblyLoadContext : System.Runtime.Loader.AssemblyLoadContext
		{
			public CollectibleAssemblyLoadContext() : base(isCollectible: true)
			{
			}
		}
#endif
	}
}
