// Ace Job Agency - JavaScript Utilities

// Session timeout warning
(function() {
    const SESSION_TIMEOUT = 20 * 60 * 1000; // 20 minutes in milliseconds
    const WARNING_BEFORE = 2 * 60 * 1000; // 2 minutes before timeout
    
    let timeoutTimer;
    let warningTimer;
    
    function resetTimers() {
        clearTimeout(timeoutTimer);
        clearTimeout(warningTimer);
        
        warningTimer = setTimeout(showTimeoutWarning, SESSION_TIMEOUT - WARNING_BEFORE);
        timeoutTimer = setTimeout(redirectToLogin, SESSION_TIMEOUT);
    }
    
    function showTimeoutWarning() {
        if (typeof bootstrap !== 'undefined') {
            const modalHtml = `
                <div class="modal fade" id="timeoutModal" tabindex="-1" aria-hidden="true">
                    <div class="modal-dialog modal-dialog-centered">
                        <div class="modal-content">
                            <div class="modal-header bg-warning">
                                <h5 class="modal-title">
                                    <i class="bi bi-exclamation-triangle-fill me-2"></i>Session Expiring
                                </h5>
                            </div>
                            <div class="modal-body">
                                <p>Your session will expire in 2 minutes due to inactivity.</p>
                                <p>Please click anywhere or press any key to stay logged in.</p>
                            </div>
                        </div>
                    </div>
                </div>
            `;
            
            // Remove existing modal if any
            const existingModal = document.getElementById('timeoutModal');
            if (existingModal) {
                existingModal.remove();
            }
            
            document.body.insertAdjacentHTML('beforeend', modalHtml);
            const modal = new bootstrap.Modal(document.getElementById('timeoutModal'));
            modal.show();
        }
    }
    
    function redirectToLogin() {
        window.location.href = '/Account/Login?error=SessionExpired';
    }
    
    // Only set up timers if user is logged in
    if (document.querySelector('[data-user-logged-in]')) {
        document.addEventListener('mousemove', resetTimers);
        document.addEventListener('keypress', resetTimers);
        document.addEventListener('click', resetTimers);
        resetTimers();
    }
})();

// Auto-hide alerts after 5 seconds
(function() {
    const alerts = document.querySelectorAll('.alert-dismissible');
    alerts.forEach(alert => {
        setTimeout(() => {
            const closeButton = alert.querySelector('.btn-close');
            if (closeButton) {
                closeButton.click();
            }
        }, 5000);
    });
})();

// Form validation enhancement
(function() {
    // Add visual feedback for valid/invalid fields
    const forms = document.querySelectorAll('form');
    forms.forEach(form => {
        const inputs = form.querySelectorAll('input, select, textarea');
        inputs.forEach(input => {
            input.addEventListener('blur', function() {
                if (this.checkValidity()) {
                    this.classList.add('is-valid');
                    this.classList.remove('is-invalid');
                } else if (this.value !== '') {
                    this.classList.add('is-invalid');
                    this.classList.remove('is-valid');
                }
            });
        });
    });
})();

// Password visibility toggle
(function() {
    document.querySelectorAll('[data-toggle-password]').forEach(toggle => {
        toggle.addEventListener('click', function() {
            const targetId = this.getAttribute('data-target');
            const input = document.getElementById(targetId);
            
            if (input) {
                const type = input.getAttribute('type') === 'password' ? 'text' : 'password';
                input.setAttribute('type', type);
                
                // Toggle icon
                const icon = this.querySelector('i');
                if (icon) {
                    icon.classList.toggle('bi-eye');
                    icon.classList.toggle('bi-eye-slash');
                }
            }
        });
    });
})();

// CSRF token helper for AJAX requests
function getAntiForgeryToken() {
    const token = document.querySelector('input[name="__RequestVerificationToken"]');
    return token ? token.value : '';
}

// AJAX helper with CSRF protection
function ajaxRequest(url, options = {}) {
    const defaultOptions = {
        headers: {
            'RequestVerificationToken': getAntiForgeryToken(),
            'X-Requested-With': 'XMLHttpRequest'
        }
    };
    
    return fetch(url, { ...defaultOptions, ...options });
}

// Confirm before leaving page with unsaved changes
(function() {
    let formChanged = false;
    const forms = document.querySelectorAll('form');
    
    forms.forEach(form => {
        form.addEventListener('change', () => {
            formChanged = true;
        });
        
        form.addEventListener('submit', () => {
            formChanged = false;
        });
    });
    
    window.addEventListener('beforeunload', (e) => {
        if (formChanged) {
            e.preventDefault();
            e.returnValue = '';
        }
    });
})();

// Utility function to format dates
function formatDate(dateString, format = 'MMM dd, yyyy') {
    const date = new Date(dateString);
    const options = { year: 'numeric', month: 'short', day: 'numeric' };
    return date.toLocaleDateString('en-US', options);
}

// Utility function to debounce function calls
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// Console warning for production
if (window.location.hostname !== 'localhost' && window.location.hostname !== '127.0.0.1') {
    console.log('%cStop!', 'color: red; font-size: 40px; font-weight: bold;');
    console.log('%cThis is a browser feature intended for developers. Do not paste any code here that you don\'t understand.', 'color: #333; font-size: 14px;');
}
