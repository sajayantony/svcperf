/*  Copyright (c) Microsoft Corporation.  All rights reserved. */
/* AUTHOR: Vance Morrison   
 * Date  : 10/20/2007  */
using System;
using System.Diagnostics;
using System.IO;

namespace EtlViewer
{
    /// <summary>
    /// Privides a quick HTML users guide.  
    /// </summary>
    static class UsersGuide
    {
        /// <summary>
        /// Displayes an the embeded HTML user's guide in a browser. 
        /// </summary>
        /// <returns>true if successful.</returns>
        public static void DisplayUsersGuide(string resourceName, string anchor = "")
        {            
                string uri = resourceName;
                {
                    if (!string.IsNullOrEmpty(anchor))
                        uri = "file:///" + uri.Replace('\\', '/').Replace(" ", "%20") + "#" + anchor;
                }
                Process.Start(uri);
        }
    }
}