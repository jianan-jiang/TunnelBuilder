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
            this.components = new System.ComponentModel.Container();
            this.AreaLabel = new System.Windows.Forms.Label();
            this.TroughWidthLabel = new System.Windows.Forms.Label();
            this.TroughWidthParameterUpDown = new System.Windows.Forms.NumericUpDown();
            this.VolumeLossLabel = new System.Windows.Forms.Label();
            this.VolumeLossUpDown = new System.Windows.Forms.NumericUpDown();
            this.SettlementsGroupBox = new System.Windows.Forms.GroupBox();
            this.UpdateSettlementGridButton = new System.Windows.Forms.Button();
            this.GeneralGroupBox = new System.Windows.Forms.GroupBox();
            this.ProfileRoleComboBox = new System.Windows.Forms.ComboBox();
            this.ProfileRoleLabel = new System.Windows.Forms.Label();
            this.ChainageAtStartLabel = new System.Windows.Forms.Label();
            this.ChainageAtStartUpDown = new System.Windows.Forms.NumericUpDown();
            this.ProfileNameTextBox = new System.Windows.Forms.TextBox();
            this.ProfileNameLabel = new System.Windows.Forms.Label();
            this.AreaUpDown = new System.Windows.Forms.NumericUpDown();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.TroughWidthParameterUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.VolumeLossUpDown)).BeginInit();
            this.SettlementsGroupBox.SuspendLayout();
            this.GeneralGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ChainageAtStartUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.AreaUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // AreaLabel
            // 
            this.AreaLabel.AutoSize = true;
            this.AreaLabel.Location = new System.Drawing.Point(6, 65);
            this.AreaLabel.Name = "AreaLabel";
            this.AreaLabel.Size = new System.Drawing.Size(52, 13);
            this.AreaLabel.TabIndex = 2;
            this.AreaLabel.Text = "Area (m2)";
            // 
            // TroughWidthLabel
            // 
            this.TroughWidthLabel.AutoSize = true;
            this.TroughWidthLabel.Location = new System.Drawing.Point(6, 16);
            this.TroughWidthLabel.Name = "TroughWidthLabel";
            this.TroughWidthLabel.Size = new System.Drawing.Size(123, 13);
            this.TroughWidthLabel.TabIndex = 4;
            this.TroughWidthLabel.Text = "Trough Width Parameter";
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
            this.TroughWidthParameterUpDown.Location = new System.Drawing.Point(142, 14);
            this.TroughWidthParameterUpDown.Name = "TroughWidthParameterUpDown";
            this.TroughWidthParameterUpDown.Size = new System.Drawing.Size(65, 20);
            this.TroughWidthParameterUpDown.TabIndex = 2;
            this.TroughWidthParameterUpDown.ValueChanged += new System.EventHandler(this.TroughWidthUpDown_ValueChanged);
            // 
            // VolumeLossLabel
            // 
            this.VolumeLossLabel.AutoSize = true;
            this.VolumeLossLabel.Location = new System.Drawing.Point(6, 42);
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
            this.VolumeLossUpDown.Location = new System.Drawing.Point(142, 42);
            this.VolumeLossUpDown.Name = "VolumeLossUpDown";
            this.VolumeLossUpDown.Size = new System.Drawing.Size(62, 20);
            this.VolumeLossUpDown.TabIndex = 3;
            this.VolumeLossUpDown.ValueChanged += new System.EventHandler(this.VolumeLossUpDown_ValueChanged);
            // 
            // SettlementsGroupBox
            // 
            this.SettlementsGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SettlementsGroupBox.AutoSize = true;
            this.SettlementsGroupBox.Controls.Add(this.UpdateSettlementGridButton);
            this.SettlementsGroupBox.Controls.Add(this.TroughWidthLabel);
            this.SettlementsGroupBox.Controls.Add(this.VolumeLossUpDown);
            this.SettlementsGroupBox.Controls.Add(this.VolumeLossLabel);
            this.SettlementsGroupBox.Controls.Add(this.TroughWidthParameterUpDown);
            this.SettlementsGroupBox.Location = new System.Drawing.Point(11, 152);
            this.SettlementsGroupBox.Name = "SettlementsGroupBox";
            this.SettlementsGroupBox.Size = new System.Drawing.Size(234, 126);
            this.SettlementsGroupBox.TabIndex = 6;
            this.SettlementsGroupBox.TabStop = false;
            this.SettlementsGroupBox.Text = "Settlements";
            // 
            // UpdateSettlementGridButton
            // 
            this.UpdateSettlementGridButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.UpdateSettlementGridButton.Location = new System.Drawing.Point(9, 84);
            this.UpdateSettlementGridButton.Name = "UpdateSettlementGridButton";
            this.UpdateSettlementGridButton.Size = new System.Drawing.Size(194, 23);
            this.UpdateSettlementGridButton.TabIndex = 6;
            this.UpdateSettlementGridButton.Text = "Update Settlement Grid";
            this.UpdateSettlementGridButton.UseVisualStyleBackColor = true;
            this.UpdateSettlementGridButton.Click += new System.EventHandler(this.UpdateSettlementGridButton_Clicked);
            // 
            // GeneralGroupBox
            // 
            this.GeneralGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.GeneralGroupBox.Controls.Add(this.ProfileRoleComboBox);
            this.GeneralGroupBox.Controls.Add(this.ProfileRoleLabel);
            this.GeneralGroupBox.Controls.Add(this.ChainageAtStartLabel);
            this.GeneralGroupBox.Controls.Add(this.ChainageAtStartUpDown);
            this.GeneralGroupBox.Controls.Add(this.ProfileNameTextBox);
            this.GeneralGroupBox.Controls.Add(this.ProfileNameLabel);
            this.GeneralGroupBox.Controls.Add(this.AreaLabel);
            this.GeneralGroupBox.Controls.Add(this.AreaUpDown);
            this.GeneralGroupBox.Location = new System.Drawing.Point(11, 18);
            this.GeneralGroupBox.Name = "GeneralGroupBox";
            this.GeneralGroupBox.Size = new System.Drawing.Size(234, 118);
            this.GeneralGroupBox.TabIndex = 7;
            this.GeneralGroupBox.TabStop = false;
            this.GeneralGroupBox.Text = "General";
            // 
            // ProfileRoleComboBox
            // 
            this.ProfileRoleComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ProfileRoleComboBox.FormattingEnabled = true;
            this.ProfileRoleComboBox.Location = new System.Drawing.Point(142, 38);
            this.ProfileRoleComboBox.Name = "ProfileRoleComboBox";
            this.ProfileRoleComboBox.Size = new System.Drawing.Size(67, 21);
            this.ProfileRoleComboBox.TabIndex = 9;
            this.ProfileRoleComboBox.SelectedIndexChanged += new System.EventHandler(this.ProfileRoleComboBox_ValueChanged);
            // 
            // ProfileRoleLabel
            // 
            this.ProfileRoleLabel.AutoSize = true;
            this.ProfileRoleLabel.Location = new System.Drawing.Point(6, 42);
            this.ProfileRoleLabel.Name = "ProfileRoleLabel";
            this.ProfileRoleLabel.Size = new System.Drawing.Size(61, 13);
            this.ProfileRoleLabel.TabIndex = 8;
            this.ProfileRoleLabel.Text = "Profile Role";
            // 
            // ChainageAtStartLabel
            // 
            this.ChainageAtStartLabel.AutoSize = true;
            this.ChainageAtStartLabel.Location = new System.Drawing.Point(6, 94);
            this.ChainageAtStartLabel.Name = "ChainageAtStartLabel";
            this.ChainageAtStartLabel.Size = new System.Drawing.Size(87, 13);
            this.ChainageAtStartLabel.TabIndex = 7;
            this.ChainageAtStartLabel.Text = "Chainage at start";
            // 
            // ChainageAtStartUpDown
            // 
            this.ChainageAtStartUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ChainageAtStartUpDown.AutoSize = true;
            this.ChainageAtStartUpDown.DecimalPlaces = 1;
            this.ChainageAtStartUpDown.Location = new System.Drawing.Point(142, 94);
            this.ChainageAtStartUpDown.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.ChainageAtStartUpDown.Name = "ChainageAtStartUpDown";
            this.ChainageAtStartUpDown.Size = new System.Drawing.Size(67, 20);
            this.ChainageAtStartUpDown.TabIndex = 6;
            this.ChainageAtStartUpDown.ThousandsSeparator = true;
            this.ChainageAtStartUpDown.ValueChanged += new System.EventHandler(this.ChainageAtStartUpDown_ValueChanged);
            // 
            // ProfileNameTextBox
            // 
            this.ProfileNameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ProfileNameTextBox.Location = new System.Drawing.Point(142, 13);
            this.ProfileNameTextBox.Name = "ProfileNameTextBox";
            this.ProfileNameTextBox.Size = new System.Drawing.Size(67, 20);
            this.ProfileNameTextBox.TabIndex = 5;
            this.ProfileNameTextBox.TextChanged += new System.EventHandler(this.ProfileNameTextBox_ValueChanged);
            // 
            // ProfileNameLabel
            // 
            this.ProfileNameLabel.AutoSize = true;
            this.ProfileNameLabel.Location = new System.Drawing.Point(6, 16);
            this.ProfileNameLabel.Name = "ProfileNameLabel";
            this.ProfileNameLabel.Size = new System.Drawing.Size(67, 13);
            this.ProfileNameLabel.TabIndex = 4;
            this.ProfileNameLabel.Text = "Profile Name";
            // 
            // AreaUpDown
            // 
            this.AreaUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.AreaUpDown.AutoSize = true;
            this.AreaUpDown.DecimalPlaces = 1;
            this.AreaUpDown.Location = new System.Drawing.Point(142, 65);
            this.AreaUpDown.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.AreaUpDown.Name = "AreaUpDown";
            this.AreaUpDown.Size = new System.Drawing.Size(67, 20);
            this.AreaUpDown.TabIndex = 1;
            this.AreaUpDown.ThousandsSeparator = true;
            this.AreaUpDown.ValueChanged += new System.EventHandler(this.MaximumSettlmentUpDown_ValueChanged);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
            // 
            // TunnelPropertyPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.Controls.Add(this.GeneralGroupBox);
            this.Controls.Add(this.SettlementsGroupBox);
            this.Name = "TunnelPropertyPanel";
            this.Size = new System.Drawing.Size(263, 574);
            ((System.ComponentModel.ISupportInitialize)(this.TroughWidthParameterUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.VolumeLossUpDown)).EndInit();
            this.SettlementsGroupBox.ResumeLayout(false);
            this.SettlementsGroupBox.PerformLayout();
            this.GeneralGroupBox.ResumeLayout(false);
            this.GeneralGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ChainageAtStartUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.AreaUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label AreaLabel;
        private System.Windows.Forms.Label TroughWidthLabel;
        private System.Windows.Forms.NumericUpDown TroughWidthParameterUpDown;
        private System.Windows.Forms.Label VolumeLossLabel;
        private System.Windows.Forms.NumericUpDown VolumeLossUpDown;
        private System.Windows.Forms.GroupBox SettlementsGroupBox;
        private System.Windows.Forms.GroupBox GeneralGroupBox;
        private System.Windows.Forms.Label ChainageAtStartLabel;
        private System.Windows.Forms.NumericUpDown ChainageAtStartUpDown;
        private System.Windows.Forms.TextBox ProfileNameTextBox;
        private System.Windows.Forms.Label ProfileNameLabel;
        private System.Windows.Forms.NumericUpDown AreaUpDown;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ComboBox ProfileRoleComboBox;
        private System.Windows.Forms.Label ProfileRoleLabel;
        private System.Windows.Forms.Button UpdateSettlementGridButton;
    }
}