var userInfoFormInputs = [];

function getInputValue(input) {
    let val = input.value;

    if (input.type == 'checkbox')
        return input.checked;

    if (input.type == 'number')
        val = parseInt(val);

    return val;
}

function getEditedValues(inputs) {
    const editedValues = [];

    inputs.forEach(x => {
        const element = document.getElementById(x.id);
        const currentValue = getInputValue(element);

        if (x.value != currentValue) {
            editedValues.push({ id: element.id, value: currentValue });
        }
    });

    return editedValues;
}

function updateValuesForForm(formElement) {
    const array = [];
    formElement.querySelectorAll('input').forEach(x => {
        array.push({ id: x.id, value: getInputValue(x) });
    });
    return array;
}

function editUserInfoSubmit() {
    const editedValues = getEditedValues(userInfoFormInputs);
    if (editedValues.length == 0)
        return;

    let data = { id: document.getElementById('userProfileId').value };
    let values = '';

    editedValues.forEach(x => {
        values += `<br/>${escapeValue(x.id)}`;
        data[x.id] = x.value;
    })

    $.confirm({
        title: 'Ostrzeżenie',
        content: 'Czy na pewno chcesz wykonać tą akcje, spowoduje to zmienienie następujących wartości:' + values,
        theme: 'dark',
        buttons: {
            confirm: function () {
                makePostRequest(modifyUserProfileUrl, data)
                    .then(data => {
                        if (data.error) {
                            toastr.error(translateCode(data.error));
                            return;
                        }

                        userInfoFormInputs = updateValuesForForm(document.getElementById('userInfoForm'));
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

(function () {
    userInfoFormInputs = updateValuesForForm(document.getElementById('userInfoForm'));

    const forms = document.querySelectorAll('.needs-validation');
    Array.prototype.slice.call(forms)
        .forEach(function (form) {
            form.addEventListener('submit', function (event) {

                event.preventDefault()
                event.stopPropagation()

                if (form.checkValidity()) {
                    switch (event.submitter.dataset.buttontype) {
                        case 'userInfo':
                            editUserInfoSubmit();
                            break;
                    }
                }

                form.classList.add('was-validated');
            }, false)
        });
})();