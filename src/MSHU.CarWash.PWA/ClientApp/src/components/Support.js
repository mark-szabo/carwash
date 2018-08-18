import React, { Component } from 'react';
import Button from '@material-ui/core/Button';

export default class Support extends Component {
    displayName = Support.name

    render() {
        return (
            <div>
                <p>If you experience any problem with the app, please contact support on <a href="mailto:carwashapp@microsoft.com" className="underline">carwashapp@microsoft.com</a>!</p>
                <Button href="mailto:carwashapp@microsoft.com" variant="contained" color="primary">Contact support</Button>
            </div>
        );
    }
}
