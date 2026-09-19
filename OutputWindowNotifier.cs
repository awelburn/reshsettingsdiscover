using System;
using JetBrains.Diagnostics;
using JetBrains.Util;
using JetBrains.Util.Logging;
using Microsoft.VisualStudio.Shell.Interop;

namespace ReshSettingsDiscover
{
    /// <summary>
    /// Reports plugin status to a dedicated pane of the Visual Studio Output window.
    /// No-op when the Output window is not available (eg under Rider). Never throws.
    /// </summary>
    public class OutputWindowNotifier
    {
    #region Logging

        private static readonly ILogger Log = Logger.GetLogger("ResharperSettings.Autodiscovery");

    #endregion

    #region Attributes

        /// <summary>
        /// Stable GUID identifying the plugin pane in the Output window.
        /// </summary>
        public static readonly Guid PaneGuid = new Guid("3F5D5B11-9B0E-4D22-9A76-2FD5C0F0A5E1");

        /// <summary>
        /// Pane title as it shows up in the Output window pane selector.
        /// </summary>
        public const string PaneTitle = "Resharper Settings Autodiscovery";

    #endregion

    #region Data

        private readonly IVsOutputWindow myOutputWindow;

    #endregion

    #region Init

        public OutputWindowNotifier(IVsOutputWindow outputWindow)
        {
            myOutputWindow = outputWindow;
        }

    #endregion

    #region Reporting

        /// <summary>
        /// Writes a single line to the plugin pane. Thread-safe, never throws.
        /// </summary>
        public void WriteLine(string message)
        {
            try
            {
                if(myOutputWindow == null)
                    return; // Not running under Visual Studio, nothing to report to

                var paneGuid = PaneGuid;
                myOutputWindow.GetPane(ref paneGuid, out var pane);
                if(pane == null)
                {
                    // Create the pane initially visible, and keep its content when a new solution opens
                    myOutputWindow.CreatePane(ref paneGuid, PaneTitle, 1, 0);
                    myOutputWindow.GetPane(ref paneGuid, out pane);
                }

                if(pane == null)
                {
                    Log.Info("Failed to create the Output window pane, message dropped: {0}", message);
                    return;
                }

                pane.OutputStringThreadSafe(message + Environment.NewLine);
            }
            catch(Exception ex)
            {
                Log.LogException(LoggingLevel.WARN, ex, ExceptionOrigin.Algorithmic, "Failed to write to Output window");
            }
        }

    #endregion
    }
}