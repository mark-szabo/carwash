import React from 'react';
import { ReactPlugin, withAITracking } from '@microsoft/applicationinsights-react-js';

var reactPlugin = new ReactPlugin();

class TrackedComponent extends React.Component {
    displayName = 'TrackedComponent';

    componentDidMount() {
        this.componentDidMountTimestamp = Date.now();
        try {
            appInsights.trackPageView(
                this.displayName, // name
                null, // url
                null, // properties
                null, // measurements
                null // duration
            );
        } catch (error) {
            console.error(`AppInsights is not loaded: ${error}`);
        }
    }

    componentWillUnmount() {
        if (!this.componentDidMountTimestamp) {
            throw new Error('componentDidMountTimestamp was not initialized. Check if super.componentDidMount() was called');
        }
        try {
            appInsights.trackMetric(
                `${this.displayName} component engagement time (seconds)`, // name
                (Date.now() - this.componentDidMountTimestamp) / 1000, // average
                1, // sampleCount
                null, // min
                null, // max
                null // properties
            );
        } catch (error) {
            console.error(`AppInsights is not loaded: ${error}`);
        }
    }

    render() {
        return false;
    }
}

export default withAITracking(reactPlugin, TrackedComponent);
