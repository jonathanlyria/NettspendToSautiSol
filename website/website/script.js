const input = document.getElementById('input');
const output = document.getElementById('output');
let headerTitle = document.getElementById('header-title');

let pkceToken = null;
let artist1 = null;
let artist2 = null;
let pathIds = [];
let authState = null; // Store state for code verification

function appendOutput(text, className = '') {
    const newLine = document.createElement('div');
    newLine.textContent = text;
    if (className) newLine.classList.add(className);
    output.appendChild(newLine);
    output.scrollTop = output.scrollHeight;
}

async function processFlow() {
    appendOutput("Welcome! Press Enter to sign into Spotify.");
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

        // First check if response is JSON
        const contentType = response.headers.get('content-type');
        if (!contentType || !contentType.includes('application/json')) {
            const text = await response.text();
            throw new Error(`Invalid response: ${text.slice(0, 100)}`);
        }

        const data = await response.json();

        if (response.ok) {
            authState = data.state;
            const authWindow = window.open(data.authUrl, 'Spotify Auth', 'width=600,height=800');
            listenForAuthCallback();
        } else {
            appendOutput(`Error: ${data.Error || 'Unknown error'}`, "error");
        }
    } catch (error) {
        appendOutput(`Authentication failed: ${error.message}`, "error");
        console.error('Authentication error:', error);
    }
}

async function listenForAuthCallback() {
    window.addEventListener('message', async (event) => {
        if (event.origin !== window.location.origin) return;

        const { code, state } = event.data;

        try {
            const response = await fetch(`${API_BASE_URL}/exchange-code`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ code, state })
            });

            // Handle non-JSON responses
            const contentType = response.headers.get('content-type');
            const data = contentType?.includes('application/json')
                ? await response.json()
                : { Error: await response.text() };

            if (response.ok) {
                pkceToken = data.pkceToken;
                appendOutput("Authentication successful.", "success");
                requestArtistOne();
            } else {
                appendOutput(`Error: ${data.Error || 'Unknown error'}`, "error");
            }
        } catch (error) {
            appendOutput(`Code exchange failed: ${error.message}`, "error");
            console.error('Code exchange error:', error);
        }
    });
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

        return data.artistExist;
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
            const pathNames = data.pathName.join(" -> ");
            pathIds = data.pathId;
            appendOutput(`Path found: ${pathNames}`, "success");
            requestPlaylistCreation();
        } else {
            appendOutput(`Error: ${data.Error}`, "error");
        }
    } catch (error) {
        appendOutput(`An error occurred: ${error.message}`, "error");
    }
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
            LookForFeatures: true,
            TracksPerArtist: 3,
            PkceToken: pkceToken,
        };
        console.log("Request body:", requestBody);

        const response = await fetch(`${API_BASE_URL}/create-playlist`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(requestBody),
        });
        console.log(response)
        const data = await response.json();
        console.log(data)

        if (response.ok) {
            appendOutput(`Playlist created successfully! Link: ${data.playlistLink}`, "success");
            window.open(data.playlistLink, '_blank');


        } else {
            appendOutput(`Error: ${data.Error}`, "error");
        }
    } catch (error) {
        appendOutput(`An error occurred: ${error.message}`, "error");
    }
}

processFlow();
