// Focuses the element once it is linked when the expression is truthy.
// Intended for elements rendered with ng-if, e.g. an inline editor that must receive focus when it appears.
angular.module('VirtoCommerce.SqlQueriesModule')
    .directive('sqlQueriesFocusIf', ['$timeout', function ($timeout) {
        return {
            restrict: 'A',
            link: function (scope, element, attrs) {
                if (!scope.$eval(attrs.sqlQueriesFocusIf)) {
                    return;
                }

                $timeout(function () {
                    const el = element[0];
                    el.focus();

                    // place the caret at the end of the existing text
                    if (typeof el.setSelectionRange === 'function' && typeof el.value === 'string') {
                        el.setSelectionRange(el.value.length, el.value.length);
                    }
                });
            }
        };
    }]);
