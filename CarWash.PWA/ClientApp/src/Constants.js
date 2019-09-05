export const State = Object.freeze({
    SubmittedNotActual: 0,
    ReminderSentWaitingForKey: 1,
    CarKeyLeftAndLocationConfirmed: 2,
    WashInProgress: 3,
    NotYetPaid: 4,
    Done: 5,
});

export const Service = Object.freeze({
    Exterior: 0,
    Interior: 1,
    Carpet: 2,
    SpotCleaning: 3,
    VignetteRemoval: 4,
    Polishing: 5,
    AcCleaningOzon: 6,
    AcCleaningBomba: 7,
    // below are those services that are hidden from the user
    BugRemoval: 8,
    WheelCleaning: 9,
    TireCare: 10,
    LeatherCare: 11,
    PlasticCare: 12,
    PreWash: 13,
});

export const NotificationChannel = Object.freeze({
    NotSet: 0,
    Disabled: 1,
    Email: 2,
    Push: 3,
});

export const Garages = Object.freeze({
    M: ['-1', '-2', '-2.5', '-3', '-3.5', 'outdoor'],
    S1: ['-1', '-2', '-3'],
    GS: ['-1', 'outdoor'],
    HX: ['-3'],
});

export const BacklogHubMethods = Object.freeze({
    ReservationCreated: 'ReservationCreated',
    ReservationUpdated: 'ReservationUpdated',
    ReservationDeleted: 'ReservationDeleted',
    ReservationDropoffConfirmed: 'ReservationDropoffConfirmed',
});

export function getStateName(state) {
    switch (state) {
        case State.SubmittedNotActual:
            return 'Scheduled';
        case State.ReminderSentWaitingForKey:
            return 'Leave the key at reception';
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

export function getServiceName(service) {
    switch (service) {
        case Service.Exterior:
            return 'exterior';
        case Service.Interior:
            return 'interior';
        case Service.Carpet:
            return 'carpet';
        case Service.SpotCleaning:
            return 'spot cleaning';
        case Service.VignetteRemoval:
            return 'vignette removal';
        case Service.Polishing:
            return 'polishing';
        case Service.AcCleaningOzon:
            return "AC cleaning 'ozon'";
        case Service.AcCleaningBomba:
            return "AC cleaning 'bomba'";
        case Service.BugRemoval:
            return 'bug removal';
        case Service.WheelCleaning:
            return 'wheel cleaning';
        case Service.TireCare:
            return 'tire care';
        case Service.LeatherCare:
            return 'leather care';
        case Service.PlasticCare:
            return 'plastic care';
        case Service.PreWash:
            return 'prewash';
        default:
            return 'no info';
    }
}

export function getServiceList() {
    return [
        {
            id: Service.Exterior,
            name: getServiceName(Service.Exterior),
            price: 3213,
            priceMpv: 4017,
            description: null,
        },
        {
            id: Service.Interior,
            name: getServiceName(Service.Interior),
            price: 1607,
            priceMpv: 2410,
            description: null,
        },
        {
            id: Service.Carpet,
            name: getServiceName(Service.Carpet),
            price: -1,
            priceMpv: -1,
            description: 'whole carpet cleaning, including all the seats',
        },
        {
            id: Service.SpotCleaning,
            name: getServiceName(Service.SpotCleaning),
            price: 3534,
            priceMpv: 3534,
            description: 'partial cleaning of the carpet, only where it is needed (eg. when something is spilled in the car)',
        },
        {
            id: Service.VignetteRemoval,
            name: getServiceName(Service.VignetteRemoval),
            price: 466,
            priceMpv: 466,
            description: 'eg. highway vignettes on the windscreen',
        },
        {
            id: Service.Polishing,
            name: getServiceName(Service.Polishing),
            price: 4498,
            priceMpv: 4498,
            description: 'for small scratches',
        },
        {
            id: Service.AcCleaningOzon,
            name: getServiceName(Service.AcCleaningOzon),
            price: 8033,
            priceMpv: 8033,
            description: 'disinfects molecules with ozone',
        },
        {
            id: Service.AcCleaningBomba,
            name: getServiceName(Service.AcCleaningBomba),
            price: 6426,
            priceMpv: 6426,
            description: 'blowing chemical spray in the AC system',
        },
        {
            id: Service.PreWash,
            name: getServiceName(Service.PreWash),
            price: 804,
            priceMpv: 804,
            description: "we'll add this if it's needed",
        },
        {
            id: Service.TireCare,
            name: getServiceName(Service.TireCare),
            price: 804,
            priceMpv: 804,
            description: "we'll add this if it's needed",
        },
    ];
}
