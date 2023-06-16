import type {Configuration} from 'webpack';
import {DefinePlugin} from 'webpack';

import {rules} from './webpack.rules';
import {plugins} from './webpack.plugins';
import {VueLoaderPlugin} from "vue-loader";
import {VuetifyPlugin} from "webpack-plugin-vuetify";


rules.push({
    test: /\.css$/,
    use: [{loader: 'style-loader'}, {loader: 'css-loader'}],
});

export const rendererConfig: Configuration = {
    module: {
        rules: [
            ...rules,
            {
                test: /\.vue$/,
                loader: 'vue-loader'
            }
        ],
    },
    plugins: [
        ...plugins,
        new DefinePlugin({
            __VUE_OPTIONS_API__: false,
            __VUE_PROD_DEVTOOLS__: false,
        }),
        new VuetifyPlugin({autoImport: true}),
        new VueLoaderPlugin()
    ],
    resolve: {
        extensions: ['.js', '.ts', '.jsx', '.tsx', '.css'],
    },
};
