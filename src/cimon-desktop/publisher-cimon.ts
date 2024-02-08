import { PublisherBase, PublisherOptions } from '@electron-forge/publisher-base';
import { createReadStream, statSync } from 'fs';
import { basename } from 'path';
import * as FormData from 'form-data';
import * as fetch from 'node-fetch';
import * as parseChangelog from 'changelog-parser';

export interface PublisherCimonConfig {
  host: string;
  token: string;
}
export class PublisherCimon extends PublisherBase<PublisherCimonConfig> {
  name = 'cimon';

  private collapseMakeResults = (makeResults: PublisherOptions['makeResults']) => {
    const newMakeResults: typeof makeResults = [];
    for (const result of makeResults) {
      const existingResult = newMakeResults.find(
        (nResult) =>
          nResult.arch === result.arch &&
          nResult.platform === result.platform &&
          nResult.packageJSON.version === result.packageJSON.version
      );
      if (existingResult) {
        existingResult.artifacts.push(...result.artifacts);
      } else {
        newMakeResults.push({ ...result });
      }
    }
    return newMakeResults;
  };

  async publish({ makeResults, setStatusLine }: PublisherOptions): Promise<void> {
    const { config } = this;

    const collapsedResults = this.collapseMakeResults(makeResults);

    for (const [resultIdx, makeResult] of collapsedResults.entries()) {
      const data = new FormData();
      data.append('platform', makeResult.platform, { contentType: 'text/plain' });
      data.append('arch', makeResult.arch, { contentType: 'text/plain' });
      data.append('version', makeResult.packageJSON.version, { contentType: 'text/plain' });
      let artifactIdx = 0;
      for (const artifactPath of makeResult.artifacts) {
        const fileName = basename(artifactPath);
        if (fileName.toLowerCase() === 'releases') continue;
        data.append(`file${artifactIdx}`, createReadStream(artifactPath), {
          knownLength: statSync(artifactPath).size,
          contentType: 'application/octet-stream',
        });
        artifactIdx += 1;
      }
      const changelog = await parseChangelog('./CHANGELOG.md');
      const currentVersionInfo = changelog?.versions?.find(
        (v: { version: string }) => v.version === makeResult.packageJSON.version
      )?.body;
      if (currentVersionInfo) {
        setStatusLine(`Found changes for current version`);
        data.append('changes', currentVersionInfo, { contentType: 'text/plain' });
      }
      const msg = `Uploading distributable (${resultIdx + 1}/${collapsedResults.length}) ${process.cwd()}`;
      setStatusLine(msg);
      const response = await fetch(`${config.host}/native/upload/${makeResult.packageJSON.build.appId}`, {
        headers: {
          Authorization: config.token,
        },
        method: 'POST',
        body: data,
      });
      if (response.status !== 200) {
        throw new Error(`Unexpected response code from cimon: ${response.status}\n\nBody:\n${await response.text()}`);
      }
    }
  }
}
