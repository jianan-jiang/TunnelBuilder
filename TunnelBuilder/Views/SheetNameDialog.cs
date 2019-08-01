using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;
using Rhino.UI.Forms;
using Rhino;

namespace TunnelBuilder.Views
{
    class SheetNameGridItem
    {
        public string Text { get; set; }

        public bool Check { get; set; }

        public int ID { get; set; }
    }
    class SheetNameDialog:CommandDialog
    {
        string[] SheetNames;
        private GridView grid;
        public SheetNameDialog(RhinoDoc doc,string title, string[] sheetNames)
        {
            var collection = new ObservableCollection<SheetNameGridItem>();
            for(int i=0;i<sheetNames.Length;i++)
            {
                collection.Add(new SheetNameGridItem { Text = sheetNames[i], Check = false, ID = i +1});
            }

            grid = new GridView { DataStore = collection };
            grid.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<SheetNameGridItem, string>(r => r.Text) },
                HeaderText = "Sheet Name"
            });

            grid.AllowMultipleSelection = false;

            Title = title;
            Resizable = false;
            Maximizable = false;
            Minimizable = false;
            ShowInTaskbar = false;
            WindowStyle = WindowStyle.Default;

            var layout = new DynamicLayout { DefaultSpacing = new Size(5, 5), Padding = Padding = new Padding(10) };
            layout.Add(grid, yscale: true);

            Content = layout;
        }

        public int selectedSheetID
        {
            get
            {
                var selectedItem = grid.SelectedItem as SheetNameGridItem;
                return selectedItem.ID;
            }
        }
    }
}
