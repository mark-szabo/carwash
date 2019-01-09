namespace MSHU.CarWash.ClassLibrary.Enums
{
    public enum State
    {
        SubmittedNotActual = 0,
        ReminderSentWaitingForKey = 1,
        DropoffAndLocationConfirmed = 2,
        WashInProgress = 3,
        NotYetPaid = 4,
        Done = 5
    }

    public static class StateExtensions
    {
        public static string ToFriendlyString(this State state)
        {
            switch (state)
            {
                case State.SubmittedNotActual:
                    return "Scheduled";
                case State.ReminderSentWaitingForKey:
                    return "Leave the key at reception";
                case State.DropoffAndLocationConfirmed:
                    return "Waiting";
                case State.WashInProgress:
                    return "Wash in progress";
                case State.NotYetPaid:
                    return "You need to pay";
                case State.Done:
                    return "Done";
                default:
                    return "No info";
            }
        }
    }
}
