import { platform, arch } from 'process';

export const CimonConfig = {
  //url: 'http://tscore-dev-13:85',
  appId: 'com.squirrel.cimon_desktop.cimon-desktop',
  url: 'http://localhost:5001',
  getReleasesUrl(version: string) {
    const callbackUrl = Buffer.from(CimonConfig.url).toString('base64');
    return `${CimonConfig.url}/native/update/${callbackUrl}/${platform}/${arch}/${version}`;
  },
};
