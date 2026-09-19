using System;
using System.Collections.Generic;
using JetBrains.Application.Components;
using JetBrains.Application.FileSystemTracker;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Implementation;
using JetBrains.Application.Settings.Storage.DefaultBody;
using JetBrains.Application.Settings.Storage.Persistence;
using JetBrains.Application.Settings.UserInterface;
using JetBrains.Application.Threading;
using JetBrains.Application.Parts;
using JetBrains.DataFlow;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ProjectModel.Settings.Storages;
using JetBrains.Util;
using JetBrains.Util.Logging;
using Microsoft.VisualStudio.Shell.Interop;

namespace ReshSettingsDiscover
{
    /// <summary>
    /// Looks for any *.AutoLoad.DotSettings files in the parent folders of the solution
    /// and mounts them as ReSharper settings layers.
    /// </summary>
    /// <remarks>
    /// A worker component nobody depends on: it implements <see cref="IStartupActivity"/> so that the component
    /// model auto-creates it when the solution component container warms up. Note that the solution container only
    /// CREATES such components, it never calls <see cref="Run"/> (that is a shell-container-only mechanism),
    /// so all the work is done in the constructor.
    /// </remarks>
    [SolutionComponent(Instantiation.DemandAnyThreadUnsafe)]
    public class FindAndLoadSettings : IStartupActivity
    {
    #region Logging

        private static readonly ILogger Log = Logger.GetLogger("ResharperSettings.Autodiscovery");

    #endregion

    #region Init

        public FindAndLoadSettings(Lifetime lifetimeComponent, SettingsStorageProvidersCollection publisher, SolutionFileLocationLive solfile, IThreading threading, IFileSystemTracker filetracker, FileSettingsStorageBehavior behavior, ISolution solution, IShellLocks locks)
        {

            var output = new OutputWindowNotifier(TryGetOutputWindow(solution));

            try
            {
                // In case the solution path changes, watch each value anew
                solfile.SolutionFileLocation.ForEachValue_NotNull(lifetimeComponent, (lifetimeLocation, location) =>
                {
                    var layerNames = new List<string>();
                    double priority = ProjectModelSettingsStorageMountPointPriorityClasses.SolutionShared;
                    for(VirtualFileSystemPath dir = location.Directory; !dir.IsNullOrEmpty(); dir = dir.Directory)
                    {
                        try
                        {
                            priority *= .9; // The upper folder, the lower priority (regular solution-shared file takes over all of them)

                            Log.Verbose("Scanning folder '{0}' (layer priority {1:0.###})", dir.FullPath, priority);

                            // Walk up folders
                            // TODO: add file-system-watcher here
                            var ext = "." + AutoLoadExtension;
                            foreach(VirtualFileSystemPath settingsfile in dir.GetChildFiles("*." + AutoLoadExtension, PathSearchFlags.ExcludeDirectories | PathSearchFlags.ExcludeHidden))
                            {
                                var relativePath = settingsfile.MakeRelativeTo(location.Directory).FullPath;
                                var name = relativePath.Replace(ext, "");

                                // Physical storage
                                var fsSettingsfile = FileSystemPath.Parse(settingsfile.FullPath);
                                IProperty<FileSystemPath> livepath = new Property<FileSystemPath>("StoragePath", fsSettingsfile);
                                var storage = new XmlFileSettingsStorage(lifetimeLocation, name, livepath, SettingsStoreSerializationToXmlDiskFile.SavingEmptyContent.KeepFile, threading, filetracker, behavior, null);

                                // Mount as a layer
                                IIsAvailable availability = new IsAvailableByDataConstant<ISolution>(lifetimeLocation, ProjectModelDataConstants.SOLUTION, solution, locks); // Only when querying in solution context (includes Application-Wide)
                                ISettingsStorageMountPoint mount = new SettingsStorageMountPoint(storage.Storage, SettingsStorageMountPoint.MountPath.Default, 0, priority, availability, name);

                                // Metadata
                                livepath.FlowInto(lifetimeLocation, mount.Metadata.GetOrCreateProperty(UserFriendlySettingsLayers.DiskFilePath, null, true));
                                mount.Metadata.Set(UserFriendlySettingsLayers.Origin, string.Format("Automatically loaded from solution parent folder, \"{0}\"", relativePath));
                                mount.Metadata.Set(UserInjectedSettingsLayers.IsHostingUserInjections, true);

                                // Publish
                                publisher.Storages.Add(lifetimeLocation, storage.Storage);
                                publisher.MountPoints.Add(lifetimeLocation, mount);
                                layerNames.Add(name);

                                Log.Info("Loaded settings layer '{0}' from '{1}' with priority {2:0.###}", name, settingsfile.FullPath, priority);
                                output.WriteLine(string.Format("Loaded settings layer '{0}' from '{1}' with priority {2:0.###}", name, settingsfile.FullPath, priority));
                            }
                        }
                        catch(Exception ex)
                        {
                            Log.LogException(LoggingLevel.WARN, ex, ExceptionOrigin.Algorithmic,
                                string.Format("Failed to load auto-discovered settings from '{0}'", dir.FullPath)
                            );
                        }
                    }

                    string summary;
                    if(layerNames.Count == 0)
                        summary = string.Format("No '*.{0}' settings files found in solution parent folders", AutoLoadExtension);
                    else
                        summary = string.Format("Loaded {0} auto-discovered settings layer(s)", layerNames.Count);

                    Log.Info(summary);
                    output.WriteLine(summary);
                });
            }
            catch(Exception ex)
            {
                Log.LogException(LoggingLevel.WARN, ex, ExceptionOrigin.OuterWorld, "Error while scanning for auto-load settings files");
            }
        }

    #endregion

        /// <summary>
        /// Never called for solution components: the solution container only auto-creates IStartupActivity parts,
        /// it does not run them. The actual work happens in the constructor. Kept only to make the container
        /// auto-create this worker component that nothing else depends on.
        /// </summary>
        public void Run(Lifetime lifetime)
        { }


        /// <summary>
        /// Gets the Visual Studio Output window service, or null when not running under Visual Studio
        /// (or the service is not available). Never throws, reporting is optional.
        /// </summary>
        /// <param name="solution"></param>
        private static IVsOutputWindow TryGetOutputWindow(ISolution solution)
        {
            try
            {
                return solution.GetComponent<IVsOutputWindow>();
            }
            catch(Exception ex)
            {
                Log.Warn("Visual Studio Output window service is not available: {0}", ex.Message);
                return null;
            }
        }

        /// <summary>Auto-load settings file extension.</summary>
        public static readonly string AutoLoadExtension = "AutoLoad.DotSettings";
    }
}