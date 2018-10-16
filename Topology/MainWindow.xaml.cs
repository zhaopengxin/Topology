using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Northwoods.GoXam.Model;
using System.Data.SQLite;
using Northwoods.GoXam;
using System.Collections;
using System.Data;

namespace Topology
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
		private GraphLinksModel<NodeData, string, string, LinkData> model;  //GoDiagram topology model

		private SQLiteConnection conn;	//Database connection, open at the beginning at window load, close at window close

		//All of the collections of component
		public Dictionary<string, Bus> busDict = new Dictionary<string, Bus>();
		public List<Bus> buses = new List<Bus>();
		public List<Branch> branches = new List<Branch>();
		public List<Generator> generators = new List<Generator>();
		public List<Load> loads = new List<Load>();

		private ObservableCollection<NodeData> nodeList;	//Node data source for this diagram
		private ObservableCollection<LinkData> linkList;	//Link data source for this diagram
		private HashSet<string> busSet;	//Just avoid duplicated buese
		private HashSet<string> branchSet;	//Just avoid duplicated branches
		private List<Bus> expandedBuses = new List<Bus>();	//Helper list for record which bus has been expanded

		public string selectedBusID;	//this is the root bus, or selected bus

		#region Windows Related functions
		public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
			model = new GraphLinksModel<NodeData, string, string, LinkData>();

			#region Get Bus, Branch From Database
			string path = @"E:\Coop\bugs\newProFor10.2.1\WECC_2028ADS_database";
			string connection = string.Format("data source={0}\\GridView.db; PRAGMA synchronous=off", path);
			conn = new SQLiteConnection(connection);
			conn.Open();

			SQLiteCommand cmd = conn.CreateCommand();
			cmd.CommandText = string.Format("Select BusID, Name From Bus");
			SQLiteDataReader reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				string busID = reader["BusID"].ToString();
				string busName = reader["Name"].ToString();
				Bus bus = new Bus(busID, busName);
				busDict.Add(busID, bus);
				buses.Add(bus);
			}
			reader.Close();

			cmd.CommandText = string.Format("Select BranchID, FromBus, ToBus, CKT, Ratio From Branch");
			reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				string branchID = reader["BranchID"].ToString();
				string fromBusID = reader["FromBus"].ToString();
				string toBusID = reader["ToBus"].ToString();
				string ckt = reader["CKT"].ToString();
				double ratio = double.Parse(reader["Ratio"].ToString());
				Bus fromBus = busDict[fromBusID];
				Bus toBus = busDict[toBusID];

				Branch branch = new Branch(fromBusID, toBusID, ckt);
				branch.BranchID = branchID;
				fromBus.branchList.Add(branch);
				toBus.branchList.Add(branch);
				branch.FromBus = fromBus;
				branch.ToBus = toBus;
				branch.Ratio = ratio;        
				branches.Add(branch);
			}
			reader.Close();
			#endregion

			#region Get Generator From Database
			cmd.CommandText = string.Format("Select GeneratorKey, GeneratorName, BusID, GeneratorID From Generator");
			reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				string GenKey = reader["GeneratorKey"].ToString();
				string GeneratorName = reader["GeneratorName"].ToString();
				string BusID = reader["BusID"].ToString();
				string GeneratorID = reader["GeneratorID"].ToString();
				if (!busDict.ContainsKey(BusID)) continue;

				Generator generator = new Generator(GenKey, GeneratorName, BusID, GeneratorID);
				
				Bus corBus = busDict[BusID];
				generator.bus = corBus;
				corBus.generatorList.Add(generator);

				generators.Add(generator);
			}
			reader.Close();
			#endregion

			#region Get Load From DataBase
			cmd.CommandText = string.Format("Select BusID, LoadID From PSSELoad");
			reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				string BusID = reader["BusID"].ToString();
				string LoadID = reader["LoadID"].ToString();
				Load load = new Load(BusID, LoadID);

				Bus corBus = busDict[BusID];
				load.bus = corBus;
				corBus.loadList.Add(load);

				loads.Add(load);
			}
			reader.Close();
			#endregion

			GetBusList();

		}

		/// <summary>
		/// Update selection bus list at the top of form
		/// </summary>
		private void GetBusList()
		{
			SQLiteCommand cmd = conn.CreateCommand();
			cmd.CommandText = string.Format("Select Bus.BusID AS BusID, Bus.Name AS Name, " +
				"Bus.BaseKV AS BaseKV, LoadArea.LoadAreaName AS LoadAreaName, Region.RegionName AS RegionName " +
				"From (Bus Left Join LoadArea On Bus.LoadAreaID=LoadArea.LoadAreaID) " +
				"Left Join Region On LoadArea.RegionID=Region.RegionID");
			SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
			DataTable dt = new DataTable();
			adapter.Fill(dt);
			lstBus.ItemsSource = dt;
		}
		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			conn.Close();
		}
		#endregion

		#region User functions
		/// <summary>
		/// Major update funtion for refreshing topology
		/// </summary>
		public void Update()
		{
			#region Clean all the flag of expanded bus
			foreach(Bus bus in expandedBuses)
			{
				bus.EverExpanded = false;
			}
			expandedBuses.Clear();
			#endregion

			Bus selectedBus = busDict[selectedBusID];

			nodeList = new ObservableCollection<NodeData> { new NodeData(selectedBus.Key, selectedBus, "All") };
			linkList = new ObservableCollection<LinkData>();

			int showLayer = 2;
			#region (BFS) Add all buses and branch with layers number equals showLayer
			busSet = new HashSet<string>() { selectedBus.Key };
			branchSet = new HashSet<string>();
			Queue<Bus> queue = new Queue<Bus>();
			int queueSize = 1;
			queue.Enqueue(selectedBus);
			while(queue.Count > 0 && showLayer > 0)
			{
				Bus busNode = queue.Dequeue();
				queueSize--;
				foreach (Branch branch in busNode.branchList)
				{
					//To Bus is the other side bus
					if (branch.FromBus != busNode)
					{
						if (!busSet.Contains(branch.FromBus.Key))
						{
							busSet.Add(branch.FromBus.Key);
							if (showLayer == 1)
							{
								nodeList.Add(new NodeData(branch.FromBus.Key, branch.FromBus, "Right", true));
							}
							else
							{
								nodeList.Add(new NodeData(branch.FromBus.Key, branch.FromBus, "Right", false));
							}
							
							queue.Enqueue(branch.FromBus);
						}
						if (!branchSet.Contains(branch.BranchID))
						{
							branchSet.Add(branch.BranchID);
							linkList.Add(new LinkData(branch.To, branch.From, branch));
						}
						
					}

					//From Bus is the other side bus
					if (branch.ToBus != busNode)
					{
						if (!busSet.Contains(branch.ToBus.Key))
						{
							busSet.Add(branch.ToBus.Key);
							if (showLayer == 1)
							{
								nodeList.Add(new NodeData(branch.ToBus.Key, branch.ToBus, "Right", true));
							}
							else
							{
								nodeList.Add(new NodeData(branch.ToBus.Key, branch.ToBus, "Right", false));
							}
							
							queue.Enqueue(branch.ToBus);
						}
						if (!branchSet.Contains(branch.BranchID))
						{
							branchSet.Add(branch.BranchID);
							linkList.Add(new LinkData(branch.From, branch.To, branch));
						}
							
					}
				}
				if(queueSize == 0)
				{
					showLayer--;
					queueSize = queue.Count;
				}
			}
			#endregion

			#region Add Corresponding Generators
			foreach (Generator generator in selectedBus.generatorList)
			{
				nodeList.Add(new NodeData(generator.Key, generator, "Left"));

				linkList.Add(new LinkData(selectedBus.Key, generator.Key, null));
			}
			#endregion

			#region Add Corresponding Loads
			foreach (Load load in selectedBus.loadList)
			{
				string loadKey = string.Format("{0}_{1}", load.BusID, load.LoadID);
				nodeList.Add(new NodeData(loadKey, load, "Left"));
				linkList.Add(new LinkData(selectedBus.Key, loadKey, null));
			}
			#endregion

			model.NodesSource = nodeList;
			model.LinksSource = linkList;
			model.Modifiable = true;
			myDiagram.Model = model;
		}

		/// <summary>
		/// Double click or Single click at node
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			Node node = Part.FindAncestor<Node>(sender as UIElement);
			if (node == null) return;
			NodeData nodeData = node.Data as NodeData;
			if (nodeData == null) return;

			if(nodeData.obj is Bus)
			{
				Bus bus = nodeData.obj as Bus;
				if (bus == null) return;

				if (DiagramPanel.IsDoubleClick(e))
				{
					e.Handled = true;
					MessageBoxResult rs = MessageBox.Show(string.Format("Go to bus:\nID: {0}\nName: '{1}'?", bus.Key, bus.Name), 
						"Confirm", MessageBoxButton.YesNo);
					if (rs != MessageBoxResult.Yes) return;

					selectedBusID = bus.Key;
					Update();
				}
				else
				{
					TxtInfo.Text = string.Format("Bus:\nID: {0} Name: '{1}'", bus.Key, bus.Name);
				}
			}
			else if(nodeData.obj is Generator)
			{
				Generator generator = nodeData.obj as Generator;
				if (generator == null) return;
				TxtInfo.Text = string.Format("Generator:\nKey: {0} Name: '{1}'", generator.Key, generator.Name);
			}
			else if (nodeData.obj is Load)
			{
				Load load = nodeData.obj as Load;
				if (load == null) return;
				TxtInfo.Text = string.Format("PSSE Load:\nBusID: {0} LoadID: '{1}'", load.BusID, load.LoadID);
			}


		}

		/// <summary>
		/// Single click at link
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void LinkPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			Link node = Part.FindAncestor<Link>(sender as UIElement);
			if (node == null) return;
			LinkData linkData = node.Data as LinkData;
			if (linkData == null) return;

			if(linkData.obj != null && linkData.obj is Branch)
			{
				Branch branch = linkData.obj as Branch;
				TxtInfo.Text = string.Format("Branch:\nFrom Bus ID: {0} To Bus ID: {1} CKT: '2'", 
					branch.FromBusID, branch.ToBusID, branch.CKT);
			}
			
		}

		/// <summary>
		/// Select different buses at bus list
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void lstBus_SelectionChanged(object sender, DevExpress.Xpf.Grid.GridSelectionChangedEventArgs e)
		{
			if (lstBus.SelectedItem == null) return;
			DataRow row = (lstBus.SelectedItem as DataRowView).Row;
			selectedBusID = row["BusID"].ToString();
			Update();
		}

		/// <summary>
		/// Click expand button at leaf buses
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void CollapseExpandButton_Click(object sender, RoutedEventArgs e)
		{
			Button button = (Button)sender;
			Node n = Part.FindAncestor<Node>(button);
			if (n != null)
			{
				Bus parentdata = (Bus)(n.Data as NodeData).obj;
				// always make changes within a transaction
				myDiagram.StartTransaction("CollapseExpand");
				// if needed, create the child data for this node
				if (!parentdata.EverExpanded)
				{
					expandedBuses.Add(parentdata);  //Add expanded bus to buffer so that when every time we refresh, reset this flag

					parentdata.EverExpanded = true;  // only create children once per node!
					int numchildren = CreateSubTree(parentdata);
					if (numchildren == 0)
					{  // now known no children: don't need Button!
						button.Visibility = Visibility.Collapsed;
					}
				}
				// toggle whether this node is expanded or collapsed
				n.IsExpandedTree = !n.IsExpandedTree;
				//if (n.IsExpandedTree)
				//	myDiagram.Panel.CenterPart(n);
				//else
				//	myDiagram.Panel.CenterPart(n.NodesInto.FirstOrDefault());
				myDiagram.CommitTransaction("CollapseExpand");
			}
		}
		
		/// <summary>
		/// Add children bus for those leaf buses
		/// </summary>
		/// <param name="busNode"></param>
		/// <returns></returns>
		private int CreateSubTree(Bus busNode)
		{
			int childrenNum = 0;
			
			foreach (Branch branch in busNode.branchList)
			{
				//To Bus is the other side bus
				if (branch.FromBus != busNode)
				{
					if (!busSet.Contains(branch.FromBus.Key))
					{
						childrenNum++;
						busSet.Add(branch.FromBus.Key);
						nodeList.Add(new NodeData(branch.FromBus.Key, branch.FromBus, "Right", true));
					}
					if (!branchSet.Contains(branch.BranchID))
					{
						branchSet.Add(branch.BranchID);
						linkList.Add(new LinkData(branch.To, branch.From, branch));
					}

				}

				//From Bus is the other side bus
				if (branch.ToBus != busNode)
				{
					if (!busSet.Contains(branch.ToBus.Key))
					{
						childrenNum++;
						busSet.Add(branch.ToBus.Key);
						nodeList.Add(new NodeData(branch.ToBus.Key, branch.ToBus, "Right", true));
					}
					if (!branchSet.Contains(branch.BranchID))
					{
						branchSet.Add(branch.BranchID);
						linkList.Add(new LinkData(branch.From, branch.To, branch));
					}

				}
			}
			return childrenNum;
		}
		#endregion
	}
}
