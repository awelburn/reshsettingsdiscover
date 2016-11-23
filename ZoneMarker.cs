using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ProjectModel;

namespace ReshSettingsDiscover
{
    [ZoneMarker]
    public class ZoneMarker : IZone, IRequire<IProjectModelZone>
    {
    }
}