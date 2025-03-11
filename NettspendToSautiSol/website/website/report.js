// Citation of modals
document.getElementById('report-link').addEventListener('click', () => {
    document.getElementById('report-issue-modal').classList.remove('hidden');
});

document.getElementById('submit-issue').addEventListener('click', async () => {
    const issueText = document.getElementById('issue-text').value.trim();
    const errorElement = document.getElementById('issue-error');

    if (!issueText) {
        errorElement.classList.remove('hidden');
        return;
    }
    errorElement.classList.add('hidden');

    try {
        const response = await fetch(`${API_BASE_URL}/report-issue?issue=${encodeURIComponent(issueText)}`);
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
