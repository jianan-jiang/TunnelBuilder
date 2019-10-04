using System;
using Eto.Drawing;
using Eto.Forms;
using Rhino.UI.Forms;
using Rhino;

namespace TunnelBuilder.Views
{
    class GenerateTunnelProfilesDialog:CommandDialog
    {
        public bool includeHitch {
            get
            {
                if(includeHitchCheckBox.Checked!=null)
                {
                    return (bool)includeHitchCheckBox.Checked;
                }
                else
                {
                    return false;
                }
            }
            set
            {
                includeHitchCheckBox.Checked = (bool)value;
            }
        }

        public bool KeepTwoDProfiles
        {
            get
            {
                if(keepTwoDProfilesCheckBox.Checked!=null)
                {
                    return (bool)keepTwoDProfilesCheckBox.Checked;
                }
                else
                {
                    return false;
                }
            }
            set
            {
                keepTwoDProfilesCheckBox.Checked = value;
            }
        }

        public bool FlipProfiles
        {
            get
            {
                if (flipProfilesCheckBox.Checked != null)
                {
                    return (bool)flipProfilesCheckBox.Checked;
                }
                else
                {
                    return false;
                }
            }
            set
            {
                flipProfilesCheckBox.Checked = value;
            }
        }

        public double WallCLineELineOffset {
            get
            {
                return WallCLineELineOffsetNumericStepper.Value;
            }
            set
            {
                WallCLineELineOffsetNumericStepper.Value = value;
            }
        }

        public double CrownCLineELineOffset
        {
            get
            {
                return CrownCLineELineOffsetNumericStepper.Value;
            }
            set
            {
                CrownCLineELineOffsetNumericStepper.Value = value;
            }
        }
        public double HitchRadius {
            get
            {
                return HitchRadiusNumericStepper.Value;
            }
            set
            {
                HitchRadiusNumericStepper.Value = value;
            }
        }

        public double HitchOffset {
            get
            {
                return HitchOffsetNumericStepper.Value;
            }
            set
            {
                HitchOffsetNumericStepper.Value = value;
            }
        }

        private NumericStepper WallCLineELineOffsetNumericStepper;
        private NumericStepper CrownCLineELineOffsetNumericStepper;
        private NumericStepper HitchRadiusNumericStepper;
        private NumericStepper HitchOffsetNumericStepper;
        private CheckBox includeHitchCheckBox;
        private CheckBox keepTwoDProfilesCheckBox;
        private CheckBox flipProfilesCheckBox;

        private RhinoDoc doc;

        public GenerateTunnelProfilesDialog(RhinoDoc d)
        {
            doc = d;

            Padding = new Padding(10);
            Title = "E-Line Parameters";
            Resizable = false;
            Maximizable = false;
            Minimizable = false;
            ShowInTaskbar = false;
            WindowStyle = WindowStyle.Default;

            WallCLineELineOffsetNumericStepper = new NumericStepper();
            WallCLineELineOffsetNumericStepper.DecimalPlaces = 2;
            WallCLineELineOffsetNumericStepper.MinValue = 0;
            WallCLineELineOffsetNumericStepper.Increment = 0.05;

            CrownCLineELineOffsetNumericStepper = new NumericStepper();
            CrownCLineELineOffsetNumericStepper.DecimalPlaces = 2;
            CrownCLineELineOffsetNumericStepper.MinValue = 0;
            CrownCLineELineOffsetNumericStepper.Increment = 0.05;

            HitchRadiusNumericStepper = new NumericStepper();
            HitchRadiusNumericStepper.DecimalPlaces = 2;
            HitchRadiusNumericStepper.MinValue = 0;
            HitchRadiusNumericStepper.Increment = 0.05;

            HitchOffsetNumericStepper = new NumericStepper();
            HitchOffsetNumericStepper.DecimalPlaces = 2;
            HitchOffsetNumericStepper.MinValue = 0;
            HitchOffsetNumericStepper.Increment = 0.05;

            includeHitchCheckBox = new CheckBox();
            includeHitchCheckBox.ThreeState = false;
            includeHitchCheckBox.CheckedChanged += includeHitchCheckBoxChanged;

            keepTwoDProfilesCheckBox = new CheckBox();
            keepTwoDProfilesCheckBox.ThreeState = false;

            flipProfilesCheckBox = new CheckBox();
            flipProfilesCheckBox.ThreeState = false;

            Content = generateTunnelProfilesDialogLayout();
        }

        private void includeHitchCheckBoxChanged(object sender,EventArgs e)
        {
            if (includeHitchCheckBox.Checked==true)
            {
                HitchRadiusNumericStepper.ReadOnly = false;
                HitchOffsetNumericStepper.ReadOnly = false;
            }
            else
            {
                HitchRadiusNumericStepper.Value = 0;
                HitchOffsetNumericStepper.Value = 0;
                HitchRadiusNumericStepper.ReadOnly = true;
                HitchOffsetNumericStepper.ReadOnly = true;
            }
        }

        private TableLayout generateTunnelProfilesDialogLayout()
        {
            TableLayout layout = new TableLayout
            {
                Spacing = new Size(5, 5),
                Padding = new Padding(10, 10, 10, 10),
                Rows=
                {
                    new TableRow(
                        new TableCell(new Label {Text="Wall C-Line E-Line Offset" },true),
                        new TableCell(WallCLineELineOffsetNumericStepper,true)
                        ),
                    new TableRow(
                        new TableCell(new Label {Text="Crown C-Line E-Line Offset" },true),
                        new TableCell(CrownCLineELineOffsetNumericStepper,true)
                        ),
                    new TableRow(
                        new TableCell(new Label {Text="Include Hitch" },true),
                        new TableCell(includeHitchCheckBox,true)
                        ),
                    new TableRow(
                        new TableCell(new Label {Text="Hitch Radius" },true),
                        new TableCell(HitchRadiusNumericStepper,true)
                        ),
                    new TableRow(
                        new TableCell(new Label {Text="Hitch Offset" },true),
                        new TableCell(HitchOffsetNumericStepper,true)
                        ),
                    new TableRow(
                        new TableCell(new Label {Text="Keep 2D Profiles" },true),
                        new TableCell(keepTwoDProfilesCheckBox,true)
                        ),
                    new TableRow(
                        new TableCell(new Label {Text="Flip Profiles" },true),
                        new TableCell(flipProfilesCheckBox,true)
                        )
                }
            };
            return layout;
        }



    }
}
