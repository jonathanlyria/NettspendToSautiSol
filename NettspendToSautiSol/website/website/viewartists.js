document.addEventListener('DOMContentLoaded', () => {
    const artistContainer = document.getElementById('artist-container');
    const loading = document.getElementById('loading');
    const errorDiv = document.getElementById('error');

    async function fetchArtists() {
        try {
            const response = await fetch('http://localhost:5048/api/get-all-artists');

            if (!response.ok) {
                throw new Error(`Server response error: ${response.status}`);
            }

            const data = await response.json();
            displayArtists(data.artists); // Note lowercase 'artists' to match JSON
        } catch (error) {
            showError(`Failed to load artists: ${error.message}`);
        } finally {
            loading.style.display = 'none';
        }
    }

    function displayArtists(artists) {
        if (!artists || artists.length === 0) {
            showError('No artists found in the database');
            return;
        }

        artistContainer.innerHTML = artists.map(artist => `
            <div class="artist-item">
                <span>${artist.name}</span>
                <a href="https://open.spotify.com/artist/${artist.spotifyId}" 
                   target="_blank" 
                   class="spotify-link">
                    View on Spotify
                </a>
            </div>
        `).join('');
    }

    function showError(message) {
        errorDiv.textContent = message;
        errorDiv.style.display = 'block';
    }

    fetchArtists();
});