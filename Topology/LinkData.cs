using Northwoods.GoXam.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Topology
{
	class LinkData : GraphLinksModelLinkData<String, String>
	{
		public Object obj;
		public LinkData(string FromID, string ToID, Object obj)
		{
			this.From = FromID;
			this.To = ToID;
			this.obj = obj;
			
			if(obj is Branch)
			{
				Branch branch = obj as Branch;
				if(branch.Ratio == 0)
				{
					Category = "BranchTemplate";
				}
				else
				{
					Category = "InterfaceTemplate";
				}
			}
			else
			{
				Category = "NormalTemplate";
			}
		}
	}
}
