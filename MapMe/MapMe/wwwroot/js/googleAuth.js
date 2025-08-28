// Google OAuth integration for MapMe authentication

let googleAuth = null;
let isGoogleInitialized = false;

/**
 * Initializes Google Sign-In
 */
window.initializeGoogleSignIn = async function() {
    if (isGoogleInitialized) {
        return;
    }

    try {
        // Load Google Identity Services
        if (!window.google) {
            await loadGoogleScript();
        }

        // Initialize Google Sign-In
        google.accounts.id.initialize({
            client_id: await getGoogleClientId(),
            callback: handleCredentialResponse,
            auto_select: false,
            cancel_on_tap_outside: true
        });

        isGoogleInitialized = true;
        console.log('Google Sign-In initialized successfully');
    } catch (error) {
        console.error('Error initializing Google Sign-In:', error);
        throw new Error('Failed to initialize Google Sign-In');
    }
};

/**
 * Triggers Google Sign-In flow
 */
window.signInWithGoogle = function() {
    return new Promise((resolve, reject) => {
        if (!isGoogleInitialized) {
            reject(new Error('Google Sign-In not initialized'));
            return;
        }

        try {
            // Store the resolve/reject functions globally so the callback can access them
            window.googleSignInResolve = resolve;
            window.googleSignInReject = reject;

            // Trigger the sign-in flow
            google.accounts.id.prompt((notification) => {
                if (notification.isNotDisplayed() || notification.isSkippedMoment()) {
                    // Fallback to popup if prompt is not displayed
                    google.accounts.id.renderButton(
                        document.getElementById('google-signin-button') || document.body,
                        {
                            theme: 'outline',
                            size: 'large',
                            width: '100%'
                        }
                    );
                }
            });
        } catch (error) {
            console.error('Error during Google Sign-In:', error);
            reject(error);
        }
    });
};

/**
 * Handles the credential response from Google
 */
function handleCredentialResponse(response) {
    try {
        if (response.credential) {
            // Decode the JWT token to get user information
            const userInfo = parseJwt(response.credential);
            
            const googleUser = {
                id: userInfo.sub,
                email: userInfo.email,
                name: userInfo.name,
                idToken: response.credential
            };

            if (window.googleSignInResolve) {
                window.googleSignInResolve(googleUser);
                window.googleSignInResolve = null;
                window.googleSignInReject = null;
            }
        } else {
            throw new Error('No credential received from Google');
        }
    } catch (error) {
        console.error('Error handling credential response:', error);
        if (window.googleSignInReject) {
            window.googleSignInReject(error);
            window.googleSignInResolve = null;
            window.googleSignInReject = null;
        }
    }
}

/**
 * Loads the Google Identity Services script
 */
function loadGoogleScript() {
    return new Promise((resolve, reject) => {
        if (document.getElementById('google-identity-script')) {
            resolve();
            return;
        }

        const script = document.createElement('script');
        script.id = 'google-identity-script';
        script.src = 'https://accounts.google.com/gsi/client';
        script.async = true;
        script.defer = true;
        
        script.onload = () => {
            console.log('Google Identity Services script loaded');
            resolve();
        };
        
        script.onerror = () => {
            console.error('Failed to load Google Identity Services script');
            reject(new Error('Failed to load Google script'));
        };

        document.head.appendChild(script);
    });
}

/**
 * Gets the Google Client ID from the server configuration
 */
async function getGoogleClientId() {
    try {
        // In a real implementation, you would get this from your server configuration
        // For now, we'll use a placeholder that should be replaced with actual client ID
        const response = await fetch('/config/google-client-id');
        if (response.ok) {
            const config = await response.json();
            return config.clientId;
        } else {
            // Fallback to environment variable or default
            return 'YOUR_GOOGLE_CLIENT_ID_HERE';
        }
    } catch (error) {
        console.warn('Could not fetch Google Client ID from server, using fallback');
        return 'YOUR_GOOGLE_CLIENT_ID_HERE';
    }
}

/**
 * Parses a JWT token to extract user information
 */
function parseJwt(token) {
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(
            atob(base64)
                .split('')
                .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
                .join('')
        );
        return JSON.parse(jsonPayload);
    } catch (error) {
        console.error('Error parsing JWT token:', error);
        throw new Error('Invalid JWT token');
    }
}

/**
 * Signs out from Google
 */
window.signOutFromGoogle = function() {
    if (window.google && google.accounts.id) {
        google.accounts.id.disableAutoSelect();
        console.log('Signed out from Google');
    }
};

/**
 * Renders a Google Sign-In button
 */
window.renderGoogleSignInButton = function(elementId, options = {}) {
    if (!isGoogleInitialized) {
        console.error('Google Sign-In not initialized');
        return;
    }

    const defaultOptions = {
        theme: 'outline',
        size: 'large',
        width: '100%',
        text: 'signin_with',
        shape: 'rectangular'
    };

    const buttonOptions = { ...defaultOptions, ...options };

    google.accounts.id.renderButton(
        document.getElementById(elementId),
        buttonOptions
    );
};
