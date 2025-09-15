using CarWash.ClassLibrary.Enums;
using CarWash.ClassLibrary.Models;

namespace CarWash.ClassLibrary.Models.ViewModels
{
    /// <summary>
    /// View model for users
    /// </summary>
    public record UserViewModel(
        string Id,
        string FirstName,
        string LastName,
        string Company,
        string Email,
        string PhoneNumber,
        string BillingName,
        string BillingAddress,
        PaymentMethod PaymentMethod,
        bool IsAdmin,
        bool IsCarwashAdmin,
        bool CalendarIntegration,
        NotificationChannel NotificationChannel)
    {
        public UserViewModel(User user) : this(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Company,
            user.Email,
            user.PhoneNumber,
            user.BillingName,
            user.BillingAddress,
            user.PaymentMethod,
            user.IsAdmin,
            user.IsCarwashAdmin,
            user.CalendarIntegration,
            user.NotificationChannel)
        { }
    }
}