import './index.css';
import ReactDOM from 'react-dom';
import { BrowserRouter } from 'react-router-dom';
import App from './App';
import Login from './Login';
import { runWithAdal } from './Auth';
import { AuthProvider } from './components/AuthProvider';

// if (!window.location.host.startsWith('www') && !window.location.host.startsWith('localhost')) {
//    window.location = `https://www.${window.location.host}${window.location.pathname}`;
// }

const baseUrl = document.getElementsByTagName('base')[0].getAttribute('href');
const rootElement = document.getElementById('root');

async function main() {
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
            <AuthProvider value={configuration}>
                <Login configuration={configuration} />
                <BrowserRouter basename={baseUrl}>
                    <App configuration={configuration} />
                </BrowserRouter>
            </AuthProvider>,
            rootElement
        );
    });
}

main();
