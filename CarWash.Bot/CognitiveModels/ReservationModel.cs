using System.Collections.Generic;

namespace CarWash.Bot.CognitiveModels
{
    internal class ReservationModel
    {
        internal CognitiveModel VehiclePlateNumber { get; set; }

        internal LocationModel Location { get; set; }

        internal CognitiveModel Private { get; set; }

        internal CognitiveModel Comment { get; set; }

        internal List<ServiceModel> Services { get; set; } = new List<ServiceModel>();

        internal DateTimeModel StartDate { get; set; }
    }
}
