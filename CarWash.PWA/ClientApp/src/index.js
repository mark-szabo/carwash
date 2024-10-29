import './index.css';
import React from 'react';
import ReactDOM from 'react-dom';
import { BrowserRouter } from 'react-router-dom';
import App from './App';
import registerServiceWorker from './registerServiceWorker';
import { runWithAdal } from './Auth';

const baseUrl = document.getElementsByTagName('base')[0].getAttribute('href');
const rootElement = document.getElementById('root');

let configuration;
try {
    const o = {};
    o.headers = new Headers();
    o.headers.append('Content-Type', 'application/json');

    const response = await window.fetch('api/.well-known/configuration', o);
    configuration = await response.json();
} catch (e) {
    console.error(`NETWORK ERROR: ${e.message}`);
}

runWithAdal(configuration, () => {
    ReactDOM.render(
        <BrowserRouter basename={baseUrl}>
            <App />
        </BrowserRouter>,
        rootElement
    );
});

registerServiceWorker();
