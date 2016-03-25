using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace MSHU.CarWash.UWP.ViewModels
{
    /// <summary>
    /// About View Model
    /// </summary>
    public class AboutViewModel : BaseViewModel
    {
        public string PackageInformation
        {
            get
            {
                //set the package information
                Package package = Package.Current;
                PackageId packageId = package.Id;
                string output = string.Format("Name: \"{0}\"\n" +
                                              "Version: {1}\n" +
                                              "Architecture: {2}\n",
                                              
                                              packageId.Name,
                                              GenerateVersionString(packageId.Version),
                                              GenerateArchitectureString(packageId.Architecture)
                                              );
                return output;
            }
        }

        /// <summary>
        /// Create version string
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        private string GenerateVersionString(PackageVersion version)
        {
            return String.Format("{0}.{1}.{2}.{3}",
                                 version.Major, version.Minor, version.Build, version.Revision);
        }

        /// <summary>
        /// Returns the architecture string
        /// </summary>
        /// <param name="architecture"></param>
        /// <returns></returns>
        private string GenerateArchitectureString(Windows.System.ProcessorArchitecture architecture)
        {
            switch (architecture)
            {
                case Windows.System.ProcessorArchitecture.X86:
                    return "x86";
                case Windows.System.ProcessorArchitecture.Arm:
                    return "arm";
                case Windows.System.ProcessorArchitecture.X64:
                    return "x64";
                case Windows.System.ProcessorArchitecture.Neutral:
                    return "neutral";
                case Windows.System.ProcessorArchitecture.Unknown:
                    return "unknown";
                default:
                    return "???";
            }
        }

    }
}
