angular.module('VirtoCommerce.SqlQueriesModule')
    .factory('VirtoCommerce.SqlQueriesModule.sqlQueriesApi',
        [
            '$resource', function ($resource) {
                return $resource('api/sql-queries/',
                    {},
                    {
                        get: { url: 'api/sql-queries/:id', method: 'GET' },
                        search: { url: 'api/sql-queries/search', method: 'POST' },
                        reports: { url: 'api/sql-queries/reports', method: 'POST' },
                        create: { url: 'api/sql-queries/', method: 'POST' },
                        update: { url: 'api/sql-queries/', method: 'PUT' },
                        delete: { url: 'api/sql-queries/', method: 'DELETE' },
                        getFormats: { url: 'api/sql-queries/formats', method: 'GET', isArray: true },
                        getDatabaseInformation: { url: 'api/sql-queries/database-information', method: 'GET' },
                        executeReport: {
                            url: 'api/sql-queries/execute/:id/:format',
                            method: 'POST',
                            responseType: 'arraybuffer',
                            transformResponse: function (data, headers) {
                                return { data: data, headers: headers() };
                            }
                        }
                    }
                );
            }
        ]);
