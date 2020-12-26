using System.IO;
using System.Reflection;

namespace AutoFake
{
	internal class AssemblyHost
	{
		public Assembly Load(Stream stream)
		{
			stream.Position = 0;
#if NETCOREAPP3_0
			return _host.LoadFromStream(stream);
#else
			var length = (int)stream.Length;
			var bytes = new byte[length];
			stream.Read(bytes, 0, length);
			return Assembly.Load(bytes);
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
