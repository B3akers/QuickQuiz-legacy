function getSelectedCategoryId() {
    const selectedCategory = document.querySelector('.category-selection.show');
    if (!selectedCategory) {
        return undefined;
    }

    return selectedCategory.dataset.categoryId;
}

function playSoloClick() {
    const category = getSelectedCategoryId();
    if (!category) {
        return;
    }

}