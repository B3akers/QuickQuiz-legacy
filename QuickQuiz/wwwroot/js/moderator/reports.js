var categoriesList = {};

function expandQuestionClick(target) {
    const currentTr = target.parentElement.parentElement;
    if (target.classList.replace('fa-plus', 'fa-minus')) {
        currentTr.nextElementSibling.classList.remove('d-none');
    } else if (target.classList.replace('fa-minus', 'fa-plus')) {
        currentTr.nextElementSibling.classList.add('d-none');
    }
}

function acceptReport(element, reportId) {
    const questionRow = element.nextElementSibling;

    let question = {};
    question.text = questionRow.querySelector('h4').innerText;
    question.categories = [];
    question.answers = [];

    element.querySelectorAll('div[data-category-id]').forEach(x => question.categories.push(x.dataset.categoryId));

    questionRow.querySelectorAll('button').forEach(x => {
        question.answers.push(x.querySelector('h5').innerText);
        if (x.hasAttribute('correct')) {
            question.correctAnswer = question.answers.length - 1;
        }
    });

    let categoriesSelect = '';
    Object.values(categoriesList).forEach(x => {
        categoriesSelect += '<option value="' + x.id + '"' + (question.categories.includes(x.id) ? ' selected' : '') + '>' + escapeValue(x.label) + '</option>';
    });

    $.confirm({
        title: 'Akceptuj zgłoszenie i popraw pytanie',
        content: '' +
            '<form action="" class="formName">' +
            '<div class="form-group">' +
            '<label>Treść</label>' +
            '<input type="text" name="label" class="name form-control" value="' + escapeValue(question.text) + '" required />' +
            '</div>' +
            '<div class="mb-3">' +
            '<label for="formFile" class="form-label">Obraz</label>' +
            '<input class="form-control" accept="image/jpeg, image/png" type="file" name="image">' +
            '</div>' +
            '<div class="form-group">' +
            '<label>Prawidłowa odpowiedź</label>' +
            '<select class="form-select" name="correctAnswer">' +
            '<option value="0" ' + (question.correctAnswer == 0 ? 'selected' : '') + '>Odpowiedź 1</option>' +
            '<option value="1" ' + (question.correctAnswer == 1 ? 'selected' : '') + '>Odpowiedź 2</option>' +
            '<option value="2" ' + (question.correctAnswer == 2 ? 'selected' : '') + '>Odpowiedź 3</option>' +
            '<option value="3" ' + (question.correctAnswer == 3 ? 'selected' : '') + '>Odpowiedź 4</option>' +
            '</select>' +
            '</div>' +
            '<div class="form-group">' +
            '<label>Odpowiedź 1</label>' +
            '<input type="text" name="answer0" class="name form-control" value="' + escapeValue(question.answers[0]) + '" required />' +
            '</div>' +
            '<div class="form-group">' +
            '<label>Odpowiedź 2</label>' +
            '<input type="text" name="answer1" class="name form-control" value="' + escapeValue(question.answers[1]) + '" required />' +
            '</div>' +
            '<div class="form-group">' +
            '<label>Odpowiedź 3</label>' +
            '<input type="text" name="answer2" class="name form-control" value="' + escapeValue(question.answers[2]) + '" required />' +
            '</div>' +
            '<div class="form-group">' +
            '<label>Odpowiedź 4</label>' +
            '<input type="text" name="answer3" class="name form-control" value="' + escapeValue(question.answers[3]) + '" required />' +
            '</div>' +
            '<div class="form-group">' +
            '<label>Kategorie</label>' +
            '<select class="form-select" size="15" multiple="multiple" name="categories">' +
            categoriesSelect +
            '</select>' +
            '</div>' +
            `<input type="hidden" name="id" value="${reportId}" />`+
            '</form>',
        theme: 'dark',
        buttons: {
            formSubmit: {
                text: 'Zaakceptuj',
                action: async function () {
                    const content = this.$content[0];

                    const label = content.querySelector('input[name="label"]').value.trim();
                    const image = content.querySelector('input[name="image"]').files[0];
                    const correctAnswer = content.querySelector('select[name="correctAnswer"]').value.trim();
                    const answer0 = content.querySelector('input[name="answer0"]').value.trim();
                    const answer1 = content.querySelector('input[name="answer1"]').value.trim();
                    const answer2 = content.querySelector('input[name="answer2"]').value.trim();
                    const answer3 = content.querySelector('input[name="answer3"]').value.trim();

                    const selectedCategoriesItems = content.querySelectorAll('select[name="categories"] option:checked');
                    const selectedCategories = Array.from(selectedCategoriesItems).map(el => el.value);

                    if (!label || !correctAnswer || !answer0 || !answer1 || !answer2 || !answer3 || selectedCategories.length == 0) {
                        toastr.error(translateCode('invalid_model'));
                        return false;
                    }

                    const data = { label: label, correctAnswer: parseInt(correctAnswer), answer0: answer0, answer1: answer1, answer2: answer2, answer3: answer3, selectedCategories: selectedCategories };
                    data.id = content.querySelector('input[name="id"]').value.trim();

                    if (image)
                        data.imageBase64 = await toBase64(image);

                    makePostRequest(acceptReportUrl, data)
                        .then(data => {
                            if (data.error) {
                                toastr.error(translateCode(data.error));
                                return;
                            }

                            element.nextElementSibling.remove();
                            element.remove();
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

function declineReport(element, reportId) {
    $.confirm({
        title: 'Potwierdź odrzucenie',
        content: 'Czy na pewno chcesz odrzucić to zgłoszenie?',
        theme: 'dark',
        buttons: {
            confirm: function () {
                makePostRequest(declineReportUrl, { id: reportId })
                    .then(data => {
                        if (data.error) {
                            toastr.error(translateCode(data.error));
                            return;
                        }

                        element.nextElementSibling.remove();
                        element.remove();
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

function reportsStartup() {
    document.querySelectorAll('div[data-category-id]').forEach(x => x.innerText = categoriesList[x.dataset.categoryId].label);
}

(function () {

    fetch(getCategoriesUrl, {
        credentials: 'same-origin'
    })
        .then(response => response.json())
        .then(data => {
            data.forEach(x => {
                categoriesList[x.id] = x;
            });

            reportsStartup();
        })
        .catch((error) => {
            toastr.error(error.toString());
        });


})();