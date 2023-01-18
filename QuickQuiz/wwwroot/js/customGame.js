$(function () {
    $('[data-toggle="tooltip"]').tooltip()
});

function createLobbySubmit() {
    const settings = {
        twitchLobby: document.getElementById('twitchLobby').checked
    };

    makePostRequest(createLobbyUrl, settings)
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

(function () {
    const forms = document.querySelectorAll('.needs-validation');
    Array.prototype.slice.call(forms)
        .forEach(function (form) {
            form.addEventListener('submit', function (event) {

                event.preventDefault()
                event.stopPropagation()

                if (form.checkValidity()) {
                    switch (event.submitter.dataset.buttontype) {
                        case 'createLobby':
                            createLobbySubmit();
                            break;
                    }
                }

                form.classList.add('was-validated');
            }, false)
        });
})();