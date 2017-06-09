using MSHU.CarWash.UWP.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.UI.Popups;

namespace MSHU.CarWash.UWP.Services
{
    class WhatsNewService : IWhatsNew
    {
        private const string whatsNewKey = "WhatsNewSeenForAppVersion";
        private ISettingsStore settingsStore;

        public WhatsNewService(ISettingsStore settingsStore)
        {
            this.settingsStore = settingsStore;
        }
        public Task FetchWhatsNewAsync()
        {
            return Task.CompletedTask;
        }

        public async Task<bool> IsSomethingNewAsync()
        {
            var lastKnownVersion = await settingsStore.TryRetrieveSettingAsync<uint?>(whatsNewKey);
            if (lastKnownVersion != null)
            {
                var version = Package.Current.Id.Version;
                return VersionToUInt(version) > lastKnownVersion;
            }

            return true;
        }

        private static uint VersionToUInt(PackageVersion version)
        {
            return (uint)((version.Major << 8*3) + (version.Minor << 8*2) + (version.Build << 8*1) + version.Revision);
        }

        private static uint VersionToUInt(ushort Major, ushort Minor, ushort Build = 0, ushort Revision = 0)
        {
            return (uint)((Major << 8*3) + (Minor << 8*2) + (Build << 8*1) + Revision);
        }

        public async Task ShowWhatsNewAsync()
        {
            string message = await GetWhatsNewMessageAsync();

            if (!string.IsNullOrWhiteSpace(message))
            {
                var dialog = new MessageDialog(message);
                dialog.Title = "Carwash has just got updated!";
                dialog.Commands.Add(new UICommand { Label = "Ok", Id = 0 });

                var dialogDisplayed = true;
                try
                {
                    var res = await dialog.ShowAsync();
                }
                catch (UnauthorizedAccessException)
                {
                    // happens if a message dialog is already being displayed - not sure why it could happen
                    // let's postpone our dialog to the next time
                    dialogDisplayed = false;
                }

                if (dialogDisplayed)
                {
                    await settingsStore.StoreSettingAsync(whatsNewKey, VersionToUInt(Package.Current.Id.Version));
                }
            }
        }

        private async Task<string> GetWhatsNewMessageAsync()
        {
            var lastKnownVersion = await settingsStore.TryRetrieveSettingAsync<uint?>(whatsNewKey);
            if (lastKnownVersion == null)
            {
                lastKnownVersion = 0;
            }

            var version = VersionToUInt(Package.Current.Id.Version);

            var changes = new[]
            {
                new { version = VersionToUInt(1, 4, 7), changes = new [] { "Support for managing car wash reservations in your calendar." } },
                new { version = VersionToUInt(1, 4, 8), changes = new [] { "Get informed about what's new in the app." } }
            };

            var changeList = new StringBuilder("Here's what's new:\r\n");
            var changesAppended = false;
            foreach (var versionChange in changes)
            {
                if (versionChange.version > lastKnownVersion)
                {
                    changesAppended = true;
                    foreach (var change in versionChange.changes)
                    {
                        changeList.AppendLine($"• {change}");
                    }
                }
            }

            if(!changesAppended)
            {
                return null;
            }
            var message = changeList.ToString();
            return message;
        }
    }
}
