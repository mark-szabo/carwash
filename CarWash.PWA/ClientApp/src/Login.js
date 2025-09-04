import React, { useEffect, useState } from 'react';
import { GoogleOAuthProvider, GoogleLogin, useGoogleLogin } from '@react-oauth/google';
import { runWithAdal } from './Auth';

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
    const [codeResponse, setCodeResponse] = useState('');

    const googleLogin = useGoogleLogin({
        flow: 'auth-code',
        ux_mode: 'redirect',
        redirect_uri: window.location.origin + '/api/auth/google-login',
        onSuccess: async codeResponse => {
            setCodeResponse(codeResponse);
        },
        onError: errorResponse => console.log(errorResponse),
    });

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
        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', marginTop: '10vh' }}>
            <h2>Sign in to CarWash</h2>
            <button
                onClick={() => handleMicrosoftLogin(configuration)}
                style={{ margin: '1em', padding: '1em 2em', fontSize: '1.1em' }}
            >
                Sign in with Microsoft Entra ID
            </button>
            <button onClick={() => googleLogin()} style={{ margin: '1em', padding: '1em 2em', fontSize: '1.1em' }}>
                Sign in with Google
            </button>
            <p>{JSON.stringify(codeResponse)}</p>
        </div>
    );
}
