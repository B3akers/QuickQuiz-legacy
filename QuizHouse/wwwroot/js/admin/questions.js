var questionsDataTable;
var categoriesList = {};

function getDataById(id) {
    const data = questionsDataTable.rows().data();
    for (let i = 0; i < data.length; i++) {
        const obj = data[i];
        if (obj && obj.id == id)
            return obj;
    }
    return null;
}

function deleteQuestion(questionId) {
    deleteRecord(deleteQuestionUrl, questionsDataTable, questionId);
}

function modifyQuestion(create, questionId) {
    var question;
    if (!create) {
        question = getDataById(questionId);
        if (!question)
            return;
    }

    let categoriesSelect = '';
    Object.values(categoriesList).forEach(x => {
        categoriesSelect += '<option value="' + x.id + '"' + (create ? '' : question.categories.includes(x.id) ? ' selected' : '') + '>' + escapeValue(x.label) + '</option>';
    });

    $.confirm({
        title: create ? 'Dodaj nowe pytanie' : 'Modyfikuj pytanie',
        content: '' +
            '<form action="" class="formName">' +
            '<div class="form-group">' +
            '<label>Treść</label>' +
            '<input type="text" name="label" class="name form-control" value="' + (create ? '' : escapeValue(question.text)) + '" required />' +
            '</div>' +
            '<div class="form-group">' +
            '<label>Obraz</label>' +
            '<input type="text" name="image" class="name form-control" value="' + (create ? '' : escapeValue(question.image)) + '" />' +
            '</div>' +
            '<div class="form-group">' +
            '<label>Autor (ID)</label>' +
            '<input type="text" name="author" class="name form-control" value="' + (create ? '' : escapeValue(question.author)) + '" />' +
            '</div>' +
            '<div class="form-group">' +
            '<label>Prawidłowa odpowiedź</label>' +
            '<select class="form-select" name="correctAnswer">' +
            '<option value="0" ' + (create ? '' : (question.correctAnswer == 0 ? 'selected' : '')) + '>Odpowiedź 1</option>' +
            '<option value="1" ' + (create ? '' : (question.correctAnswer == 1 ? 'selected' : '')) + '>Odpowiedź 2</option>' +
            '<option value="2" ' + (create ? '' : (question.correctAnswer == 2 ? 'selected' : '')) + '>Odpowiedź 3</option>' +
            '<option value="3" ' + (create ? '' : (question.correctAnswer == 3 ? 'selected' : '')) + '>Odpowiedź 4</option>' +
            '</select>' +
            '</div>' +
            '<div class="form-group">' +
            '<label>Odpowiedź 1</label>' +
            '<input type="text" name="answer0" class="name form-control" value="' + (create ? '' : escapeValue(question.answers[0])) + '" required />' +
            '</div>' +
            '<div class="form-group">' +
            '<label>Odpowiedź 2</label>' +
            '<input type="text" name="answer1" class="name form-control" value="' + (create ? '' : escapeValue(question.answers[1])) + '" required />' +
            '</div>' +
            '<div class="form-group">' +
            '<label>Odpowiedź 3</label>' +
            '<input type="text" name="answer2" class="name form-control" value="' + (create ? '' : escapeValue(question.answers[2])) + '" required />' +
            '</div>' +
            '<div class="form-group">' +
            '<label>Odpowiedź 4</label>' +
            '<input type="text" name="answer3" class="name form-control" value="' + (create ? '' : escapeValue(question.answers[3])) + '" required />' +
            '</div>' +
            '<div class="form-group">' +
            '<label>Kategorie</label>' +
            '<select class="form-select" size="15" multiple="multiple" name="categories">' +
            categoriesSelect +
            '</select>' +
            '</div>' +
            (create ? '' : `<input type="hidden" name="id" value="${questionId}" />`) +
            '</form>',
        theme: 'dark',
        buttons: {
            formSubmit: {
                text: create ? 'Dodaj' : 'Modyfikuj',
                action: function () {
                    const content = this.$content[0];

                    const label = content.querySelector('input[name="label"]').value.trim();
                    const image = content.querySelector('input[name="image"]').value.trim();
                    const author = content.querySelector('input[name="author"]').value.trim();
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


                    const data = { label: label, image: image, author: author, correctAnswer: parseInt(correctAnswer), answer0: answer0, answer1: answer1, answer2: answer2, answer3: answer3, selectedCategories: selectedCategories };
                    if (!create) {
                        data.id = content.querySelector('input[name="id"]').value.trim();
                    }

                    makePostRequest(create ? addQuestionUrl : editQuestionUrl, data)
                        .then(data => {
                            if (data.error) {
                                toastr.error(translateCode(data.error));
                                return;
                            }

                            questionsDataTable.ajax.reload();
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

function expandQuestionClick(target) {
    const currentTr = target.parentElement.parentElement;
    if (target.classList.replace('fa-plus', 'fa-minus')) {
        let newTr = document.createElement('tr');
        let td = document.createElement('td');
        td.setAttribute('colspan', '4');

        const questionData = getDataById(target.dataset.id);
        if (questionData) {
            const answers = questionData.answers;

            let questionsHTML = '';
            for (let i = 0; i < answers.length; i++) {
                const answer = answers[i];
                let attribute = i == questionData.correctAnswer ? 'correct' : 'wrong';
                questionsHTML += `<div class="row justify-content-center"><button type="button" ${attribute}="" class="btn btn-question mb-3" disabled><h5>${escapeValue(answer)}</h5></button></div>`;
            }

            td.innerHTML = `<div class="text-center">
		<div class="p-2">
			<h4>${escapeValue(questionData.text)}</h4>
		</div>
		<div class="p-3 question-img">
			<img width="250" height="250" src="${escapeValue(questionData.image)}" style="object-fit: contain;" />
		</div>
		<div class="question-table">
        ${questionsHTML}
		</div>
	</div>`;
        }

        newTr.appendChild(td);
        currentTr.after(newTr);
    } else if (target.classList.replace('fa-minus', 'fa-plus')) {
        currentTr.nextElementSibling.remove();
    }
}

function loadTable() {
    const table = document.getElementById('questionsTable');

    questionsDataTable = $(table).DataTable({
        pageLength: 15,
        serverSide: true,
        order: [],
        columns: [
            {
                name: "id",
                data: "id",
                render: function (data, type) {
                    if (type === "display") {
                        return `<i role="button" data-id="${data}" onclick="expandQuestionClick(this)" class="fa fa-lg fa-plus m-2"></i>${data}`;
                    }
                    return data;
                },
                orderable: false
            },
            {
                name: "author",
                data: "author",
                render: function (data, type) {
                    if (!data) {
                        return '';
                    }

                    if (type === "display") {
                        return `<a class="link-info" target="_blank" href="${userProfileUrl}/${data}">Autor</a>`;
                    }
                    return data;
                },
                orderable: false
            },
            {
                name: "categories",
                data: "categories",
                render: function (data, type) {
                    if (type === "display") {
                        const categories = [];
                        data.forEach(x => {
                            let category = categoriesList[x];
                            categories.push(category ? category.label : 'Usunięta');
                        });

                        return categories.join('<br/>');
                    }
                    return data;
                },
                orderable: false
            },
            {
                name: "action",
                data: "id",
                render: function (data, type) {
                    if (type === "display") {
                        return `<i role="button" title="Edytuj pytanie" onclick="modifyQuestion(false, this.dataset.id)" data-id="${data}" class="fa fa-lg fa-pencil"></i>` +
                            `<i role="button" title="Usuń pytanie" onclick="deleteQuestion(this.dataset.id)" data-id="${data}" class="fa fa-lg fa-trash ms-3"></i>`;
                    }
                    return data;
                },
                orderable: false
            }
        ],
        ajax: {
            url: getQuestionsUrl,
            contentType: "application/json",
            type: "POST",
            data: function (d) {
                return JSON.stringify(d);
            }
        },
        initComplete: function () {
            this.api()
                .columns()
                .every(function () {
                    var column = this;
                    if (column.index() != 2)
                        return;
                    var select = $('<select><option value=""></option></select>')
                        .appendTo($(column.footer()).empty())
                        .on('change', function () {
                            var val = $.fn.dataTable.util.escapeRegex($(this).val());

                            column.search(val ? val : '', false, false).draw();
                        });

                    Object.values(categoriesList).forEach(x => {
                        select.append('<option value="' + x.id + '">' + escapeValue(x.label) + '</option>');
                    });

                });
        },
    });
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

            loadTable();
        })
        .catch((error) => {
            toastr.error(error.toString());
        });


})();