const input = document.getElementById('input');
const output = document.getElementById('output-box');
let headerTitle = document.getElementById('header-title');

let artist1 = null;
let artist2 = null;
let pathIds = [];
let authState = null;
let authCode = null;

function appendOutput(text, className = '') {
    const newLine = document.createElement('div');
    newLine.textContent = text;
    if (className) newLine.classList.add(className);
    output.appendChild(newLine);
    output.scrollTop = output.scrollHeight;
}

async function processFlow() {
    requestArtistOne();
}

function requestArtistOne() {
    appendOutput("Enter the name of the first artist:");
    const handler = async (event) => {
        if (event.key === 'Enter') {
            const artistName = input.value.trim();
            input.value = '';
            appendOutput(`> ${artistName}`);
            input.removeEventListener('keydown', handler);

            const exists = await checkArtistExists(artistName);
            if (exists) {
                artist1 = artistName;
                document.title = `from ${artist1}`;
                headerTitle.textContent = `from ${artist1}`;
                requestArtistTwo();
            } else {
                appendOutput("Artist not found. Please try again.", "error");
                requestArtistOne();
            }
        }
    };
    input.addEventListener('keydown', handler);
}

function requestArtistTwo() {
    appendOutput("Enter the name of the second artist:");
    const handler = async (event) => {
        if (event.key === 'Enter') {
            const artistName = input.value.trim();
            input.value = '';
            appendOutput(`> ${artistName}`);
            input.removeEventListener('keydown', handler);

            const exists = await checkArtistExists(artistName);
            if (exists) {
                artist2 = artistName;
                document.title = `from ${artist1} to ${artist2}`;
                headerTitle.textContent = `from ${artist1} to ${artist2}`;
                await findPathBetweenArtists();
            } else {
                appendOutput("Artist not found. Please try again.", "error");
                requestArtistTwo();
            }
        }
    };
    input.addEventListener('keydown', handler);
}

async function checkArtistExists(artistName) {
    try {
        console.log(`Checking artist: ${artistName}`);
        const response = await fetch(`${API_BASE_URL}/artists-exist?artistName=${encodeURIComponent(artistName)}`);
        const data = await response.json();

        console.log("Response status:", response.status);
        console.log("Response data:", data);

        if (!response.ok) {
            appendOutput(`Error from server: ${data.Error || "Unknown error"}`, "error");
            return false;
        }

        return data.ArtistExist; // Match API casing (ArtistExist)
    } catch (error) {
        appendOutput(`An error occurred: ${error.message}`, "error");
        return false;
    }
}

async function findPathBetweenArtists() {
    try {
        appendOutput(`Finding path between "${artist1}" and "${artist2}"...`);
        const response = await fetch(`${API_BASE_URL}/find-path?artist1=${encodeURIComponent(artist1)}&artist2=${encodeURIComponent(artist2)}`);
        const data = await response.json();
        console.log(data);

        if (response.ok) {
            const pathNames = data.PathName.join(" -> "); // Match API casing (PathName)
            pathIds = data.PathId; // Match API casing (PathId)
            appendOutput(`Path found: ${pathNames}`, "success");
            requestAuthentication();
        } else {
            appendOutput(`Error: ${data.Error}`, "error");
        }
    } catch (error) {
        appendOutput(`An error occurred: ${error.message}`, "error");
    }
}

async function requestAuthentication() {
    appendOutput("Press Enter to sign into Spotify.");
    input.addEventListener('keydown', async function handler(event) {
        if (event.key === 'Enter') {
            input.removeEventListener('keydown', handler);
            appendOutput("> Authenticating...");
            await startAuthentication();
        }
    });
}

async function startAuthentication() {
    try {
        const response = await fetch(`${API_BASE_URL}/authenticate-user`);

        const contentType = response.headers.get('content-type');
        if (!contentType || !contentType.includes('application/json')) {
            const text = await response.text();
            throw new Error(`Invalid response: ${text.slice(0, 100)}`);
        }

        const data = await response.json();

        if (response.ok) {
            authState = data.State; // Match API casing (State)
            const authWindow = window.open(data.AuthUrl, 'Spotify Auth', 'width=600,height=800'); // Match API casing (AuthUrl)

            // Setup handler for when Spotify redirects back to our site with the code
            window.addEventListener('message', handleSpotifyCallback);
            appendOutput("Waiting for Spotify authentication...");
        } else {
            appendOutput(`Error: ${data.Error || 'Unknown error'}`, "error");
        }
    } catch (error) {
        appendOutput(`Authentication failed: ${error.message}`, "error");
        console.error('Authentication error:', error);
    }
}

// This function would be called when the Spotify OAuth redirect occurs
function handleSpotifyCallback(event) {
    if (event.origin !== window.location.origin) return;

    const { code, state } = event.data;

    // Verify state matches what we sent
    if (state !== authState) {
        appendOutput("Authentication failed: State mismatch", "error");
        return;
    }

    authCode = code;
    window.removeEventListener('message', handleSpotifyCallback);
    appendOutput("Authentication successful.", "success");
    requestPlaylistCreation();
}

function requestPlaylistCreation() {
    appendOutput("Press Enter to create the playlist.");
    input.addEventListener('keydown', async function handler(event) {
        if (event.key === 'Enter') {
            input.removeEventListener('keydown', handler);
            await createPlaylist();
        }
    });
}

async function createPlaylist() {
    try {
        appendOutput("Creating playlist...");
        const requestBody = {
            Path: pathIds,
            Code: authCode,  // Send code instead of token
            State: authState // Include state for verification
        };
        console.log("Request body:", requestBody);

        const response = await fetch(`${API_BASE_URL}/create-playlist`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(requestBody),
        });
        console.log(response);
        const data = await response.json();
        console.log(data);

        if (response.ok) {
            appendOutput(`Playlist created successfully! Link: ${data.PlaylistLink}`, "success"); // Match API casing (PlaylistLink)
            window.open(data.PlaylistLink, '_blank');
        } else {
            appendOutput(`Error: ${data.Error}`, "error");
        }
    } catch (error) {
        appendOutput(`An error occurred: ${error.message}`, "error");
    }
}

processFlow();