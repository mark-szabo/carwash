using CarWash.ClassLibrary.Models;

namespace CarWash.PWA.ViewModels
{
    /// <summary>
    /// ViewModel for a key locker box
    /// </summary>
    public record KeyLockerBoxViewModel(string BoxId, int BoxSerial, string Building, string Floor, string Name)
    {
        /// <summary>
        /// ViewModel for a key locker box response.
        /// </summary>
        /// <param name="box"></param>
        public KeyLockerBoxViewModel(KeyLockerBox box) : this(
            box.Id,
            box.BoxSerial,
            box.Building,
            box.Floor,
            box.Name)
        { }
    }
}