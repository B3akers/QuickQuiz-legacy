function getSelectedCategoryId() {
    const selectedCategory = document.querySelector('.category-selection.show');
    if (!selectedCategory) {
        return undefined;
    }

    return selectedCategory.dataset.categoryId;
}

function addQuestionClick() {
    const category = getSelectedCategoryId();
    if (!category) {
        return;
    }

    let categories = [];
    document.querySelectorAll('a[data-category-id]').forEach(x => categories.push({ id: x.dataset.categoryId, label: x.querySelector('div.rounded-pill').innerText }));

    let categoriesSelect = '';
    categories.forEach(x => {
        categoriesSelect += '<option value="' + x.id + '"' + (x.id == category ? ' selected' : '') + '>' + escapeValue(x.label) + '</option>';
    });

    $.confirm({
        title: 'Dodaj pytanie',
        content: '' +
            '<form action="" class="formName">' +
            '<div class="form-group">' +
            '<label>Treść</label>' +
            '<input type="text" name="label" class="name form-control" value="" required />' +
            '</div>' +
            '<div class="mb-3">' +
            '<label for="formFile" class="form-label">Obraz (jeżeli wymagany)</label>' +
            '<input class="form-control" accept="image/jpeg, image/png" type="file" name="image">' +
            '</div>' +
            '<div class="form-group">' +
            '<label>Prawidłowa odpowiedź</label>' +
            '<select class="form-select" name="correctAnswer">' +
            '<option value="0" selected>Odpowiedź 1</option>' +
            '<option value="1">Odpowiedź 2</option>' +
            '<option value="2">Odpowiedź 3</option>' +
            '<option value="3">Odpowiedź 4</option>' +
            '</select>' +
            '</div>' +
            '<div class="form-group">' +
            '<label>Odpowiedź 1</label>' +
            '<input type="text" name="answer0" class="name form-control" value="" required />' +
            '</div>' +
            '<div class="form-group">' +
            '<label>Odpowiedź 2</label>' +
            '<input type="text" name="answer1" class="name form-control" value="" required />' +
            '</div>' +
            '<div class="form-group">' +
            '<label>Odpowiedź 3</label>' +
            '<input type="text" name="answer2" class="name form-control" value="" required />' +
            '</div>' +
            '<div class="form-group">' +
            '<label>Odpowiedź 4</label>' +
            '<input type="text" name="answer3" class="name form-control" value="" required />' +
            '</div>' +
            '<div class="form-group">' +
            '<label>Kategorie</label>' +
            '<select class="form-select" size="15" multiple="multiple" name="categories">' +
            categoriesSelect +
            '</select>' +
            '</div>' +
            '</form>',
        theme: 'dark',
        buttons: {
            formSubmit: {
                text: 'Dodaj',
                action: function () {
                    const content = this.$content[0];

                    const text = content.querySelector('input[name="label"]').value.trim();
                    const correctAnswer = content.querySelector('select[name="correctAnswer"]').value.trim();
                    const answer0 = content.querySelector('input[name="answer0"]').value.trim();
                    const answer1 = content.querySelector('input[name="answer1"]').value.trim();
                    const answer2 = content.querySelector('input[name="answer2"]').value.trim();
                    const answer3 = content.querySelector('input[name="answer3"]').value.trim();

                    const image = content.querySelector('input[name="image"]');

                    const selectedCategoriesItems = content.querySelectorAll('select[name="categories"] option:checked');
                    const selectedCategories = Array.from(selectedCategoriesItems).map(el => el.value);

                    if (!text || !correctAnswer || !answer0 || !answer1 || !answer2 || !answer3 || selectedCategories.length == 0) {
                        toastr.error(translateCode('invalid_model'));
                        return false;
                    }

                    var data = new FormData();

                    if (image.files.length > 0) {
                        data.append('image', image.files[0]);
                    }
                    data.append('text', text);
                    data.append('correctAnswer', correctAnswer);
                    data.append('answer0', answer0);
                    data.append('answer1', answer1);
                    data.append('answer2', answer2);
                    data.append('answer3', answer3);
                    data.append('selectedCategories', selectedCategories.join(','));

                    fetch(addQuestionRequestUrl, {
                        method: 'POST',
                        mode: 'cors',
                        cache: 'no-cache',
                        credentials: 'same-origin',
                        headers: {
                            'X-Csrf-Token-Value': csrfToken
                        },
                        redirect: 'follow',
                        referrerPolicy: 'no-referrer',
                        body: data
                    })
                        .then((response) => {
                            if (response.status > 299 || response.status < 200) {
                                return { errorCode: response.status, error: 'status_not_success' };
                            }
                            return response.json();
                        })
                        .then(data => {
                            if (data.errorCode == 413) {
                                toastr.error(translateCode('image_too_large'));
                                return;
                            }


                            if (data.error) {
                                toastr.error(translateCode(data.error));
                                return;
                            }

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

function playSoloClick() {
    const category = getSelectedCategoryId();
    if (!category) {
        return;
    }

    makePostRequest(createSoloGameUrl, { categoryId: category })
        .then(data => {
            if (data.error) {
                toastr.error(translateCode(data.error));
                return;
            }

            window.location = gameIndexUrl;
        })
        .catch((error) => {
            toastr.error(error.toString());
        });
}

(function () {
    showMessagesFromUrl();
})();