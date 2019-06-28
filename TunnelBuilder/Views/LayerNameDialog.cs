using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;
using Rhino.UI.Forms;
using Rhino;

namespace TunnelBuilder.Views
{
    class LayerTreeGridItem:List<LayerTreeGridItem>,ITreeGridItem<LayerTreeGridItem>
    {
        public string layerName;
        public string layerFullPath;
        public string Text { get { return layerName; } }
        public bool Expanded { get; set; }
        public bool Expandable { get { return Count > 0; } }
        public ITreeGridItem Parent { get; set; }

        public LayerTreeGridItem(Rhino.DocObjects.Layer l)
        {
            layerName = l.Name;
            layerFullPath = l.FullPath;
            Rhino.DocObjects.Layer[] childrenLayers = l.GetChildren();
            if (childrenLayers != null)
            {
                for (int i = 0; i < childrenLayers.Length; i++)
                {
                    var temp = new LayerTreeGridItem(childrenLayers[i]);
                    temp.Parent = this;
                    this.Add(temp);
                }
            }
        }
    }
    class LayerNameDialog:CommandDialog
    {
        private TreeGridView layerNameTreeView;
        public LayerNameDialog(RhinoDoc doc,string boltLayerFullPath="")
        {
            TreeGridItem layerNameRootItem = new TreeGridItem();
            
            foreach (var l in doc.Layers)
            {
                if(l.ParentLayerId == Guid.Empty)
                {
                    LayerTreeGridItem layerTreeGridItem = new LayerTreeGridItem(l);
                    layerNameRootItem.Children.Add(layerTreeGridItem);
                }
            }
            layerNameTreeView = new TreeGridView();
            layerNameTreeView.ShowHeader = false;
            layerNameTreeView.Columns.Add(new GridColumn { DataCell = new TextBoxCell{ Binding=new DelegateBinding<LayerTreeGridItem,string>(r=>r.layerName)}, HeaderText = "Layer", AutoSize = true, Editable = false });
            layerNameTreeView.DataStore = layerNameRootItem;
            layerNameTreeView.AllowMultipleSelection = false;
            
            Title = "Select Root Bolt Layer";
            Resizable = false;
            Maximizable = false;
            Minimizable = false;
            ShowInTaskbar = false;
            WindowStyle = WindowStyle.Default;

            var layout = new DynamicLayout { DefaultSpacing = new Size(5, 5), Padding = Padding = new Padding(10) };
            layout.Add(layerNameTreeView, yscale: true);

            Content = layout;
        }

        public string selectedLayerName
        {
            get {
                var selectedItem = layerNameTreeView.SelectedItem as LayerTreeGridItem;
                if (selectedItem == null){
                    return "";
                }
                return selectedItem.layerName;
            }
        }

        public string selectedLayerFullPath
        {
            get
            {
                var selectedItem = layerNameTreeView.SelectedItem as LayerTreeGridItem;
                if (selectedItem == null)
                {
                    return "";
                }
                return selectedItem.layerFullPath;
            }
        }
    }
}
