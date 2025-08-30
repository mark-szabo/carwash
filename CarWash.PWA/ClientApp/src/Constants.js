export const State = Object.freeze({
    SubmittedNotActual: 0,
    ReminderSentWaitingForKey: 1,
    CarKeyLeftAndLocationConfirmed: 2,
    WashInProgress: 3,
    NotYetPaid: 4,
    Done: 5,
});

/**
 * Only those services are marked here which have some special front-end ruling (like only one type of AC cleaning can be selected).
 * @deprecated
 */
export const Service = Object.freeze({
    Exterior: 0,
    Interior: 1,
    Carpet: 2,
    AcCleaningOzon: 6,
    AcCleaningBomba: 7,
    Prewash: 13,
    WheelCleaning: 9,
});

export const NotificationChannel = Object.freeze({
    NotSet: 0,
    Disabled: 1,
    Email: 2,
    Push: 3,
});

export const Severity = Object.freeze({
    Error: 0,
    Warning: 1,
    Info: 2,
    Success: 3,
});

export const PaymentMethod = Object.freeze({
    NotSet: 0,
    CreditCard: 1,
    WireTransfer: 2,
});

export const BacklogHubMethods = Object.freeze({
    ReservationCreated: 'ReservationCreated',
    ReservationUpdated: 'ReservationUpdated',
    ReservationDeleted: 'ReservationDeleted',
    ReservationDropoffConfirmed: 'ReservationDropoffConfirmed',
    ReservationChatMessageSent: 'ReservationChatMessageSent',
});

export const KeyLockerHubMethods = Object.freeze({
    KeyLockerBoxOpened: 'KeyLockerBoxOpened',
    KeyLockerBoxClosed: 'KeyLockerBoxClosed',
});

export function getStateName(state) {
    switch (state) {
        case State.SubmittedNotActual:
            return 'Scheduled';
        case State.ReminderSentWaitingForKey:
            return 'Drop-off the key';
        case State.CarKeyLeftAndLocationConfirmed:
            return 'All set, ready to wash';
        case State.WashInProgress:
            return 'Wash in progress';
        case State.NotYetPaid:
            return 'You need to pay';
        case State.Done:
            return 'Completed';
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
            return 'Completed';
        default:
            return 'No info';
    }
}

export function getServiceName(configuration, serviceId) {
    return configuration.services.find(s => s.id === serviceId)?.name;
}
