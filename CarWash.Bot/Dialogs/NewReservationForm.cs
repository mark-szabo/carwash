#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1401 // Fields should be private

using System;
using System.Collections.Generic;
using Bot.Builder.Community.Dialogs.FormFlow;
using CarWash.ClassLibrary.Enums;

namespace CarWash.Bot.Dialogs
{
    public class NewReservationForm
    {
        public string VehiclePlateNumber;
        public List<ServiceType> Services;
        public bool Private;
        public DateTime StartDate;
        public string Comment;

        public static IForm<NewReservationForm> BuildForm()
        {
            return new FormBuilder<NewReservationForm>()
                    .Message("Welcome to the reservation form!")
                    .Field(nameof(Services))
                    .Field(nameof(StartDate))
                    .Field(nameof(VehiclePlateNumber))
                    .Field(nameof(Private))
                    .Field(nameof(Comment))
                    .Build();
        }
    }
}
