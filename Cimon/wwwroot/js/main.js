class InteractiveAPI {
    highlightElementByHash(){
        const hash = window.location.hash;
        const className = 'highlighted-by-hash';
        const previousElement = document.querySelector(className);
        if (previousElement) {
            previousElement.classList.remove(className);
        }
        if (hash) {
            const targetElement = document.querySelector(hash);
            if (targetElement) {
                targetElement.classList.add(className);
            }
        }
    }
}
window.interactiveAPI = new InteractiveAPI();