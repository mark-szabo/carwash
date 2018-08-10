import React, { Component } from 'react';
import { adalFetch } from '../Auth';
import PropTypes from 'prop-types';
import { withStyles } from '@material-ui/core/styles';
import Stepper from '@material-ui/core/Stepper';
import Step from '@material-ui/core/Step';
import StepLabel from '@material-ui/core/StepLabel';
import StepContent from '@material-ui/core/StepContent';
import Button from '@material-ui/core/Button';
import Chip from '@material-ui/core/Chip';
import Typography from '@material-ui/core/Typography';

const styles = theme => ({
    stepper: {
        padding: 0,
        backgroundColor: 'inherit',
    },
    button: {
        marginTop: theme.spacing.unit,
        marginRight: theme.spacing.unit,
    },
    actionsContainer: {
        marginTop: theme.spacing.unit,
        marginBottom: theme.spacing.unit * 2,
    },
    resetContainer: {
        padding: theme.spacing.unit * 3,
    },
    chip: {
        margin: theme.spacing.unit / 2,
    },
    selectedChip: {
        margin: theme.spacing.unit / 2,
        backgroundColor: theme.palette.primary.main,
        '&:hover': {
            backgroundColor: theme.palette.primary.dark,
        },
        '&:focus': {
            backgroundColor: theme.palette.primary.main,
        },
        '&:hover:focus': {
            backgroundColor: theme.palette.primary.dark,
        },
    }
});

class Reserve extends Component {
    displayName = Reserve.name

    constructor(props) {
        super(props);
        this.state = {
            activeStep: 0,
            obfuscatedReservations: [],
            loading: true,
            services: [
                { id: 0, name: 'exterior', selected: false },
                { id: 1, name: 'interior', selected: false },
                { id: 2, name: 'carpet', selected: false },
                { id: 3, name: 'spot cleaning', selected: false },
                { id: 4, name: 'vignette removal', selected: false },
                { id: 5, name: 'polishing', selected: false },
                { id: 6, name: 'AC cleaning \'ozon\'', selected: false },
                { id: 7, name: 'AC cleaning \'bomba\'', selected: false }
            ],
            garages: {
                M: [
                    '-1',
                    '-2',
                    '-2.5',
                    '-3',
                    '-3.5',
                    'outdoor'
                ],
                G: [
                    '-1',
                    'outdoor'
                ],
                S1: [
                    '-1',
                    '-2',
                    '-3'
                ]
            }
        };
    }

    handleNext = () => {
        this.setState(state => ({
            activeStep: state.activeStep + 1,
        }));
    };

    handleBack = () => {
        this.setState(state => ({
            activeStep: state.activeStep - 1,
        }));
    };

    handleServiceChipClick = service => () => {
        this.setState(state => {
            const services = [...state.services];
            services[service.id].selected = !services[service.id].selected;

            // if carpet, must include exterior and interior too
            if (service.id === 2 && service.selected) {
                services[0].selected = true;
                services[1].selected = true;
            }
            if ((service.id === 0 || service.id === 1) && !service.selected) {
                services[2].selected = false;
            }

            return { services };
        });
    };

    componentDidMount() {
        adalFetch('api/reservations/obfuscated')
            .then(response => response.json())
            .then(data => {
                this.setState({ obfuscatedReservations: data, loading: false });
            });
    }

    render() {
        const { classes } = this.props;
        const { activeStep } = this.state;

        return (
            <Stepper activeStep={activeStep} orientation="vertical" className={classes.stepper}>
                <Step>
                    <StepLabel>Select services</StepLabel>
                    <StepContent>
                        {this.state.services.map(service =>
                            <div>
                                {service.id === 0 && (<Typography variant="body2" gutterBottom>Basic</Typography>)}
                                {service.id === 3 && (<Typography variant="body2" gutterBottom>Extras</Typography>)}
                                {service.id === 6 && (<Typography variant="body2" gutterBottom>AC</Typography>)}
                                <Chip
                                    key={service.id}
                                    label={service.name}
                                    onClick={this.handleServiceChipClick(service)}
                                    className={service.selected ? classes.selectedChip : classes.chip}
                                />
                                {service.id === 2 && (<br/>)}
                                {service.id === 5 && (<br/>)}
                            </div>
                        )}
                        <div className={classes.actionsContainer}>
                            <div>
                                <Button
                                    disabled={true}
                                    className={classes.button}
                                >Back</Button>
                                <Button
                                    variant="contained"
                                    color="primary"
                                    onClick={this.handleNext}
                                    className={classes.button}
                                    disabled={this.state.services.filter(service => service.selected === true).length > 0}
                                >Next</Button>
                            </div>
                        </div>
                    </StepContent>
                </Step>
                <Step>
                    <StepLabel>Choose date</StepLabel>
                    <StepContent>
                        <div className={classes.actionsContainer}>
                            <div>
                                <Button
                                    onClick={this.handleBack}
                                    className={classes.button}
                                >Back</Button>
                                <Button
                                    variant="contained"
                                    color="primary"
                                    onClick={this.handleNext}
                                    className={classes.button}
                                >Next</Button>
                            </div>
                        </div>
                    </StepContent>
                </Step>
                <Step>
                    <StepLabel>Reserve</StepLabel>
                    <StepContent>
                        <div className={classes.actionsContainer}>
                            <div>
                                <Button
                                    onClick={this.handleBack}
                                    className={classes.button}
                                >Back</Button>
                                <Button
                                    variant="contained"
                                    color="primary"
                                    onClick={this.handleNext}
                                    className={classes.button}
                                >Finish</Button>
                            </div>
                        </div>
                    </StepContent>
                </Step>
            </Stepper>
            //activeStep === steps.length && (
            //    <Paper square elevation={0} className={classes.resetContainer}>
            //        <Typography>All steps completed - you&quot;re finished</Typography>
            //        <Button onClick={this.handleReset} className={classes.button}>
            //            Reset
            //        </Button>
            //    </Paper>
            //)
        );
    }
}

Reserve.propTypes = {
    classes: PropTypes.object.isRequired,
};

export default withStyles(styles)(Reserve);