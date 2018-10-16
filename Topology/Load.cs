using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Topology
{
    public class Load
    {
		public string BusID { set; get; }
		public string LoadID { set; get; }

		public Bus bus;

		public Load(string BusID, string LoadID)
		{
			this.BusID = BusID;
			this.LoadID = LoadID;
		}
    }
}
