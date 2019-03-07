using System;
using Eto.Drawing;
using Eto.Forms;
using Rhino.UI.Forms;

namespace TunnelBuilder.Views
{
    class ExportBoltDialog:CommandDialog
    {
        private TextBox boltLayerNameTextBox;
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

        public ExportBoltDialog()
        {
            Padding = new Padding(10);
            Title = "Bolts Parameters";
            Resizable = false;
            Maximizable = false;
            Minimizable = false;
            ShowInTaskbar = false;
            WindowStyle = WindowStyle.Default;

            boltLayerNameTextBox = new TextBox();

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

            Content = new TableLayout
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
                        new TableCell(new Label {Text="Grout Stifness" },true),
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
        }

        public string boltLayerName
        {
            get { return boltLayerNameTextBox.Text; }
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
    }
}
