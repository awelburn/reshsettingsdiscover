using System;
using JetBrains.Application.FileSystemTracker;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Implementation;
using JetBrains.Application.Settings.Storage.DefaultBody;
using JetBrains.Application.Settings.Storage.Persistence;
using JetBrains.Application.Settings.UserInterface;
using JetBrains.Application.Threading;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ProjectModel.Settings.Storages;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace ReshSettingsDiscover
{
    [SolutionComponent]
    public class FindAndLoadSettings
    {
    #region Init

        public FindAndLoadSettings(Lifetime lifetimeComponent, SettingsStorageProvidersCollection publisher, SolutionFileLocationLive solfile, IThreading threading, IFileSystemTracker filetracker, FileSettingsStorageBehavior behavior, ISolution solution, IShellLocks locks)
        {
            // In case the solution path changes, watch each value anew
            solfile.SolutionFileLocation.ForEachValue_NotNull(lifetimeComponent, (lifetimeLocation, location) =>
            {
                double priority = ProjectModelSettingsStorageMountPointPriorityClasses.SolutionShared;
                for(VirtualFileSystemPath dir = location.Directory; !dir.IsNullOrEmpty(); dir = dir.Directory)
                {
                    try
                    {
                        priority *= .9; // The upper folder, the lower priority (regular solution-shared file takes over all of them)

                        // Walk up folders
                        // TODO: add file-system-watcher here
                        foreach(VirtualFileSystemPath settingsfile in dir.GetChildFiles("*." + AutoLoadExtension, PathSearchFlags.ExcludeDirectories | PathSearchFlags.ExcludeHidden))
                        {
                            var relativePath = settingsfile.MakeRelativeTo(location.Directory).FullPath;
                            var name = relativePath.Replace("." + AutoLoadExtension, "");

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
                        }
                    }
                    catch(Exception ex)
                    {
                        Logger.LogException(ex);
                    }
                }
            });
        }

    #endregion

    #region Attributes

    public static readonly string AutoLoadExtension = "AutoLoad.DotSettings";

    #endregion
    }
}