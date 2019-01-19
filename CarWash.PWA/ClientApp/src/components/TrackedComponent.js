import React from 'react';
import { AppInsights } from 'applicationinsights-js';

export default class TrackedComponent extends React.Component {
    displayName = 'TrackedComponent';

    componentDidMount() {
        this.componentDidMountTimestamp = Date.now();
        try {
            AppInsights.trackPageView(
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
            AppInsights.trackMetric(
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
