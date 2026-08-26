// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
const randomRepositoryForm = document.getElementById("random-repository-form");

randomRepositoryForm?.addEventListener("submit", event => {
    event.preventDefault();

    const button = document.getElementById("random-repository-button");
    const spinner = document.getElementById("random-repository-spinner");
    const text = document.getElementById("random-repository-button-text");

    if (button) {
        button.disabled = true;
    }

    spinner?.classList.remove("d-none");

    if (text) {
        text.textContent = "Finding repository...";
    }

    requestAnimationFrame(() => {
        requestAnimationFrame(() => {
            randomRepositoryForm.submit();
        });
    });
});