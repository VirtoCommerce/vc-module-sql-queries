angular.module('VirtoCommerce.SqlQueriesModule')
    .controller('VirtoCommerce.SqlQueriesModule.reportViewController',
        [
            '$scope',
            'platformWebApp.bladeNavigationService',
            'VirtoCommerce.SqlQueriesModule.sqlQueriesApi',
            function (
                $scope,
                bladeNavigationService,
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

                $scope.openExportBlade = function () {
                    const newBlade = {
                        id: 'reportExecutionBlade',
                        controller: 'VirtoCommerce.SqlQueriesModule.reportExecutionController',
                        template: 'Modules/$(VirtoCommerce.SqlQueries)/Scripts/blades/report-execution.html',
                        currentEntity: blade.currentEntity
                    };
                    bladeNavigationService.showBlade(newBlade, blade);
                };

                // local functions
                function syncTestParameters() {
                    var currentParams = blade.currentEntity.parameters || [];
                    var newTestParams = [];

                    currentParams.forEach(function (param) {
                        var existing = _.find(blade.testParameters, function (tp) {
                            return tp.name === param.name;
                        });
                        newTestParams.push({
                            name: param.name,
                            type: param.type,
                            value: existing ? existing.value : null
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

                function initializeToolbar() {
                    blade.toolbarCommands = [
                        {
                            name: 'sql-queries.commands.export-report',
                            icon: 'fa fa-download',
                            executeMethod: function () {
                                $scope.openExportBlade();
                            },
                            canExecuteMethod: function () {
                                return !blade.isLoading;
                            },
                            permission: 'sql-queries:read'
                        }
                    ];
                }

                // init
                syncTestParameters();
                initializeToolbar();
                blade.refresh();
            }
        ]
    );
