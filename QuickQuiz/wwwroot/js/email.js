(function () {
    document.getElementById('resendEmail').addEventListener('click', function (event) {
        makeGetRequest(resendEmailUrl)
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
    });
})();