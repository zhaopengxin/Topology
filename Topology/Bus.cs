using Northwoods.GoXam.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Topology
{
	public class Bus: GraphLinksModelNodeData<string>
	{
		public string Name { set; get; }
		public string LayoutID { set; get; }
		public List<Branch> branchList = new List<Branch>();
		public List<Generator> generatorList = new List<Generator>();
		public List<Load> loadList = new List<Load>();

		public bool EverExpanded { get; set; }
		public Bus(string key)
		{
			this.Key = key;
			LayoutID = "Right";
		}
		public Bus(string key, string name)
		{
			this.Key = key;
			this.Name = name;
			LayoutID = "Right";
		}
	}
}
