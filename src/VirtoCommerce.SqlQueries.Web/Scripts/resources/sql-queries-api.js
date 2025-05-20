angular.module('VirtoCommerce.SqlQueries')
    .factory('VirtoCommerce.SqlQueries.webApi', ['$resource', function ($resource) {
        return $resource('api/sql-queries');
    }]);
