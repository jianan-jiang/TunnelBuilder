using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Rhino.DocObjects;
using Rhino.UI;

using TunnelBuilder.Models;

namespace TunnelBuilder.Views
{
    [
        System.Runtime.InteropServices.Guid("B907C6DF-B575-492E-A736-B193EDB499AB"),
        Rhino.Commands.CommandStyle(Rhino.Commands.Style.ScriptRunner)
    ]
    public partial class TunnelPropertyPanel : UserControl
    {
        public TunnelPropertyPanel()
        {
            InitializeComponent();
            ProfileRoleComboBox.DataSource = getProfileRoleItems();
            ProfileRoleComboBox.DisplayMember = "Text";
            ProfileRoleComboBox.ValueMember = "Value";
        }

        public double Area {
            get { return (double)AreaUpDown.Value; }
            set { AreaUpDown.Value = (decimal)value; }
        }
        public double TroughWidthParameter
        {
            get { return (double)TroughWidthParameterUpDown.Value; }
            set { TroughWidthParameterUpDown.Value = (decimal)value; }
        }

        public double VolumeLoss
        {
            get { return (double)VolumeLossUpDown.Value; }
            set { VolumeLossUpDown.Value = (decimal)value; }
        }

        public string ProfileName
        {
            get { return ProfileNameTextBox.Text.Trim(); }
            set { ProfileNameTextBox.Text = value; }
        }

        public string ProfileRole
        {
            get
            {
                ProfileRoleItem selectedRole = (ProfileRoleItem)ProfileRoleComboBox.SelectedItem;
                return selectedRole.ProfileRole;
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    ProfileRoleComboBox.SelectedItem = ProfileRoleComboBox.Items[0];
                }
                else
                {
                    ProfileRoleComboBox.SelectedValue = value;
                }   
            }
        }
        public double ChainageAtStart
        {
            get { return (double)ChainageAtStartUpDown.Value; }
            set { ChainageAtStartUpDown.Value = (decimal)value; }
        }

        void MaximumSettlmentUpDown_ValueChanged(object sender,EventArgs e)
        {
            OnTunnelPropertyUpdated(e);
        }

        void TroughWidthUpDown_ValueChanged(object sender, EventArgs e)
        {
            OnTunnelPropertyUpdated(e);
        }

        void VolumeLossUpDown_ValueChanged(object sender, EventArgs e)
        {
            OnTunnelPropertyUpdated(e);
        }

        void ChainageAtStartUpDown_ValueChanged(object sender, EventArgs e)
        {
            OnTunnelPropertyUpdated(e);
        }

        void ProfileNameTextBox_ValueChanged(object sender, EventArgs e)
        {
            OnTunnelPropertyUpdated(e);
        }

        void ProfileRoleComboBox_ValueChanged(object sender, EventArgs e)
        {
            if(ProfileRole!="Control Line")
            {
                SettlementsGroupBox.Hide();
            }
            else
            {
                SettlementsGroupBox.Show();
            }
            OnTunnelPropertyUpdated(e);
        }

        void UpdateSettlementGridButton_Clicked(object sender, EventArgs e)
        {
            Rhino.RhinoApp.RunScript("GenerateSettlementContour", true);
        }

        public event EventHandler TunnelPropertyUpdated;
        protected virtual void OnTunnelPropertyUpdated(EventArgs e)
        {
            EventHandler handler = TunnelPropertyUpdated;
            handler?.Invoke(this, e);
        }

        private ProfileRoleItem[] getProfileRoleItems()
        {
            List<ProfileRoleItem> profileRoleItems = new List<ProfileRoleItem>();
            int i = 1;
            foreach(ProfileRole role in Enum.GetValues(typeof(ProfileRole)))
            {
                profileRoleItems.Add(new ProfileRoleItem{ ID=i,ProfileRole=TunnelProperty.ProfileRoleNameDictionary[role] });
            }
            return profileRoleItems.ToArray();
        }

    }

    class ProfileRoleItem
    {
        public int ID { get; set; }
        public string ProfileRole { get; set; }

        public string Value {
            get
            {
                return ProfileRole;
            }
            set
            {
                ProfileRole = value;
            }
        }

        public string Text
        {
            get
            {
                return ProfileRole;
            }
            set
            {
                ProfileRole = value;
            }
        }
    }

    class TunnelPropertyPage:ObjectPropertiesPage
    {
        TunnelPropertyPanel m_control = new TunnelPropertyPanel();
        
        RhinoObject rhObj;
        public override Icon PageIcon(Size sizeInPixels)
        {
            return Properties.Resources.TunnelBuilderIcon;
        }
        public override object PageControl
        {
            get { return m_control; }
        }
        public override string EnglishPageTitle
        {
            get { return "Tunnel Property"; }
        }

        void OnTunnelPropertyUpdated(object sender,EventArgs e)
        {
            var tunnelProperty = rhObj.Geometry.UserData.Find(typeof(Models.TunnelProperty)) as Models.TunnelProperty;
            if (tunnelProperty != null)
            {
                rhObj.Geometry.UserData.Remove(tunnelProperty);
            }

            tunnelProperty = new Models.TunnelProperty();
            tunnelProperty.Area = m_control.Area;
            tunnelProperty.TroughWidthParameter = m_control.TroughWidthParameter;
            tunnelProperty.VolumeLoss = m_control.VolumeLoss;
            tunnelProperty.ProfileName = m_control.ProfileName;
            tunnelProperty.ChainageAtStart = m_control.ChainageAtStart;
            tunnelProperty.ProfileRole = m_control.ProfileRole;
            rhObj.Geometry.UserData.Add(tunnelProperty);
        }
        public override bool ShouldDisplay(ObjectPropertiesPageEventArgs e)
        {
            
            var objCount = e.ObjectCount;
            if (objCount == 1){
                rhObj = e.Objects[0];
                var tunnelProperty = rhObj.Geometry.UserData.Find(typeof(Models.TunnelProperty)) as Models.TunnelProperty;
                if (tunnelProperty == null)
                {
                    if (rhObj.Geometry as Rhino.Geometry.Curve != null || rhObj.Geometry as Rhino.Geometry.Brep != null)
                    {
                        m_control.TunnelPropertyUpdated += OnTunnelPropertyUpdated;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    m_control.Area = tunnelProperty.Area;
                    m_control.TroughWidthParameter = tunnelProperty.TroughWidthParameter;
                    m_control.VolumeLoss = tunnelProperty.VolumeLoss;
                    m_control.ProfileName = tunnelProperty.ProfileName;
                    m_control.ChainageAtStart = tunnelProperty.ChainageAtStart;
                    m_control.ProfileRole = tunnelProperty.ProfileRole;
                    m_control.TunnelPropertyUpdated += OnTunnelPropertyUpdated;
                    return true;
                }
            }
            else
            {
                return false;
            }
            
        }


    }
}
