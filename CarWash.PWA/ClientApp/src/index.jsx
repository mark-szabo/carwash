import './index.css';
import ReactDOM from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import App from './App';
import { runWithAdal } from './Auth';

// if (!window.location.host.startsWith('www') && !window.location.host.startsWith('localhost')) {
//    window.location = `https://www.${window.location.host}${window.location.pathname}`;
// }

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
    const root = ReactDOM.createRoot(rootElement);
    root.render(
        <BrowserRouter>
            <App configuration={configuration} />
        </BrowserRouter>
    );
});
