namespace MSHU.CarWash.ClassLibrary
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
}
