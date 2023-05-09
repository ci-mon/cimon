export interface ICimonAPI {
    init: () => Promise<void>,
}

declare global {
    interface Window {
        CimonDesktop: ICimonAPI
    }
}

(async function(){
    await window.CimonDesktop.init()
})();
console.log('👋 This message is being logged by "renderer.js", included via webpack');
