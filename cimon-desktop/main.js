const { app, BrowserWindow } = require('electron');
const signalR = require("@microsoft/signalr");

const createWindow = async () => {
  const win = new BrowserWindow({
    width: 400,
    height: 800,
    autoHideMenuBar: true
  });

  let connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5001/user")
    .withAutomaticReconnect()
    .build();

  connection.onreconnected(() => {
    win.reload();
  }); 
  await connection.start();

  win.loadURL('http://localhost:5001/monitor/bpms?full-screen=true');
}

app.whenReady().then(() => {
  createWindow()
})