export interface ICimonAPI {
    init: () => Promise<void>,
    skipInit: () => void
}

declare global {
    interface Window {
        CimonDesktop: ICimonAPI
    }
}