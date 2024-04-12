import { CimonNotifier } from './cimon-notifier';
import process from 'process';
import { WindowsNotifier } from './notifier.win';
import { NotifierElectron } from './notifier.electron';
import isDev from 'electron-is-dev';

export async function initializeCimonNotifier(){
  const notifier: CimonNotifier = process.platform === 'win32' ? new WindowsNotifier() : new NotifierElectron();
  //if notifications not visible run once with 'true' argument to fix registration
  await notifier.init(isDev, false);
  CimonNotifier.Instance = notifier;
  return notifier;
}
