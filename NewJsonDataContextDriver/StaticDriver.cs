using LINQPad;
using LINQPad.Extensibility.DataContext;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace JsonDataContextDriver
{
	public class StaticDriver : StaticDataContextDriver
	{
		static StaticDriver()
		{
			// Uncomment the following code to attach to Visual Studio's debugger when an exception is thrown:
			//AppDomain.CurrentDomain.FirstChanceException += (sender, args) =>
			//{
			//	if (args.Exception.StackTrace.Contains ("JsonDataContextDriver"))
			//		Debugger.Launch ();
			//};
		}
	
		public override string Name => "(Name for your driver)";

		public override string Author => "(Your name)";

		public override string GetConnectionDescription (IConnectionInfo cxInfo)
			=> "(Description for this connection)";

		public override bool ShowConnectionDialog (IConnectionInfo cxInfo, ConnectionDialogOptions dialogOptions)
			=> new ConnectionDialog (cxInfo).ShowDialog () == true;

		public override List<ExplorerItem> GetSchema (IConnectionInfo cxInfo, Type customType)
		{
			// TODO - implement
			return new ExplorerItem[0].ToList();
		}
		
#if NETCORE
		// Put stuff here that's just for LINQPad 6+ (.NET Core and .NET 5+).
#else
		// Put stuff here that's just for LINQPad 5 (.NET Framework)
#endif
	}
}