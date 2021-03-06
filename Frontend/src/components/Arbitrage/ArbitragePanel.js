﻿import React, { Component } from 'react';
import './Arbitrage.css';

export class ArbitragePanel extends Component {
    displayName = ArbitragePanel.name

    constructor(props) {
        super(props);

        this.state = {
            error: null,
            exchange: props.exchange,
            path: props.path,
            profit: props.profit,
            transactionFee: props.transactionFee,
        };
    }

    componentWillReceiveProps(nextProps) {
        this.setState({
            exchange: nextProps.exchange,
            path: nextProps.path,
            profit: nextProps.profit,
            transactionFee: nextProps.transactionFee,
        });
    }

    render() {
        const { error } = this.state;

        if (error) {
            return <div className="arbitragePanel darkerContainer">Error: {error.message}</div>;
        } else {
            return (
                <div className="arbitragePanel darkerContainer">
                    Exchange: {this.state.exchange}<br/>
                    Path: {this.state.path}<br />
                    Profit: {this.state.profit}%<br />
                    Transaction Fee: {this.state.transactionFee}%<br />
                </div>
            );
        }
    }
}