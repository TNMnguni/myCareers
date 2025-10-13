// Bootstrap form validation
(function () {
    'use strict';
    window.addEventListener('load', function () {
        var forms = document.getElementsByClassName('needs-validation');
        var validation = Array.prototype.filter.call(forms, function (form) {
            form.addEventListener('submit', function (event) {
                if (form.checkValidity() === false) {
                    event.preventDefault();
                    event.stopPropagation();
                }
                form.classList.add('was-validated');
            }, false);
        });
    }, false);
})();

// Auto-hide alerts after 5 seconds
document.addEventListener('DOMContentLoaded', function () {
    const alerts = document.querySelectorAll('.alert-dismissible');
    alerts.forEach(function (alert) {
        setTimeout(function () {
            if (alert && alert.parentNode) {
                alert.style.opacity = '0';
                alert.style.transition = 'opacity 0.5s';
                setTimeout(function () {
                    alert.remove();
                }, 500);
            }
        }, 5000);
    });
});

// Form submission loading states
document.addEventListener('DOMContentLoaded', function () {
    const forms = document.querySelectorAll('form');
    forms.forEach(function (form) {
        form.addEventListener('submit', function (e) {
            const submitButton = form.querySelector('button[type="submit"]');
            if (submitButton) {
                submitButton.disabled = true;
                submitButton.classList.add('loading');

                // Re-enable after 10 seconds as failsafe
                setTimeout(function () {
                    submitButton.disabled = false;
                    submitButton.classList.remove('loading');
                }, 10000);
            }
        });
    });
});

// Password strength indicator
function checkPasswordStrength(password) {
    let strength = 0;
    const indicators =

    {
        length: password.length >= 8, lowercase: /[a-z]/.test(password), uppercase: /[A-Z]/.test(password), numbers: /\d/.test(password), special: /[@$!%*?&]/.test(password)
    }

        ;

    for (let key in indicators) {
        if (indicators[key]) strength++;
    }

    return {
        score: strength, indicators: indicators
    }

        ;
}

// Initialize password strength checker if password field exists
document.addEventListener('DOMContentLoaded', function () {
    const passwordField = document.querySelector('input[type="password"]');
    if (passwordField) {
        passwordField.addEventListener('input', function () {
            const strength = checkPasswordStrength(this.value);
            // You can add visual feedback here
        });
    }
});
