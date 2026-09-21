# reshsettingsdiscover
ReSharper Solution Settings Autodiscovery.

From Serge Baltic: When you open a solution in Visual Studio, this plugin looks for any `*.AutoLoad.DotSettings` files in parent folders and loads them as ReSharper settings layers. This allows to apply a root settings file to all solutions in the same source control.

## What's new

- **ReSharper 2026.2 compatibility** — package version 3.0, depends on Wave 262.0.0.
- **Fix: settings layers not loading** — the discovery component now implements `IStartupActivity` so ReSharper's component model reliably instantiates it when a solution opens. Nothing else depends on this component, so under the newer lazy-instantiation rules it was previously never created and the auto-discovered layers silently never loaded.
- **Status reporting in the Output window** — a dedicated "Resharper Settings Autodiscovery" pane prints every discovered layer and a per-solution-load summary (see below).
- **Robustness and logging** — a dedicated `ResharperSettings.Autodiscovery` log category, per-folder error isolation (a failure to read one folder no longer aborts the whole scan), and the entire scan wrapped in error handling so it can never break solution load.

## How it works

Three source files:

- **`FindAndLoadSettings.cs`** — The plugin logic. Marked `[SolutionComponent(Instantiation.DemandAnyThreadUnsafe)]` and implementing `IStartupActivity` so ReSharper instantiates it automatically when a solution loads (the solution container only *creates* such components, it never *runs* them — all the work happens in the constructor). It walks up the directory tree from the solution file, finds every `*.AutoLoad.DotSettings` file, and registers each as an `XmlFileSettingsStorage` mounted on `SettingsStorageProvidersCollection`, watching each file for changes via `IFileSystemTracker`.
- **`OutputWindowNotifier.cs`** — Optional status reporting to the Visual Studio Output window. No-op (never throws) when the Output window service is unavailable, e.g. under Rider.
- **`ZoneMarker.cs`** — Boilerplate ReSharper zone marker. Declares a dependency on `IProjectModelZone` so the plugin is only active when the project model is loaded.

Priority is assigned as `SolutionShared × 0.9` per parent level, so files closer to the solution always win over ancestors (a regular solution-shared settings file takes precedence over all of them).

## Output window reporting

On every solution load, the plugin writes to the "Resharper Settings Autodiscovery" pane in the Visual Studio Output window (**View > Output**):

- one line per discovered settings file, e.g. `Loaded settings layer 'Team' from 'C:\RepoRoot\Team.AutoLoad.DotSettings' with priority 4.455`
- a summary per solution load: `Loaded 2 auto-discovered settings layer(s)` or `No '*.AutoLoad.DotSettings' settings files found in solution parent folders`

The pane is created on first use and keeps its content across solution loads. Reporting is best-effort: any failure to write to the pane is logged and dropped, never thrown.

## Logging

The plugin logs through a dedicated category, `ResharperSettings.Autodiscovery`:

- **Info** — component creation, each settings layer mounted (name, path, priority), and the per-solution-load summary.
- **Verbose/Trace** — every parent folder as it is scanned, with its computed layer priority.
- **Warn** — failures to read a folder or mount a settings file, with the full exception.

## Testing

### Build and install

```bash
dotnet build ReshSettingsDiscover.sln -c Release
package.cmd
```

`package.cmd` requires the `DATA_DIR` environment variable and `nuget.exe` on PATH. Visual Studio interop types come from the `Microsoft.VisualStudio.Interop` NuGet package, so no Visual Studio install is needed to build. It packs the plugin and drops the package into the local NuGet feed at `%DATA_DIR%\nuget\temp`. In Visual Studio, install or update it via **ReSharper > Extension Manager** (add that folder as a package source first, if not already done), then restart Visual Studio.

### Manual test steps

1. Create a test layout:

   ```
   RepoRoot\
       Team.AutoLoad.DotSettings              <- shared settings to be picked up
       Src\
           Project\
               Project.sln                    <- solution opened in Visual Studio
               Project.AutoLoad.DotSettings   <- optional, to test priority
   ```

2. Open `Project.sln` in Visual Studio.
3. Go to **ReSharper > Manage Layers**: each discovered file should appear as a layer with the origin note "Automatically loaded from solution parent folder".
4. If both files exist, the one in the solution folder takes priority over the one at the repo root (priority decreases x0.9 per parent level).
5. Open **View > Output** and pick the Resharper Settings Autodiscovery pane: it prints one line per discovered settings layer as it is found (`Loaded settings layer '...' from '...' with priority ...`), followed by a summary per solution load: `Loaded 2 auto-discovered settings layer(s)` or `No '*.AutoLoad.DotSettings' settings files found in solution parent folders`.

### Checking the logs

Launch Visual Studio with logging enabled, e.g.:

```
devenv /ReSharper.LogFile "%TEMP%\JetLogs\resharper.log" /ReSharper.LogLevel Trace Project.sln
```

- `/ReSharper.LogLevel INFO` shows component creation, layer mounts, and the summary; use `VERBOSE` (or `TRACE`) to also see each folder walked.
- The log file lands in `%TEMP%\JetLogs` unless a different path is given to `/ReSharper.LogFile`.
- Filter the log for the `ResharperSettings.Autodiscovery` category to see only this plugin's output, e.g. expected messages:
  - `Solution settings autodiscovery component created`
  - `Scanning folder '...' (layer priority ...)` (verbose only)
  - `Loaded settings layer '...' from '...' with priority ...`
  - `No '*.AutoLoad.DotSettings' settings files found in solution parent folders` (when nothing is discovered)
- If no messages from the category appear at all, make sure the freshly packed version is actually installed: check the version in **ReSharper > Extension Manager** (it must be newer than any previously installed package), then restart Visual Studio.
