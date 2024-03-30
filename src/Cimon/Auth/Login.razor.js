export class LoginManager {
  static async doAutologin(){
    const response = await fetch('/auth/autologin', {credentials: 'same-origin'});
    return response.ok;
  }
}
window.LoginManager = LoginManager;
