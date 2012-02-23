using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NMEAServer
{
	public static class Program
	{
		public static int Main(String[] args)
		{
			AsynchronousSocketListener.StartListening();
			return 0;
		}
	}
}
