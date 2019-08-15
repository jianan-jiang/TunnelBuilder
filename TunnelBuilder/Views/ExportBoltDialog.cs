using System;
using Eto.Drawing;
using Eto.Forms;
using Rhino.UI.Forms;
using Rhino;
using System.ComponentModel;

namespace TunnelBuilder.Views
{
    class LayerNameTextBox:TextBox
    {
        private RhinoDoc layer_doc;
        public string FullPath;
        public LayerNameTextBox(RhinoDoc doc)
        {
            ReadOnly = true;
            layer_doc = doc;
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            var layer_dialog = new Views.LayerNameDialog(layer_doc);
            var layer_dialog_rc = layer_dialog.ShowModal();
            Text = layer_dialog.selectedLayerName;
            FullPath = layer_dialog.selectedLayerFullPath;
        }
    }
    class ExportBoltDialog:CommandDialog
    {
        private LayerNameTextBox boltLayerNameTextBox;
        private NumericStepper segmentNumberStepper;
        private NumericStepper boltStartIDStepper;
        private NumericStepper youngStepper;
        private NumericStepper groutCohesionStepper;
        private NumericStepper groutStiffnessStepper;
        private NumericStepper groutPerimeterStepper;
        private NumericStepper crossSectionAreaStepper;
        private NumericStepper yieldCompressionStepper;
        private NumericStepper yieldTensionStepper;
        private NumericStepper preTensionStepper;
        private NumericStepper longitudinalSpacingStepper;
        private ExportEnvironment exportEnvironment;
        private RhinoDoc doc;

        public ExportBoltDialog(ExportEnvironment eE, RhinoDoc d)
        {
            exportEnvironment = eE;
            doc = d;

            Padding = new Padding(10);
            Title = "Bolts Parameters";
            Resizable = false;
            Maximizable = false;
            Minimizable = false;
            ShowInTaskbar = false;
            WindowStyle = WindowStyle.Default;

            boltLayerNameTextBox = new LayerNameTextBox(doc);

            segmentNumberStepper = new NumericStepper();
            segmentNumberStepper.DecimalPlaces = 0;
            segmentNumberStepper.Increment = 1;
            segmentNumberStepper.MinValue = 0;
            segmentNumberStepper.Value = 10;

            boltStartIDStepper = new NumericStepper();
            boltStartIDStepper.DecimalPlaces = 0;
            boltStartIDStepper.Increment = 1;
            boltStartIDStepper.MinValue = 1;
            boltStartIDStepper.Value = 1;

            preTensionStepper = new NumericStepper();
            preTensionStepper.DecimalPlaces = 3;
            preTensionStepper.Increment = 0.01;
            preTensionStepper.MinValue = 0;
            preTensionStepper.Value = 0.05;

            youngStepper = new NumericStepper();
            youngStepper.DecimalPlaces = 0;
            youngStepper.Increment = 1000;
            youngStepper.MinValue = 0;
            youngStepper.Value = 210000;

            groutCohesionStepper = new NumericStepper();
            groutCohesionStepper.DecimalPlaces = 3;
            groutCohesionStepper.MinValue = 0;
            groutCohesionStepper.Value = 0.136;

            groutStiffnessStepper = new NumericStepper();
            groutStiffnessStepper.DecimalPlaces = 1;
            groutStiffnessStepper.MinValue = 0;
            groutStiffnessStepper.Value = 200;

            groutPerimeterStepper = new NumericStepper();
            groutPerimeterStepper.DecimalPlaces = 3;
            groutPerimeterStepper.MinValue = 0;
            groutPerimeterStepper.Value = 0.141;

            crossSectionAreaStepper = new NumericStepper();
            crossSectionAreaStepper.DecimalPlaces = 6;
            crossSectionAreaStepper.MinValue = 0;
            crossSectionAreaStepper.Value = 0.00037;

            yieldTensionStepper = new NumericStepper();
            yieldTensionStepper.DecimalPlaces = 3;
            yieldTensionStepper.MinValue = 0;
            yieldTensionStepper.Value = 0.31;

            yieldCompressionStepper = new NumericStepper();
            yieldCompressionStepper.DecimalPlaces = 3;
            yieldCompressionStepper.Value = 0.31;

            longitudinalSpacingStepper = new NumericStepper();
            longitudinalSpacingStepper.DecimalPlaces = 2;
            longitudinalSpacingStepper.MinValue = 0;
            longitudinalSpacingStepper.Value = 1.0;

            Content = exportBoltDialogLayout();
            
        }

