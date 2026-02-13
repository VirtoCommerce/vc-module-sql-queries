const namespace = 'VirtoCommerce.SqlQueries'

const glob = require('glob');
const path = require('path');
const webpack = require('webpack');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');
const { CleanWebpackPlugin } = require('clean-webpack-plugin');

const rootPath = path.resolve(__dirname, 'dist');

function getEntryPoints() {
    return [
        ...glob.sync('./Scripts/**/*.js', { nosort: true }),
        ...glob.sync('./Content/**/*.css', { nosort: true }),
    ];
}

module.exports = (env, argv) => {
    const isProduction = argv.mode === 'production';

    return {
        entry: getEntryPoints(),
        devtool: false,
        output: {
            path: rootPath,
            filename: 'app.js',
        },
        module: {
            rules: [
                {
                    test: /\.css$/,
                    use: [MiniCssExtractPlugin.loader, 'css-loader'],
                }
            ]
        },
        externals: [
            // CodeMirror core is provided globally by the platform vendor bundle.
            // Prevent bundling a second copy when importing SQL mode and hint addons.
            function ({ request }, callback) {
                if (request && /[/\\]lib[/\\]codemirror(\.js)?$/.test(request)) {
                    return callback(null, 'CodeMirror');
                }
                callback();
            }
        ],
        plugins: [
            new CleanWebpackPlugin(),
            isProduction ?
                new webpack.SourceMapDevToolPlugin({
                    namespace: namespace,
                    filename: '[file].map[query]'
                }) :
                new webpack.SourceMapDevToolPlugin({
                    namespace: namespace
                }),
            new MiniCssExtractPlugin({
                filename: 'style.css',
            }),
        ],
    };
};
