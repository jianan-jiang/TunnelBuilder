using System;
using System.Collections.Generic;
using Rhino.FileIO;

namespace TunnelBuilder.Models
{
    public class DocumentData
    {
        /// <summary>
        /// Class major and minor version numbers
        /// </summary>
        private const int MAJOR = 1;
        private const int MINOR = 0;

        /// <summary>
        /// Public constructor
        /// </summary>
        /// 
        public DocumentData()
        {
            controlLineLayerAddress = "Control Line";
        }

        /// <summary>
        /// Return our data
        /// </summary>
        /// 
        public string controlLineLayerAddress { get; set; }

        /// <summary>
        /// Write to binary archive
        /// </summary>
        /// 
        public bool Write(BinaryArchiveWriter archive)
        {
            var rc = false;
            if (null != archive)
            {
                try
                {
                    archive.Write3dmChunkVersion(MAJOR, MINOR);
                    archive.WriteString(controlLineLayerAddress);
                    rc = archive.WriteErrorOccured;
                }
                catch
                {
                    // ignored
                }
            }
            return rc;
        }
    }
}
