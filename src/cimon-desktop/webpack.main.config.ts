import type { Configuration } from 'webpack';

import { rules } from './webpack.rules';
import path from "path";

export const mainConfig: Configuration = {
  infrastructureLogging: {
    appendOnly: true,
    level: 'info'
  },
  /**
   * This is the main entry point for your application, it's the first file
   * that runs in the main process.
   */
  entry: './src/index.ts',
  // Put your normal webpack config below here
  module: {
    rules: [
      ...rules,
      {
        test: /node_modules(.*)@microsoft(.*)signalr(.*)\.js/,
        use: {
          loader: path.resolve('SignalRLoader.js')
        },
      }
    ],
  },
  resolve: {
    extensions: ['.js', '.ts', '.jsx', '.tsx', '.css', '.json'],
  }
};
