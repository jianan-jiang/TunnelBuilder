using System.Collections.Generic;
using Rhino.UI;
using TunnelBuilder.Views;
using AutoUpdaterDotNET;
using Rhino.PlugIns;
using System.ComponentModel;
using System.Threading;
using System;

namespace TunnelBuilder
{
    ///<summary>
    /// <para>Every RhinoCommon .rhp assembly must have one and only one PlugIn-derived
    /// class. DO NOT create instances of this class yourself. It is the
    /// responsibility of Rhino to create an instance of this class.</para>
    /// <para>To complete plug-in information, please also see all PlugInDescription
    /// attributes in AssemblyInfo.cs (you might need to click "Project" ->
    /// "Show All Files" to see it in the "Solution Explorer" window).</para>
    ///</summary>
    public class TunnelBuilderPlugIn : Rhino.PlugIns.PlugIn,ISynchronizeInvoke

    {

        private readonly object _sync;

        public TunnelBuilderPlugIn()
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            string buildDate = Properties.BuildInfo.BuildDate.Replace(Environment.NewLine,"");
            Rhino.RhinoApp.WriteLine("Loading Tunnel Builder, version {0}, built on {1}",version,buildDate);
            Rhino.RhinoApp.WriteLine("jianan.jiang@psm.com.au");
            _sync = new object();
            Instance = this;
        }

        ///<summary>Gets the only instance of the TunnelBuilderPlugIn plug-in.</summary>
        public static TunnelBuilderPlugIn Instance
        {
            get; private set;
        }

        // You can override methods here to change the plug-in behavior on
        // loading and shut down, add options pages to the Rhino _Option command
        // and maintain plug-in wide options in a document.

        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            //Setup auto update check
            AutoUpdater.OpenDownloadPage = true;
            AutoUpdater.Start(Properties.UpdateResource.UpdateXMLAddress, Assembly);

            //Check updates every two minutes
            //System.Timers.Timer timer = new System.Timers.Timer { Interval = Convert.ToInt32(Properties.UpdateResource.UpdateInterval) * 60 * 1000, SynchronizingObject = this };
            //timer.Elapsed += delegate
            //{
            //    AutoUpdater.Start(Properties.UpdateResource.UpdateXMLAddress, Assembly);
            //};
            //timer.Start();
     
            return LoadReturnCode.Success;
        }
        protected override void ObjectPropertiesPages(List<ObjectPropertiesPage> pages)
        {
            pages.Add(new TunnelPropertyPage());
        }

        //Implements ISynchronizeInvoke Interface
        ///<summary>Behaviour when the methods have been invoked</summary>
        public IAsyncResult BeginInvoke(Delegate method,object[] args)
        {
            var result = new AsyncResult();
            ThreadPool.QueueUserWorkItem(delegate
            {
                result.AsyncWaitHandle = new ManualResetEvent(false);
                try
                {
                    result.AsyncState = Invoke(method, args);
                }catch (Exception exception)
                {
                    result.Exception = exception;
                }
                result.IsCompleted = true;
            });

            return result;
        }

        public object EndInvoke(IAsyncResult result)
        {
            if (!result.IsCompleted)
            {
                result.AsyncWaitHandle.WaitOne();
            }

            return result.AsyncState;
        }

        public object Invoke(Delegate method,object[] args)
        {
            lock (_sync)
            {
                return method.DynamicInvoke(args);
            }
        }

        public bool InvokeRequired
        {
            get { return true; }
        }
    }

    class AsyncResult : IAsyncResult
    {
        object _state;

        public bool IsCompleted { get; set; }
        public WaitHandle AsyncWaitHandle { get; internal set; }
        public object AsyncState {
            get
            {
                if(Exception != null)
                {
                    throw Exception;
                }
                return _state;
            }
            internal set
            {
                _state = value;
            }
        }

        public bool CompletedSynchronously { get { return IsCompleted; } }

        internal Exception Exception { get; set; }
    }
}