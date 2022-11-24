var categoriesDataTable;

function deleteCategory(categoryId) {
    deleteRecord(deleteCategoryUrl, categoriesDataTable, categoryId);
}

function refershQuestionsCount() {
    $.confirm({
        title: 'Ostrzeżenie',
        content: 'Czy na pewno chcesz wykonać tą akcje, spowoduje to iteracje po wszystkich pytaniach w bazie?',
        theme: 'dark',
        buttons: {
            confirm: function () {
                makeGetRequest(refreshCategoriesQuestions)
                    .then(data => {
                        if (data.error) {
                            toastr.error(translateCode(data.error));
                            return;
                        }

                        categoriesDataTable.ajax.reload();
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

function resetPopularity() {
    $.confirm({
        title: 'Ostrzeżenie',
        content: 'Czy na pewno chcesz wykonać tą akcje, spowoduje to wyzerowanie wszystkich wartości?',
        theme: 'dark',
        buttons: {
            confirm: function () {
                makeGetRequest(resetPopularityUrl)
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
            },
            cancel: function () {

            },
        }
    });
}

function getDataById(id) {
    const data = categoriesDataTable.rows().data();
    for (let i = 0; i < data.length; i++) {
        const obj = data[i];
        if (obj && obj.id == id)
            return obj;
    }
    return null;
}

function modifyCategory(create, categoryId) {
    var category;
    if (!create) {
        category = getDataById(categoryId);
        if (!category)
            return;
    }

    $.confirm({
        title: create ? 'Dodaj nową kategorie' : 'Modyfikuj kategorię',
        content: '' +
            '<form action="" class="formName">' +
            '<div class="form-group">' +
            '<label>Nazwa kategorii</label>' +
            '<input type="text" name="label" class="name form-control" value="' + (create ? '' : escapeValue(category.label)) + '" required />' +
            '</div>' +
            '<div class="form-group">' +
            '<label>Kolor</label>' +
            '<input type="color" name="color" class="name form-control" value="' + (create ? '' : category.color) + '" required />' +
            '</div>' +
            (create ?
                ('<div class="mb-3">' +
                    '<label for="formFile" class="form-label">Logotyp</label>' +
                    '<input class="form-control" accept="image/jpeg, image/png" type="file" name="icon">' +
                    '</div>')
                : ('<div class="form-group">' +
                    '<label>Logotyp</label>' +
                    '<input type="text" name="icon" class="name form-control" value="' + escapeValue(category.icon) + '" required />' +
                    '</div>')) +

            (create ? '' : `<input type="hidden" name="id" value="${categoryId}" />`) +
            '</form>',
        theme: 'dark',
        buttons: {
            formSubmit: {
                text: create ? 'Dodaj' : 'Modyfikuj',
                action: async function () {
                    const content = this.$content[0];

                    const label = content.querySelector('input[name="label"]').value.trim();
                    const color = content.querySelector('input[name="color"]').value.trim();
                    const icon = create ? content.querySelector('input[name="icon"]').files[0] : content.querySelector('input[name="icon"]').value.trim();

                    if (!label || !color || !icon) {
                        toastr.error(translateCode('invalid_model'));
                        return false;
                    }

                    const data = { label: label, color: color };
                    if (!create) {
                        data.icon = icon;
                        data.id = content.querySelector('input[name="id"]').value.trim();
                    } else {
                        data.iconBase64 = await toBase64(icon);
                    }

                    makePostRequest(create ? addCategoryUrl : editCategoryUrl, data)
                        .then(data => {
                            if (data.error) {
                                toastr.error(translateCode(data.error));
                                return;
                            }

                            categoriesDataTable.ajax.reload();
                            toastr.success(translateCode(data.success));
                        })
                        .catch((error) => {
                            toastr.error(error.toString());
                        });

                    return true;
                }
            },
            cancel: function () {

            }
        }
    })
}

(function () {
    const table = document.getElementById('categoriesTable');

    categoriesDataTable = $(table).DataTable({
        pageLength: 15,
        serverSide: true,
        order: [],
        columns: [
            {
                name: "name",
                data: "label",
                orderable: false
            },
            {
                name: "color",
                data: "color",
                render: function (data, type) {
                    if (type === "display") {
                        return `<div style="height: 1.5rem; background-color: ${data};"></div>`;
                    }
                    return data;
                },
                orderable: false
            },
            {
                name: "logo",
                data: "icon",
                render: function (data, type) {
                    if (type === "display") {
                        return `<img width="32" heigth="32" src="${data}"></img>`;
                    }
                    return data;
                },
                orderable: false
            },
            {
                name: "questions",
                data: "questionCount",
                orderable: true
            },
            {
                name: "actions",
                data: "id",
                render: function (data, type) {
                    if (type === "display") {
                        return `<i role="button" title="Edytuj kategorie" onclick="modifyCategory(false, this.dataset.id)" data-id="${data}" class="fa fa-lg fa-pencil"></i>` +
                            `<i role="button" title="Usuń kategorie" onclick="deleteCategory(this.dataset.id)" data-id="${data}" class="fa fa-lg fa-trash ms-3"></i>`;
                    }
                    return data;
                },
                orderable: false
            }
        ],
        ajax: {
            url: getCategoriesUrl,
            contentType: "application/json",
            type: "POST",
            data: function (d) {
                return JSON.stringify(d);
            }
        }
    });
})();