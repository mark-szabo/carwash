import React, { useEffect } from 'react';
import { GoogleOAuthProvider, GoogleLogin, useGoogleOneTapLogin } from '@react-oauth/google';
import { runWithAdal } from './Auth';

const GOOGLE_CLIENT_ID =
    process.env.REACT_APP_GOOGLE_CLIENT_ID ||
    '671648737845-v6mijdiaui34trt49mf4hsf34ins6obq.apps.googleusercontent.com';

function handleMicrosoftLogin(configuration) {
    localStorage.setItem('authProvider', 'microsoft');
    runWithAdal(configuration, () => window.location.reload());
}

function handleGoogleLoginSuccess(credentialResponse) {
    localStorage.setItem('authProvider', 'google');
    // Send credentialResponse.credential (ID token) to backend for validation
    fetch('/signin-google', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ idToken: credentialResponse.credential }),
    }).then(() => window.location.reload());
}

function handleGoogleLoginError() {
    alert('Google login failed. Please try again.');
}

export default function Login({ configuration }) {
    useEffect(() => {
        // If user already chose a provider, auto-redirect
        const provider = localStorage.getItem('authProvider');
        if (provider === 'microsoft') {
            handleMicrosoftLogin(configuration);
        } else if (provider === 'google') {
            // Optionally, trigger Google One Tap again or show Google login
        }
    }, [configuration]);

    return (
        <GoogleOAuthProvider clientId={GOOGLE_CLIENT_ID}>
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', marginTop: '10vh' }}>
                <h2>Sign in to CarWash</h2>
                <button
                    onClick={() => handleMicrosoftLogin(configuration)}
                    style={{ margin: '1em', padding: '1em 2em', fontSize: '1.1em' }}
                >
                    Sign in with Microsoft Entra ID
                </button>
                <GoogleLogin
                    onSuccess={handleGoogleLoginSuccess}
                    onError={handleGoogleLoginError}
                    useOneTap
                    width="300px"
                    shape="pill"
                    text="Sign in with Google Workspace"
                />
            </div>
        </GoogleOAuthProvider>
    );
}
