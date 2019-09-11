using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;
using System.Linq;

using System.Windows.Forms;

using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

using TunnelBuilder.Models.FLAC3D;

namespace TunnelBuilder
{
    [System.Runtime.InteropServices.Guid("B2A40B15-5F11-41B9-AD1A-5E0F96601B8B")]
    public class ExportPlot:Command
    {
        bool OverrideAllFiles = false;
        string Folder;
        ExportEnvironment exportEnvironment;

        public override string EnglishName
        {
            get { return "ExportPlot"; }
        }

        Dictionary<ExportEnvironment, String> ExportEnvironmentExtension = new Dictionary<ExportEnvironment, string>
        {
            { ExportEnvironment.FLAC3D,".dat"}
        };

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {

            var result = getFolder();
            if(result != Result.Success)
            {
                return result;
            }

            var views = doc.NamedViews.ToDictionary(v=>v.Name);

            ResourceManager resourceManagerIsometric = new ResourceManager(typeof(Properties.Isometric));
            ResourceSet resourceSetIsometric = resourceManagerIsometric.GetResourceSet(CultureInfo.CurrentCulture, true, true);

            ResourceManager resourceManagerPlan = new ResourceManager(typeof(Properties.Plan));
            ResourceSet resourceSetPlan = resourceManagerPlan.GetResourceSet(CultureInfo.CurrentCulture, true, true);

            ResourceManager resourceManagerSection = new ResourceManager(typeof(Properties.Section));
            ResourceSet resourceSetSection = resourceManagerSection.GetResourceSet(CultureInfo.CurrentCulture, true, true);

            foreach (var viewName in views.Keys)
            {
                var view = views[viewName];
                var viewport = view.Viewport;
                if(viewName=="Isometric")
                {
                    if(!viewport.IsParallelProjection)
                    {
                        RhinoApp.WriteLine("Isometric view not set to parallel projection");
                        viewport.ChangeToParallelProjection(true);
                    }

                    var center = viewport.CameraLocation;
                    var target = viewport.TargetPoint;
                    double roll = 0.0;
                    Camera camera = new Camera(center, target, roll);
                    foreach (DictionaryEntry entry in resourceSetIsometric)
                    {
                        string plotName = entry.Key.ToString();
                        string plotTemplate = System.Text.Encoding.ASCII.GetString((byte[])entry.Value);

                        Plot plot = new Plot(camera, plotTemplate);
                        string plotData = plot.compile();

                        result = savePlot("",plotName, plotData);

                    }
                }else if (viewName == "Plan")
                {
                    if (!viewport.IsParallelProjection)
                    {
                        RhinoApp.WriteLine("Plan view not set to parallel projection");
                        viewport.ChangeToParallelProjection(true);
                    }

                    var center = viewport.CameraLocation;
                    var target = viewport.TargetPoint;
                    double roll = 0.0;
                    Camera camera = new Camera(center, target, roll);
                    foreach (DictionaryEntry entry in resourceSetPlan)
                    {
                        string plotName = entry.Key.ToString();
                        string plotTemplate = System.Text.Encoding.ASCII.GetString((byte[])entry.Value);

                        Plot plot = new Plot(camera, plotTemplate);
                        string plotData = plot.compile();

                        result = savePlot(viewName,plotName, plotData);
                    }
                }
                else
                {
                    var center = viewport.CameraLocation;
                    var target = viewport.TargetPoint;
                    double roll = 0.0;
                    Camera camera = new Camera(center, target, roll);
                    Surface cutSurface = null;
                    using (Rhino.Input.Custom.GetObject go = new Rhino.Input.Custom.GetObject())
                    {
                        go.DisablePreSelect();
                        go.SetCommandPrompt("Select cut surface for "+viewName);
                        go.GeometryFilter = Rhino.DocObjects.ObjectType.Surface;
                        while (true)
                        {
                            GetResult get_rc = go.GetMultiple(1,1);
                            if (get_rc == GetResult.Object)
                            {
                                cutSurface = go.Object(0).Surface();
                                if (cutSurface == null)
                                {
                                    return Rhino.Commands.Result.Failure;
                                }
                            }
                            else if (get_rc == GetResult.Option)
                            {
                                continue;
                            }
                            break;
                        }

                        if (cutSurface == null)
                        {
                            RhinoApp.WriteLine("No surface was selected");
                            return Rhino.Commands.Result.Failure;
                        }
                    }

                    var uSPan = cutSurface.Domain(0);
                    var vSpan = cutSurface.Domain(1);

                    Point3d origin = cutSurface.PointAt(uSPan.Mid, vSpan.Mid);
                    Vector3d normal = cutSurface.NormalAt(uSPan.Mid, vSpan.Mid);

                    Section section = new Section(origin, normal);

                    foreach (DictionaryEntry entry in resourceSetSection)
                    {
                        string plotName = entry.Key.ToString();
                        string plotTemplate = System.Text.Encoding.ASCII.GetString((byte[])entry.Value);

                        SectionPlot sectionPlot = new SectionPlot(camera, section, plotTemplate,viewName);
                        string plotData = sectionPlot.compile();

                        result = savePlot(viewName, plotName, plotData);
                    }

                }

                if(result != Result.Success)
                {
                    return result;
                }
            }

            return Result.Success;
        }

        private Result getFolder()
        {
            var fbd = new FolderBrowserDialog();
            fbd.Description = "Select the folder to save plot data files";

            DialogResult dialogResult = fbd.ShowDialog();

            if (dialogResult == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                var fn = fbd.SelectedPath;

                string filenameWithoutExtension = System.IO.Path.ChangeExtension(fn, null);

                Folder = filenameWithoutExtension;
                return Result.Success;
            }
            else
            {
                return Result.Cancel;
            }
            
        }

        private Result savePlot(string prefix, string plotName, string plotData)
        {
            string filename = Folder + "\\"+ prefix + plotName + ExportEnvironmentExtension[ExportEnvironment.FLAC3D];
            System.IO.StreamWriter fs = new System.IO.StreamWriter(filename);

            if(System.IO.File.Exists(filename) && !OverrideAllFiles)
            {
                var result = Rhino.UI.Dialogs.ShowMessage("Overwrite existing files", "Found existing files", Rhino.UI.ShowMessageButton.YesNoCancel, Rhino.UI.ShowMessageIcon.Question);
                if(result == Rhino.UI.ShowMessageResult.Yes)
                {
                    OverrideAllFiles = true;
                }else if (result==Rhino.UI.ShowMessageResult.No)
                {
                    getFolder();
                    savePlot(prefix, plotName, plotData);
                }
                else
                {
                    return Result.Cancel;
                }
            }

            fs.Write(plotData);
            fs.Close();

            return Result.Success;
        }
    }

    
}