        private TableLayout exportBoltDialogLayout()
        {
            TableLayout layout = new TableLayout
            {
                Spacing = new Size(5, 5),
                Padding = new Padding(10, 10, 10, 10),
                Rows =
                {
                    new TableRow(
                        new TableCell(new Label {Text="Bolt Layer Name" },true),
                        new TableCell(boltLayerNameTextBox,true)
                        ),
                    new TableRow(
                        new TableCell(new Label {Text="Segment Number" },true),
                        new TableCell(segmentNumberStepper,true)
                        ),
                    new TableRow(
                        new TableCell(new Label {Text="Bolt Start ID" },true),
                        new TableCell(boltStartIDStepper,true)
                        ),
                    new TableRow(
                        new TableCell(new Label {Text="Pren Tension" },true),
                        new TableCell(preTensionStepper,true)
                        ),
                    new TableRow(
                        new TableCell(new Label {Text="Young's Modulus" },true),
                        new TableCell(youngStepper,true)
                        ),
                    new TableRow(
                        new TableCell(new Label {Text="Grout Cohesion" },true),
                        new TableCell(groutCohesionStepper,true)
                        ),
                    new TableRow(
                        new TableCell(new Label {Text="Grout Stiffness" },true),
                        new TableCell(groutStiffnessStepper,true)
                        ),
                    new TableRow(
                        new TableCell(new Label {Text="Grout Perimeter" },true),
                        new TableCell(groutPerimeterStepper,true)
                        ),
                    new TableRow(
                        new TableCell(new Label {Text="Cross Section Area" },true),
                        new TableCell(crossSectionAreaStepper,true)
                        ),
                    new TableRow(
                        new TableCell(new Label {Text="Yield Tension" },true),
                        new TableCell(yieldTensionStepper,true)
                        ),
                    new TableRow(
                        new TableCell(new Label {Text="Yield Compression" },true),
                        new TableCell(yieldCompressionStepper,true)
                        ),
                }
            };

            if (exportEnvironment == ExportEnvironment.UDEC)
            {

                layout.Rows.Add(new TableRow(new TableCell(new Label { Text = "Longitudinal Spacing" }, true), new TableCell(longitudinalSpacingStepper, true)));
            }
            return layout;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if(Result==Rhino.Commands.Result.Success)
            {
                if (validateInputs() == false)
                {
                    e.Cancel = true;
                }
            }
            
        }

        private bool validateInputs()
        {
            if(this.doc.Layers.FindByFullPath(this.boltLayerFullPath,-1)==-1)
            {
                MessageBox.Show("Please select a valid bolt root layer", "Input Error", MessageBoxType.Error);
                return false;
            }
            return true;
        }

        public string boltLayerName
        {
            get { return boltLayerNameTextBox.Text; }
        }

        public string boltLayerFullPath
        {
            get { return boltLayerNameTextBox.FullPath; }
        }

        public int boltSegment
        {
            get { return Convert.ToInt32(segmentNumberStepper.Value); }
        }
        public int boltStartId
        {
            get { return Convert.ToInt32(boltStartIDStepper.Value); }
        }
        public double preTension
        {
            get { return preTensionStepper.Value; }
        }
        public double young
        {
            get { return youngStepper.Value; }
        }
        public double groutCohesion
        {
            get { return groutCohesionStepper.Value; }
        }
        public double groutStiffness
        {
            get { return groutStiffnessStepper.Value; }
        }
        public double groutPerimeter
        {
            get { return groutPerimeterStepper.Value; }
        }
        public double crossSectionArea
        {
            get { return crossSectionAreaStepper.Value; }
        }
        public double yieldTension
        {
            get { return yieldTensionStepper.Value; }
        }
        public double yieldCompression
        {
            get { return yieldCompressionStepper.Value; }
        }

        public double longitudinalSpacing
        {
            get { return longitudinalSpacingStepper.Value; }
        }
    }
}
