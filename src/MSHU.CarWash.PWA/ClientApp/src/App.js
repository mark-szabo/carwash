import React, { Component } from 'react';
import { Route } from 'react-router';
import { MuiThemeProvider, createMuiTheme } from '@material-ui/core/styles';
import Layout from './components/Layout';
import { Home } from './components/Home';
import { Support } from './components/Support';
import { FetchData } from './components/FetchData';
import { Counter } from './components/Counter';

// A theme with custom primary and secondary color.
const theme = createMuiTheme({
    palette: {
        primary: {
            light: '#b5ffff',
            main: '#80d8ff',
            dark: '#49a7cc',
        },
        secondary: {
            light: '#b5ffff',
            main: '#80d8ff',
            dark: '#49a7cc',
        },
    },
    typography: {
        fontFamily: [
            '"Segoe UI"',
            'Roboto',
            '"Helvetica Neue"',
            'Arial',
            'sans-serif',
        ].join(','),
    },
});

export default class App extends Component {
    displayName = App.name

    render() {
        return (
            <MuiThemeProvider theme={theme}>
                <Layout>
                    <Route exact path="/" component={Home} navbarName="My reservations" />
                    <Route exact path="/index.html" component={Home} navbarName="My reservations" />
                    <Route path="/support" component={Support} navbarName="Support" />
                    <Route path="/counter" component={Counter} navbarName="Counter" />
                    <Route path="/fetchdata" component={FetchData} navbarName="Fetch data" />
                </Layout>
            </MuiThemeProvider>
        );
    }
}
