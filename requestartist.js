document.getElementById('requestartist-link').addEventListener('click', () => {
    document.getElementById('request-artist-modal').classList.remove('hidden');
});

document.getElementById('submit-artist').addEventListener('click', async () => {
    const artistName = document.getElementById('artist-name').value.trim();
    const errorElement = document.getElementById('artist-error');

    if (!artistName) {
        errorElement.classList.remove('hidden');
        return;
    }
    errorElement.classList.add('hidden');

    try {
        const response = await fetch(`/request-artist?artistName=${encodeURIComponent(artistName)}`);
        const message = await response.text();

        if (response.ok) {
            alert(message);
        } else {
            alert(`Error: ${message}`);
        }
    } catch (error) {
        alert(`Error: ${error.message}`);
    }
});

document.querySelectorAll('.close-modal').forEach(button => {
    button.addEventListener('click', () => {
        document.getElementById('report-issue-modal').classList.add('hidden');
        document.getElementById('request-artist-modal').classList.add('hidden');
    });
});
