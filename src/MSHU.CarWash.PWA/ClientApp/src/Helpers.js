import * as moment from 'moment';

/**
 * Format location for display
 * @param {string} location car location
 * @returns {string} location in the format like 'M/-2' or 'M/-2/125'
 */
export function formatLocation(location) {
    if (!location) return null;

    const [garage, floor, seat] = location.split('/');
    if (!garage || !floor) return null;

    if (seat) return `${garage}/${floor}/${seat}`;
    return `${garage}/${floor}`;
}

/**
 * Format reservation date for display
 * @param {Reservation} reservation reservation
 * @returns {string} date in the format like '2:00 PM - 5:00 PM • September 21'
 */
export function formatDate(reservation) {
    const startTime = moment(reservation.startDate).format('h:mm A');
    const endTime = moment(reservation.endDate).format('h:mm A');
    const date = moment(reservation.startDate).format('MMMM D');

    return `${startTime} - ${endTime} • ${date}`;
}

/**
 * Format reservation date for display
 * @param {Reservation} reservation reservation
 * @returns {string} date in the format like 'September 21, 2:00 PM - 5:00 PM'
 */
export function formatDate2(reservation) {
    const startTime = moment(reservation.startDate).format('MMMM D, h:mm A');
    const endTime = moment(reservation.endDate).format('h:mm A');

    return `${startTime} - ${endTime}`;
}
