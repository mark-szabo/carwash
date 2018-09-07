import React, { Component } from 'react';
import './RoadAnimation.css';

export default class RoadAnimation extends Component {
    displayName = RoadAnimation.name;

    render() {
        return (
            <div className="scene">
                <div className="inner-scene">
                    <div className="road">
                        <div className="item stripe" />
                        <div className="item plant" />
                        <div className="item plant" />
                        <div className="item plant" />
                        <div className="item plant" />
                        <div className="item plant" />
                        <div className="item plant" />
                    </div>
                    <div className="road">
                        <div className="item stripe" />
                        <div className="item plant" />
                        <div className="item plant" />
                        <div className="item plant" />
                        <div className="item plant" />
                        <div className="item plant" />
                        <div className="item plant" />
                    </div>
                    <div className="item car" />
                </div>
            </div>
        );
    }
}
