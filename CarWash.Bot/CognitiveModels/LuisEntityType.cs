#pragma warning disable SA1602 // Enumeration items should be documented

namespace CarWash.Bot.CognitiveModels
{
    /// <summary>
    /// Types of LUIS entities for recognizing information entities in the user's messages.
    /// </summary>
    internal enum LuisEntityType
    {
        Service,
        DateTime,
        Comment,
        Building,
        Floor,
        Seat,
        Private,
        VehiclePlateNumber,
        WeatherLocation,
    }
}
