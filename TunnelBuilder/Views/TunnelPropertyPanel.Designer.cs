namespace TunnelBuilder.Views
{
    partial class TunnelPropertyPanel
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.AreaLabel = new System.Windows.Forms.Label();
            this.TroughWidthLabel = new System.Windows.Forms.Label();
            this.AreaUpDown = new System.Windows.Forms.NumericUpDown();
            this.TroughWidthParameterUpDown = new System.Windows.Forms.NumericUpDown();
            this.VolumeLossLabel = new System.Windows.Forms.Label();
            this.VolumeLossUpDown = new System.Windows.Forms.NumericUpDown();
            this.SettingsGroupBox = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.AreaUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TroughWidthParameterUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.VolumeLossUpDown)).BeginInit();
            this.SettingsGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // AreaLabel
            // 
            this.AreaLabel.AutoSize = true;
            this.AreaLabel.Location = new System.Drawing.Point(6, 14);
            this.AreaLabel.Name = "AreaLabel";
            this.AreaLabel.Size = new System.Drawing.Size(52, 13);
            this.AreaLabel.TabIndex = 2;
            this.AreaLabel.Text = "Area (m2)";
            // 
            // TroughWidthLabel
            // 
            this.TroughWidthLabel.AutoSize = true;
            this.TroughWidthLabel.Location = new System.Drawing.Point(6, 40);
            this.TroughWidthLabel.Name = "TroughWidthLabel";
            this.TroughWidthLabel.Size = new System.Drawing.Size(123, 13);
            this.TroughWidthLabel.TabIndex = 4;
            this.TroughWidthLabel.Text = "Trough Width Parameter";
            // 
            // AreaUpDown
            // 
            this.AreaUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.AreaUpDown.AutoSize = true;
            this.AreaUpDown.DecimalPlaces = 1;
            this.AreaUpDown.Location = new System.Drawing.Point(142, 14);
            this.AreaUpDown.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.AreaUpDown.Name = "AreaUpDown";
            this.AreaUpDown.Size = new System.Drawing.Size(79, 20);
            this.AreaUpDown.TabIndex = 1;
            this.AreaUpDown.ThousandsSeparator = true;
            this.AreaUpDown.ValueChanged += new System.EventHandler(this.MaximumSettlmentUpDown_ValueChnaged);
            // 
            // TroughWidthParameterUpDown
            // 
            this.TroughWidthParameterUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TroughWidthParameterUpDown.AutoSize = true;
            this.TroughWidthParameterUpDown.DecimalPlaces = 2;
            this.TroughWidthParameterUpDown.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.TroughWidthParameterUpDown.Location = new System.Drawing.Point(142, 38);
            this.TroughWidthParameterUpDown.Name = "TroughWidthParameterUpDown";
            this.TroughWidthParameterUpDown.Size = new System.Drawing.Size(79, 20);
            this.TroughWidthParameterUpDown.TabIndex = 2;
            this.TroughWidthParameterUpDown.ValueChanged += new System.EventHandler(this.TroughWidthUpDown_ValueChnaged);
            // 
            // VolumeLossLabel
            // 
            this.VolumeLossLabel.AutoSize = true;
            this.VolumeLossLabel.Location = new System.Drawing.Point(9, 66);
            this.VolumeLossLabel.Name = "VolumeLossLabel";
            this.VolumeLossLabel.Size = new System.Drawing.Size(67, 13);
            this.VolumeLossLabel.TabIndex = 5;
            this.VolumeLossLabel.Text = "Volume Loss";
            // 
            // VolumeLossUpDown
            // 
            this.VolumeLossUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.VolumeLossUpDown.AutoSize = true;
            this.VolumeLossUpDown.DecimalPlaces = 2;
            this.VolumeLossUpDown.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.VolumeLossUpDown.Location = new System.Drawing.Point(142, 66);
            this.VolumeLossUpDown.Name = "VolumeLossUpDown";
            this.VolumeLossUpDown.Size = new System.Drawing.Size(79, 20);
            this.VolumeLossUpDown.TabIndex = 3;
            this.VolumeLossUpDown.ValueChanged += new System.EventHandler(this.VolumeLossUpDown_ValueChnaged);
            // 
            // SettingsGroupBox
            // 
            this.SettingsGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SettingsGroupBox.AutoSize = true;
            this.SettingsGroupBox.Controls.Add(this.TroughWidthLabel);
            this.SettingsGroupBox.Controls.Add(this.VolumeLossUpDown);
            this.SettingsGroupBox.Controls.Add(this.AreaLabel);
            this.SettingsGroupBox.Controls.Add(this.VolumeLossLabel);
            this.SettingsGroupBox.Controls.Add(this.AreaUpDown);
            this.SettingsGroupBox.Controls.Add(this.TroughWidthParameterUpDown);
            this.SettingsGroupBox.Location = new System.Drawing.Point(3, 14);
            this.SettingsGroupBox.Name = "SettingsGroupBox";
            this.SettingsGroupBox.Size = new System.Drawing.Size(248, 105);
            this.SettingsGroupBox.TabIndex = 6;
            this.SettingsGroupBox.TabStop = false;
            this.SettingsGroupBox.Text = "Settings";
            // 
            // TunnelPropertyPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.Controls.Add(this.SettingsGroupBox);
            this.Name = "TunnelPropertyPanel";
            this.Size = new System.Drawing.Size(263, 285);
            ((System.ComponentModel.ISupportInitialize)(this.AreaUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TroughWidthParameterUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.VolumeLossUpDown)).EndInit();
            this.SettingsGroupBox.ResumeLayout(false);
            this.SettingsGroupBox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label AreaLabel;
        private System.Windows.Forms.Label TroughWidthLabel;
        private System.Windows.Forms.NumericUpDown AreaUpDown;
        private System.Windows.Forms.NumericUpDown TroughWidthParameterUpDown;
        private System.Windows.Forms.Label VolumeLossLabel;
        private System.Windows.Forms.NumericUpDown VolumeLossUpDown;
        private System.Windows.Forms.GroupBox SettingsGroupBox;
    }
}