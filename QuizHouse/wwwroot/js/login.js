function submitRegister() {
    makePostRequest(registerAccountUrl, { username: document.getElementById('userName').value, email: document.getElementById('userEmail').value, password: document.getElementById('userPassword').value })
        .then(data => {
            if (data.error) {
                toastr.error(translateCode(data.error));
                return;
            }

            window.location = loginHomeUrl + '?success=account_created';
        })
        .catch((error) => {
            toastr.error(error.toString());
        });
}

function loginSubmit() {
    makePostRequest(loginAccountUrl, { email: document.getElementById('userEmail').value, password: document.getElementById('userPassword').value, rememberme: document.getElementById('rememberMe').checked })
        .then(data => {
            if (data.error) {
                toastr.error(translateCode(data.error));
                return;
            }

            window.location = homeUrl;
        })
        .catch((error) => {
            toastr.error(error.toString());
        });
}

function resetPassword() {
    const email = document.getElementById('userEmail');
    if (email.checkValidity()) {
        makePostRequest(passwordAccountResetUrl, { email: email.value })
            .then(data => {
                if (data.error) {
                    toastr.error(translateCode(data.error));
                    return;
                }

                toastr.success(translateCode(data.success));
            })
            .catch((error) => {
                toastr.error(error.toString());
            });
    }
    email.parentElement.classList.add('was-validated');
}

(function () {
    showMessagesFromUrl();

    const forms = document.querySelectorAll('.needs-validation');
    Array.prototype.slice.call(forms)
        .forEach(function (form) {
            form.addEventListener('submit', function (event) {

                event.preventDefault()
                event.stopPropagation()

                if (form.checkValidity()) {
                    switch (event.submitter.dataset.buttontype) {
                        case 'login':
                            loginSubmit();
                            break;
                        case 'register':
                            submitRegister();
                            break;
                    }
                }

                form.classList.add('was-validated');
            }, false)
        });
})();