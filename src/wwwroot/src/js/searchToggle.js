// Search toggle functionality for expandable search in header
// Based on reference implementation from C:\Work\ci-general-prototype

export function initSearchToggle() {
    const searchToggle = document.getElementById('search-toggle-button');
    const searchPanel = document.getElementById('search-overlay-panel');

    if (!searchToggle || !searchPanel) {
        return;
    }

    // Toggle search panel on button click
    searchToggle.addEventListener('click', function() {
        const isHidden = searchPanel.hasAttribute('hidden');

        if (isHidden) {
            // Open search panel
            searchToggle.setAttribute('aria-expanded', 'true');
            searchPanel.removeAttribute('hidden');
            searchToggle.classList.add('search-toggle-button--open');
            
            // Focus on search input
            const searchInput = searchPanel.querySelector('#search-overlay-input');
            if (searchInput) {
                setTimeout(() => searchInput.focus(), 100);
            }
        } else {
            // Close search panel
            searchToggle.setAttribute('aria-expanded', 'false');
            searchPanel.setAttribute('hidden', 'hidden');
            searchToggle.classList.remove('search-toggle-button--open');
        }
    });

    // Close search panel on Escape key
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape') {
            const isHidden = searchPanel.hasAttribute('hidden');
            if (!isHidden) {
                searchToggle.setAttribute('aria-expanded', 'false');
                searchPanel.setAttribute('hidden', 'hidden');
                searchToggle.classList.remove('search-toggle-button--open');
                
                // Return focus to toggle button
                searchToggle.focus();
            }
        }
    });
}

