function changePasswordSubmit() {
    makePostRequest(changePasswordUrl, { currentPassword: document.getElementById('currentPassword').value, password: document.getElementById('userPassword').value })
        .then(data => {
            if (data.error) {
                toastr.error(translateCode(data.error));
                return;
            }

            toastr.success(translateCode(data.success));
        })
        .catch((error) => {
            toastr.error('Błąd ' + error.toString());
        });
}

function removeAccountConnection(target) {
    const removeConnectionType = target.parentElement.parentElement.querySelector('th').dataset.connectionType;
    makePostRequest(removeAccountConnectionUrl, { connectionType: removeConnectionType })
        .then(data => {
            if (data.error) {
                toastr.error(translateCode(data.error));
                return;
            }

            if (removeConnectionType == 'Twitch') {
                document.getElementById('twitchConnectionButton').classList.remove('d-none');
            }
            target.parentElement.parentElement.remove();
            toastr.success(translateCode(data.success));
        })
        .catch((error) => {
            toastr.error('Błąd ' + error.toString());
        });
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
                        case 'password':
                            changePasswordSubmit();
                            break;
                    }
                }

                form.classList.add('was-validated');
            }, false)
        });

    const connectionTable = document.getElementById('accountConnectionsTable');
    const isConnectedToTwitch = connectionTable.querySelector('th[data-connection-type=Twitch]') != undefined;
    if (!isConnectedToTwitch) {
        document.getElementById('twitchConnectionButton').classList.remove('d-none');
    }
})();