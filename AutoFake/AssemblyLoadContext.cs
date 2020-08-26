using System;
using System.IO;
using System.Reflection;

namespace AutoFake
{
	internal class AssemblyLoadContext
#if NETCOREAPP3_0
		: System.Runtime.Loader.AssemblyLoadContext, IDisposable
#else
		: IDisposable
#endif
	{
#if NETCOREAPP3_0
		public AssemblyLoadContext() : base(isCollectible: true)
		{
		}
#endif

		public Assembly Load(MemoryStream stream)
		{
#if NETCOREAPP3_0
			stream.Position = 0;
			return LoadFromStream(stream);
#else
			return Assembly.Load(stream.ToArray());
#endif
		}

		public void Dispose()
		{
#if NETCOREAPP3_0
			Unload();
#endif
		}
	}
}
