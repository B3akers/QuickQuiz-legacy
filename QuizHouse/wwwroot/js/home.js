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