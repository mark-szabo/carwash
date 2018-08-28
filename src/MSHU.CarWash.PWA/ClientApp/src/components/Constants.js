export const State = Object.freeze({
    SubmittedNotActual: 0,
    ReminderSentWaitingForKey: 1,
    CarKeyLeftAndLocationConfirmed: 2,
    WashInProgress: 3,
    NotYetPaid: 4,
    Done: 5,
});

export function getStateName(state) {
    switch (state) {
        case State.SubmittedNotActual:
            return 'Scheduled';
        case State.ReminderSentWaitingForKey:
            return 'Leave the key at reception';
        case State.CarKeyLeftAndLocationConfirmed:
            return 'Waiting';
        case State.WashInProgress:
            return 'Wash in progress';
        case State.NotYetPaid:
            return 'You need to pay';
        case State.Done:
            return 'Done';
        default:
            return 'No info';
    }
}

export function getAdminStateName(state) {
    switch (state) {
        case State.SubmittedNotActual:
            return 'Scheduled';
        case State.ReminderSentWaitingForKey:
            return 'Waiting for key';
        case State.CarKeyLeftAndLocationConfirmed:
            return 'Queued';
        case State.WashInProgress:
            return 'In progress';
        case State.NotYetPaid:
            return 'Waiting for payment';
        case State.Done:
            return 'Done';
        default:
            return 'No info';
    }
}

export function getServiceName(service) {
    switch (service) {
        case 0:
            return 'exterior';
        case 1:
            return 'interior';
        case 2:
            return 'carpet';
        case 3:
            return 'spot cleaning';
        case 4:
            return 'vignette removal';
        case 5:
            return 'polishing';
        case 6:
            return "AC cleaning 'ozon'";
        case 7:
            return "AC cleaning 'bomba'";
        case 8:
            return 'bug removal';
        case 9:
            return 'wheel cleaning';
        case 10:
            return 'tire care';
        case 11:
            return 'leather care';
        case 12:
            return 'plastic care';
        case 13:
            return 'prewash';
        default:
            return 'no info';
    }
}
