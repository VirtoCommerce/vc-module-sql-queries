angular.module('VirtoCommerce.SqlQueriesModule')
    .controller('VirtoCommerce.SqlQueriesModule.reportViewController',
        [
            '$scope',
            'VirtoCommerce.SqlQueriesModule.sqlQueriesApi',
            function (
                $scope,
                sqlQueriesApi)
            {
                const blade = $scope.blade;

                // blade properties
                blade.title = 'sql-queries.blades.report-view.title';
                blade.titleValues = { name: blade.currentEntity.name ?? "" };

                // test mode state
                blade.testResults = null;
                blade.testError = null;
                blade.testLoading = false;
                blade.testParameters = [];
                blade.testMaxRows = 100;
                blade.testDatepickers = {};
                blade.testGridOptions = {
                    enableSorting: true,
                    enableColumnResizing: true,
                    enableHorizontalScrollbar: 1,
                    enableVerticalScrollbar: 1,
                    minRowsToShow: 5,
                    data: [],
                    columnDefs: []
                };

                // export state
                blade.exportFormats = [];
                blade.exportFormat = null;
                blade.exportLoading = false;

                // blade functions
                blade.refresh = function () {
                    sqlQueriesApi.get({ id: blade.currentEntity.id }, function (data) {
                        blade.currentEntity = data;
                        blade.titleValues = { name: data.name };
                        syncTestParameters();
                        blade.isLoading = false;
                        $scope.runTestQuery();
                    });
                };

                blade.openTestDatepicker = function ($event, paramName) {
                    $event.preventDefault();
                    $event.stopPropagation();
                    blade.testDatepickers[paramName] = true;
                };

                // scope functions
                $scope.runTestQuery = function () {
                    blade.testError = null;
                    blade.testResults = null;
                    blade.testLoading = true;

                    var request = {
                        parameters: blade.testParameters,
                        maxRows: blade.testMaxRows || 100
                    };

                    sqlQueriesApi.executeQueryById({ id: blade.currentEntity.id }, request, function (result) {
                        blade.testResults = result;
                        blade.testLoading = false;

                        if (result.columns && result.columns.length) {
                            blade.testGridOptions.columnDefs = result.columns.map(function (col, index) {
                                return {
                                    name: 'col_' + index,
                                    displayName: col.name,
                                    field: 'col_' + index,
                                    minWidth: 100,
                                    cellTooltip: true
                                };
                            });

                            blade.testGridOptions.data = result.rows.map(function (row) {
                                var obj = {};
                                result.columns.forEach(function (col, index) {
                                    obj['col_' + index] = row[index];
                                });
                                return obj;
                            });
                        } else {
                            blade.testGridOptions.columnDefs = [];
                            blade.testGridOptions.data = [];
                        }
                    }, function (error) {
                        blade.testLoading = false;
                        blade.testError = (error.data && (error.data.message || error.data.detail)) ||
                                          error.statusText || 'An error occurred while executing the query.';
                    });
                };

                $scope.exportReport = function () {
                    if (!blade.exportFormat) {
                        return;
                    }

                    blade.exportLoading = true;

                    sqlQueriesApi.executeReport(
                        { id: blade.currentEntity.id, format: blade.exportFormat },
                        blade.testParameters,
                        function (response) {
                            var contentType = response.headers['content-type'];
                            var blob = new Blob([response.data], { type: contentType });

                            var fileName = 'report.' + blade.exportFormat;
                            var contentDisposition = response.headers['content-disposition'];

                            if (contentDisposition && contentDisposition.indexOf('attachment') !== -1) {
                                var fileNameRegex = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/;
                                var matches = fileNameRegex.exec(contentDisposition);
                                if (matches != null && matches[1]) {
                                    fileName = matches[1].replace(/['"]/g, '');
                                }
                            }

                            var downloadUrl = URL.createObjectURL(blob);
                            var link = document.createElement('a');
                            link.href = downloadUrl;
                            link.download = fileName;
                            document.body.appendChild(link);
                            link.click();
                            document.body.removeChild(link);
                            URL.revokeObjectURL(downloadUrl);

                            blade.exportLoading = false;
                        },
                        function (error) {
                            blade.exportLoading = false;
                            console.error('File download failed:', error);
                        }
                    );
                };

                // local functions
                function syncTestParameters() {
                    var currentParams = blade.currentEntity.parameters || [];
                    var newTestParams = [];

                    currentParams.forEach(function (param) {
                        var existing = _.find(blade.testParameters, function (tp) {
                            return tp.name === param.name;
                        });
                        var value = existing ? existing.value : null;
                        if (isEmptyValue(value)) {
                            value = getDefaultParameterValue(param.type);
                        }
                        newTestParams.push({
                            name: param.name,
                            type: param.type,
                            value: value
                        });
                    });

                    blade.testParameters = newTestParams;

                    blade.testDatepickers = {};
                    currentParams.forEach(function (param) {
                        if (param.type === 'DateTime') {
                            blade.testDatepickers[param.name] = false;
                        }
                    });
                }

                function getDefaultParameterValue(type) {
                    switch (type) {
                        case 'DateTime': return getToday();
                        case 'Integer':
                        case 'Decimal': return 0;
                        case 'Boolean': return false;
                        default: return null;
                    }
                }

                function getToday() {
                    var today = new Date();
                    today.setHours(0, 0, 0, 0);
                    return today;
                }

                function isEmptyValue(value) {
                    return angular.isUndefined(value) || value === null || value === '';
                }

                function loadExportFormats() {
                    sqlQueriesApi.getFormats(function (formats) {
                        blade.exportFormats = formats;
                        if (formats && formats.length) {
                            blade.exportFormat = formats[0];
                        }
                    });
                }

                // init
                syncTestParameters();
                blade.toolbarCommands = [];
                loadExportFormats();
                blade.refresh();
            }
        ]
    );
