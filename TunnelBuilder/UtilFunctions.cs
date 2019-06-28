using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace TunnelBuilder
{
    public static class UtilFunctions
    {
        public static int AddNewLayer(Rhino.RhinoDoc doc,string layer_name)
        {

            // Was a layer named entered?
            layer_name = layer_name.Trim();
            if (string.IsNullOrEmpty(layer_name))
            {
                Rhino.RhinoApp.WriteLine("Layer name cannot be blank.");
                return -1;
            }

            // Is the layer name valid?
            if (!Rhino.DocObjects.Layer.IsValidName(layer_name))
            {
                Rhino.RhinoApp.WriteLine(layer_name + " is not a valid layer name.");
                return -1;
            }

            // Does a layer with the same name already exist?
            Rhino.DocObjects.Layer layer = doc.Layers.FindName(layer_name);
            
            if (layer != null)
            {
                return layer.Index;
            }

            // Add a new layer to the document
           
            System.Drawing.Color layer_color = getRandomColor();
            int layer_index = doc.Layers.Add(layer_name, layer_color);
            if (layer_index < 0)
            {
                Rhino.RhinoApp.WriteLine("Unable to add {0} layer.", layer_name);
                return -1;
            }
            return layer_index;
        }
        public static int AddNewLayer(Rhino.RhinoDoc doc, string layer_name, string parent_layer_name)
        {
            // Was a layer named entered?
            layer_name = layer_name.Trim();
            if (string.IsNullOrEmpty(layer_name))
            {
                Rhino.RhinoApp.WriteLine("Layer name cannot be blank.");
                return -1;
            }

            // Is the layer name valid?
            if (!Rhino.DocObjects.Layer.IsValidName(layer_name))
            {
                Rhino.RhinoApp.WriteLine(layer_name + " is not a valid layer name.");
                return -1;
            }

            // Does a layer with the same name already exist?
            int layer_index = doc.Layers.FindByFullPath(parent_layer_name + "::"+layer_name, -1);
            if (layer_index >= 0)
            {
                return layer_index;
            }

            // Get parent layer
            Rhino.DocObjects.Layer parent_layer = doc.Layers.FindName(parent_layer_name);
            Rhino.DocObjects.Layer child_layer = new Rhino.DocObjects.Layer();
            child_layer.ParentLayerId = parent_layer.Id;
            child_layer.Name = layer_name;
            child_layer.Color = getRandomColor();

            layer_index = doc.Layers.Add(child_layer);
            if (layer_index < 0)
            {
                Rhino.RhinoApp.WriteLine("Unable to add {0} layer.", layer_name);
                return -1;
            }
            return layer_index;
        }

        public static System.Drawing.Color getRandomColor()
        {
            Random rnd = new Random();
            int r = rnd.Next(0, 255);
            int g = rnd.Next(0, 255);
            int b = rnd.Next(0, 255);

            return System.Drawing.Color.FromArgb(r, g, b);
        }

        public static string GetDescription<T>(this T e) where T : IConvertible
        {
            if (e is Enum)
            {
                Type type = e.GetType();
                Array values = System.Enum.GetValues(type);

                foreach (int val in values)
                {
                    if (val == e.ToInt32(CultureInfo.InvariantCulture))
                    {
                        var memInfo = type.GetMember(type.GetEnumName(val));
                        var descriptionAttribute = memInfo[0]
                            .GetCustomAttributes(typeof(DescriptionAttribute), false)
                            .FirstOrDefault() as DescriptionAttribute;

                        if (descriptionAttribute != null)
                        {
                            return descriptionAttribute.Description;
                        }
                    }
                }
            }

            return null; // could also return string.Empty
        }
}

   
}
