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

namespace TunnelBuilder.Views
{
    [System.Runtime.InteropServices.Guid("B907C6DF-B575-492E-A736-B193EDB499AB")]
    public partial class TunnelPropertyPanel : UserControl
    {
        public TunnelPropertyPanel()
        {
            InitializeComponent();
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

        void MaximumSettlmentUpDown_ValueChnaged(object sender,EventArgs e)
        {
            OnTunnelPropertyUpdated(e);
        }

        void TroughWidthUpDown_ValueChnaged(object sender, EventArgs e)
        {
            OnTunnelPropertyUpdated(e);
        }

        void VolumeLossUpDown_ValueChnaged(object sender, EventArgs e)
        {
            OnTunnelPropertyUpdated(e);
        }

        public event EventHandler TunnelPropertyUpdated;
        protected virtual void OnTunnelPropertyUpdated(EventArgs e)
        {
            EventHandler handler = TunnelPropertyUpdated;
            handler?.Invoke(this, e);
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

            rhObj.Geometry.UserData.Add(new Models.TunnelProperty(m_control.Area, m_control.TroughWidthParameter,m_control.VolumeLoss));
        }
        public override bool ShouldDisplay(ObjectPropertiesPageEventArgs e)
        {
            
            var objCount = e.ObjectCount;
            if (objCount == 1){
                rhObj = e.Objects[0];
                var tunnelProperty = rhObj.Geometry.UserData.Find(typeof(Models.TunnelProperty)) as Models.TunnelProperty;
                if (tunnelProperty == null)
                {
                    if (rhObj.Geometry as Rhino.Geometry.Curve != null)
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
