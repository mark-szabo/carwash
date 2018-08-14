import './index.css';
import React from 'react';
import ReactDOM from 'react-dom';
import { BrowserRouter } from 'react-router-dom';
import App from './App';
import registerServiceWorker from './registerServiceWorker';
import { runWithAdal } from './Auth';

const baseUrl = document.getElementsByTagName('base')[0].getAttribute('href');
const rootElement = document.getElementById('root');

runWithAdal((user) => {

    ReactDOM.render(
        <BrowserRouter basename={baseUrl}>
            <App user={user}/>
        </BrowserRouter>,
        rootElement);

});


registerServiceWorker();
