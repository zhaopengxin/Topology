using Northwoods.GoXam.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Topology
{
	public class Generator : GraphLinksModelNodeData<string>
	{
		public string Name { set; get; }
		public string GeneratorKey { set; get; }

		public string BusID { set; get; }

		public string GeneratorID { set; get; }

		public Bus bus;

		public string LayoutID { set; get; }

		public Generator(string Genkey, string name, string BusID, string generatorID)
		{
			this.GeneratorKey = Genkey;
			this.Key = Genkey;

			this.Name = name;
			this.BusID = BusID;
			this.GeneratorID = generatorID;
			LayoutID = "Left";
		}
	}
}
