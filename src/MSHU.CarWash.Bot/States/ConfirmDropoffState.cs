namespace MSHU.CarWash.Bot.States
{
    /// <summary>
    /// State properties ('conversation' scope) for drop-off confirmation.
    /// </summary>
    public class ConfirmDropoffState
    {
        public string ReservationId { get; set; }

        public string Building { get; set; }

        public string Floor { get; set; }

        public string Seat { get; set; }
    }
}
