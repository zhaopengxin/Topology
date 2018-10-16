using Northwoods.GoXam.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Topology
{
	class NodeData : GraphLinksModelNodeData<string>
	{
		public Object obj;
		public string LayoutID { set; get; }
		public string Name { set; get; }

		public string Type { set; get; }

		public NodeData(string Key, Object obj, string LayoutID="Right", bool isExteneded=false)
		{
			this.Key = Key;
			this.obj = obj;
			this.LayoutID = LayoutID;
			if(obj is Bus)
			{
				Bus bus = obj as Bus;
				Name = bus.Name;
				Type = "Bus";
				Category = isExteneded ? "ExtBusTemplate" : "BusTemplate";
			}
			else if(obj is Generator)
			{
				Generator generator = obj as Generator;
				Name = generator.Name;
				Type = "Generator";
				Category = "GeneratorTemplate";
			}else if(obj is Load)
			{
				Load load = obj as Load;
				Name = string.Format("{0}_{1}", load.BusID, load.LoadID);
				Type = "Load";
				Category = "LoadTemplate";
			}
		
		}
	}
}
