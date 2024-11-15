using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

using LINQPad.Extensibility.DataContext;

namespace JsonDataContextDriver
{
	public partial class ConnectionDialog : Window
	{
		IConnectionInfo _cxInfo;

		public ConnectionDialog (IConnectionInfo cxInfo)
		{
            ic(cxInfo);

        }
        internal void ic (IConnectionInfo cxInfo)
        {
            _cxInfo = cxInfo;

            // ConnectionProperties is your view-model.
            DataContext = new ConnectionProperties(cxInfo);

            InitializeComponent();
        }

        void btnOK_Click (object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}		
	}
}