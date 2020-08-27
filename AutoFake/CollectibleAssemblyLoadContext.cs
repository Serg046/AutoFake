#if NETCOREAPP3_0
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace AutoFake
{
	internal class CollectibleAssemblyLoadContext : AssemblyLoadContext
	{
		public CollectibleAssemblyLoadContext() : base(isCollectible: true)
		{
		}

		public static Assembly Load(MemoryStream stream)
		{
			stream.Position = 0;
			return new CollectibleAssemblyLoadContext().LoadFromStream(stream);
		}
	}
}
#endif