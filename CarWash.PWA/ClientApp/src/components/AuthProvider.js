import { createContext } from 'react';
import { GoogleOAuthProvider } from '@react-oauth/google';

export const AuthContext = createContext(null);

const GOOGLE_CLIENT_ID =
    process.env.REACT_APP_GOOGLE_CLIENT_ID ||
    '671648737845-v6mijdiaui34trt49mf4hsf34ins6obq.apps.googleusercontent.com';

export const AuthProvider = ({ children }) => {
    return (
        <AuthContext.Provider value={{}}>
            <GoogleOAuthProvider clientId={GOOGLE_CLIENT_ID}>{children}</GoogleOAuthProvider>
        </AuthContext.Provider>
    );
};
