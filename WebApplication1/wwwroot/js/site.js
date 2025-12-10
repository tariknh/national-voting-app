// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Mobile menu toggle
document.addEventListener('DOMContentLoaded', function() {
    const mobileMenuBtn = document.getElementById('mobileMenuBtn');
    const primaryNav = document.getElementById('primaryNav');
    const navActions = document.getElementById('navActions');

    if (mobileMenuBtn) {
        mobileMenuBtn.addEventListener('click', function() {
            const isExpanded = this.getAttribute('aria-expanded') === 'true';
            
            // Toggle button state
            this.classList.toggle('active');
            this.setAttribute('aria-expanded', !isExpanded);
            this.setAttribute('aria-label', isExpanded ? 'Åpne meny' : 'Lukk meny');
            
            // Toggle nav visibility
            primaryNav.classList.toggle('active');
            navActions.classList.toggle('active');
        });

        // Close menu when clicking a link (mobile)
        const navLinks = document.querySelectorAll('.primary-nav a');
        navLinks.forEach(link => {
            link.addEventListener('click', function() {
                if (window.innerWidth <= 768) {
                    mobileMenuBtn.classList.remove('active');
                    mobileMenuBtn.setAttribute('aria-expanded', 'false');
                    primaryNav.classList.remove('active');
                    navActions.classList.remove('active');
                }
            });
        });

        // Close menu on window resize to desktop
        window.addEventListener('resize', function() {
            if (window.innerWidth > 768) {
                mobileMenuBtn.classList.remove('active');
                mobileMenuBtn.setAttribute('aria-expanded', 'false');
                primaryNav.classList.remove('active');
                navActions.classList.remove('active');
            }
        });
    }
});
