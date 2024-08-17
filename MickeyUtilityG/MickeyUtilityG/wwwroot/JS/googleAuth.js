// Replace these with your actual Google API credentials
const CLIENT_ID = '303704802649-1elhd6tjhdef2t5m4mk82gkdfdo1vtsq.apps.googleusercontent.com';
const API_KEY = 'YOUR_API_KEY_HERE';
const SCOPES = 'https://www.googleapis.com/auth/spreadsheets';

let tokenClient;

async function initializeGapiClient() {
    try {
        await gapi.client.init({
            apiKey: API_KEY,
            discoveryDocs: ['https://sheets.googleapis.com/$discovery/rest?version=v4'],
        });
        console.log('GAPI client initialized successfully');
    } catch (error) {
        console.error('Error initializing GAPI client:', error);
        throw error;
    }
}

window.googleAuthHelper = {
    initializeGoogleAuth: async function () {
        try {
            console.log('Starting Google Auth initialization');
            if (typeof gapi === 'undefined') {
                throw new Error('gapi is not loaded. Make sure the Google API script is included in your HTML.');
            }
            await new Promise((resolve) => gapi.load('client', resolve));
            await initializeGapiClient();
            tokenClient = google.accounts.oauth2.initTokenClient({
                client_id: CLIENT_ID,
                scope: SCOPES,
                callback: '', // defined later
            });
            console.log('Google Auth initialized successfully');
            return 'Initialization successful';
        } catch (error) {
            console.error('Error in initializeGoogleAuth:', error);
            throw error;
        }
    },
    getAccessToken: function () {
        return new Promise((resolve, reject) => {
            if (!tokenClient) {
                reject(new Error('Token client not initialized. Call initializeGoogleAuth first.'));
                return;
            }
            tokenClient.callback = (resp) => {
                if (resp.error !== undefined) {
                    reject(resp);
                } else {
                    resolve(resp.access_token);
                }
            };
            tokenClient.requestAccessToken({ prompt: 'consent' });
        });
    }
};

console.log('googleAuth.js loaded');
