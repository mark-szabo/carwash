export function formatLocation(location) {
    if (!location) return null;

    const [garage, floor, seat] = location.split('/');
    if (!garage || !floor) return null;

    if (seat) return `${garage}/${floor}/${seat}`;
    return `${garage}/${floor}`;
}
