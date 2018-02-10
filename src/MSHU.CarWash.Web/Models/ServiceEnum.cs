using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Web;

namespace MSHU.CarWash.Models
{
    public enum ServiceEnum
    {
        [Description("EXTERIOR")]
        Exterior = 0,

        [Description("INTERIOR")]
        Interior = 1,

        [Description("EXTERIOR + INTERIOR")]
        ExteriorInterior = 2,

        [Description("EXTERIOR + INTERIOR + CARPET")]
        ExteriorInteriorCarpet = 3,

        [Description("EXTERIOR (STEAM)")]
        ExteriorSteam = 4,

        [Description("INTERIOR (STEAM)")]
        InteriorSteam = 5,

        [Description("EXTERIOR + INTERIOR (STEAM)")]
        ExteriorInteriorSteam = 6
    }
}