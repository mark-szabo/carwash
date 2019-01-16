import React from 'react';
import { withStyles } from '@material-ui/core/styles';

const styles = {
    powerbi: {
        margin: -24,
        width: 'calc(100% + 48px)',
        height: 'calc(100% + 48px)',
    },
};

class Analytics extends React.PureComponent {
    displayName = 'Analytics';

    componentDidMount() {
        document.getElementsByTagName('main')[0].style.overflow = 'hidden';
    }

    componentWillUnmount() {
        document.getElementsByTagName('main')[0].style.overflow = 'auto';
    }

    render() {
        const { classes } = this.props;

        return (
            <iframe
                title="Power BI"
                src="https://msit.powerbi.com/reportEmbed?reportId=55872423-9855-48ee-a4ae-50c3ccb5918e&autoAuth=true"
                frameBorder="0"
                allowFullScreen
                className={classes.powerbi}
            />
        );
    }
}

export default withStyles(styles)(Analytics);
