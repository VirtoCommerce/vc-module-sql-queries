// Register CodeMirror SQL mode and hint addons.
// CodeMirror core is provided globally by the platform vendor bundle (window.CodeMirror).
// Webpack externals redirect codemirror/lib/codemirror requires to the global.
require('codemirror/mode/sql/sql');
require('codemirror/addon/hint/show-hint');
require('codemirror/addon/hint/sql-hint');
require('codemirror/addon/hint/show-hint.css');
