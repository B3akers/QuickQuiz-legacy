﻿const toBase64 = file => new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.readAsDataURL(file);
    reader.onload = () => resolve(reader.result);
    reader.onerror = error => reject(error);
});

function deleteRecord(url, table, recordId) {
    $.confirm({
        title: 'Potwierdź usunięcie',
        content: 'Czy na pewno chcesz usuńać ten rekord?',
        theme: 'dark',
        buttons: {
            confirm: function () {
                makePostRequest(url, { id: recordId })
                    .then(data => {
                        if (data.error) {
                            toastr.error(translateCode(data.error));
                            return;
                        }

                        table.ajax.reload();
                        toastr.success(translateCode(data.success));
                    })
                    .catch((error) => {
                        toastr.error(error.toString());
                    });
            },
            cancel: function () {

            },
        }
    });
}