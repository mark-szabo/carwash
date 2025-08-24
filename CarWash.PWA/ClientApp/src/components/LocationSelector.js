import PropTypes from 'prop-types';
import { withStyles } from '@mui/styles';
import InputLabel from '@mui/material/InputLabel';
import FormControl from '@mui/material/FormControl';
import TextField from '@mui/material/TextField';
import MenuItem from '@mui/material/MenuItem';
import Select from '@mui/material/Select';

const styles = theme => ({
    formControl: {
        marginRight: theme.spacing(1),
        marginBottom: theme.spacing(1),
        marginTop: theme.spacing(2),
        [theme.breakpoints.down('md')]: {
            width: '100%',
        },
    },
});

const DEFAULT_WIDTH = 176;

function LocationSelector({
    classes,
    configuration,
    garage,
    floor,
    spot,
    validationErrors,
    onGarageChange,
    onFloorChange,
    onSpotChange,
    textFieldWidth,
}) {
    return (
        <>
            <FormControl
                className={classes.formControl}
                error={validationErrors.garage}
                sx={{
                    width: {
                        md: textFieldWidth || DEFAULT_WIDTH,
                        lg: textFieldWidth || DEFAULT_WIDTH,
                        xl: textFieldWidth || DEFAULT_WIDTH,
                    },
                }}
            >
                <InputLabel id="garageLabel">Building</InputLabel>
                <Select
                    id="garage"
                    labelId="garageLabel"
                    label="Building"
                    required
                    value={garage}
                    onChange={onGarageChange}
                    inputProps={{
                        name: 'garage',
                        id: 'garage',
                    }}
                >
                    {configuration.garages.map(g => (
                        <MenuItem key={g.building} value={g.building}>
                            {g.building}
                        </MenuItem>
                    ))}
                </Select>
            </FormControl>
            {garage && configuration.garages.some(g => g.building === garage) && (
                <FormControl
                    className={classes.formControl}
                    error={validationErrors.floor}
                    sx={{
                        width: {
                            md: textFieldWidth || DEFAULT_WIDTH,
                            lg: textFieldWidth || DEFAULT_WIDTH,
                            xl: textFieldWidth || DEFAULT_WIDTH,
                        },
                    }}
                >
                    <InputLabel id="floorLabel">Floor</InputLabel>
                    <Select
                        id="floor"
                        labelId="floorLabel"
                        label="Floor"
                        required
                        value={floor}
                        onChange={onFloorChange}
                        inputProps={{
                            name: 'floor',
                            id: 'floor',
                        }}
                    >
                        {configuration.garages
                            .find(g => g.building === garage)
                            .floors.map(f => (
                                <MenuItem value={f} key={f}>
                                    {f}
                                </MenuItem>
                            ))}
                    </Select>
                </FormControl>
            )}
            {floor && (
                <TextField
                    id="spot"
                    label="Spot (optional)"
                    value={spot}
                    className={classes.formControl}
                    margin="normal"
                    onChange={onSpotChange}
                    sx={{
                        width: {
                            md: textFieldWidth || DEFAULT_WIDTH,
                            lg: textFieldWidth || DEFAULT_WIDTH,
                            xl: textFieldWidth || DEFAULT_WIDTH,
                        },
                    }}
                />
            )}
        </>
    );
}

LocationSelector.propTypes = {
    classes: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    configuration: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    garage: PropTypes.string.isRequired,
    floor: PropTypes.string.isRequired,
    spot: PropTypes.string.isRequired,
    validationErrors: PropTypes.shape({
        garage: PropTypes.bool,
        floor: PropTypes.bool,
    }).isRequired,
    onGarageChange: PropTypes.func.isRequired,
    onFloorChange: PropTypes.func.isRequired,
    onSpotChange: PropTypes.func.isRequired,
    textFieldWidth: PropTypes.number,
};

export default withStyles(styles)(LocationSelector);
