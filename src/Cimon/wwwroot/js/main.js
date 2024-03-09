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
                targetElement.scrollIntoViewIfNeeded();
            }
        }
    }
}
window.interactiveAPI = new InteractiveAPI();

window.blazorExtensions = {
    WriteCookie: function (name, value, days) {
        let expires;
        if (days) {
            const date = new Date();
            date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
            expires = "; expires=" + date.toGMTString();
        }
        else {
            expires = "";
        }
        document.cookie = `${name}=${value}${expires}; path=/`;
    }
}
class AuthApi {
    async autologin(returnUrl){
        window.location.href = `/auth/autologin?returnUrl=${returnUrl}`;
    }
}
window.authAPI = new AuthApi(); 

class UIApi{
    resetIcon() {
        this.setIcon('green');
    }
    setIcon(name){
        const icon = Array.from(document.head.childNodes).find(x=>x.rel === 'icon');
        if (icon) {
            const url = new URL(icon.href);
            icon.href = `/${name}.ico?${url.searchParams}`;
        }
    }
}

window.uiApi = new UIApi();
