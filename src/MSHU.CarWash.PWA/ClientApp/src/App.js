import React, { Component } from 'react';
import { Route } from 'react-router';
import { MuiThemeProvider, createMuiTheme } from '@material-ui/core/styles';
import Layout from './components/Layout';
import Home from './components/Home';
import Reserve from './components/Reserve';
import { Support } from './components/Support';

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
            'sans-serif'
        ].join(','),
    },
});

export default class App extends Component {
    displayName = App.name

    render() {
        const { user } = this.props;
        return (
            <MuiThemeProvider theme={theme}>
                <Layout user={user}>
                        <Route
                            exact
                            path="/"
                            navbarName="My reservations"
                            render={props => <Home user={user} {...props} />}
                        />
                        <Route
                            exact
                            path="/reserve"
                            navbarName="Reserve"
                            render={props => <Reserve user={user} {...props} />}
                        />
                        <Route
                            exact
                            path="/support"
                            navbarName="Support"
                            component={Support}
                        />
                </Layout>
            </MuiThemeProvider>
        );
    }
}
