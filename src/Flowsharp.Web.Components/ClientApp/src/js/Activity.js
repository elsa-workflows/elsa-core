import React from 'react';
import dotnetify from 'dotnetify';

export default class Activity extends React.Component {
    constructor(props) {
        super(props);
        dotnetify.react.connect('Activity', this);
        this.state = { Title: 'Activity 1' };
    }

    render() {
        return (
            <div className="activity">
                <div>Content</div>
            </div>
        );
    }
}