const { app, BrowserWindow } = require('electron')

const createWindow = () => {
  const win = new BrowserWindow({
    width: 400,
    height: 800,
    autoHideMenuBar: true
  })

  win.loadURL('http://localhost:5001/monitor/bpms');
}

app.whenReady().then(() => {
  createWindow()
})