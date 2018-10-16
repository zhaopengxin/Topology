using Northwoods.GoXam.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Topology
{
	public class Branch:GraphLinksModelLinkData<String, String>
	{
		public string BranchID { set; get; }
		public string FromBusID { set; get; }
		public Bus FromBus;

		public string CKT { set; get; }

		public string ToBusID { set; get; }
		public Bus ToBus;

		public double Ratio { set; get; }

		public Branch(string fromBus, string toBus, string ckt)
		{
			this.FromBusID = fromBus;
			this.From = fromBus;

			this.ToBusID = toBus;
			this.To = toBus;

			this.CKT = ckt;
		}
	}
}
