angular.module('VirtoCommerce.SqlQueriesModule')
    .controller('VirtoCommerce.SqlQueriesModule.sqlQueryDetailsController',
        [
            '$scope',
            'platformWebApp.bladeNavigationService', 'platformWebApp.dialogService',
            'VirtoCommerce.SqlQueriesModule.sqlQueriesApi',
            function (
                $scope,
                bladeNavigationService, dialogService,
                sqlQueriesApi)
            {
                const blade = $scope.blade;

                //blade properties
                blade.connectionStringNames = [];
                blade.types = ['ShortText', 'DateTime', 'Boolean', 'Integer', 'Decimal'];
                blade.title = blade.isNew ? 'sql-queries.blades.sql-query-details.title-add' : 'sql-queries.blades.sql-query-details.title';
                blade.titleValues = { name: blade.currentEntity.name ?? "" };

                // test mode state
                blade.testResults = null;
                blade.testError = null;
                blade.testLoading = false;
                blade.testParamValues = {};
                blade.testMaxRows = 100;
                blade.testDatepickers = {};
                blade.editingParams = {};
                blade.editorOptions = {
                    lineNumbers: true,
                    lineWrapping: true,
                    mode: 'text/x-sql',
                    extraKeys: { 'Ctrl-Space': 'autocomplete' },
                    hintOptions: { completeSingle: false }
                };
                blade.testGridOptions = {
                    enableSorting: true,
                    enableColumnResizing: true,
                    enableHorizontalScrollbar: 1,
                    enableVerticalScrollbar: 1,
                    minRowsToShow: 5,
                    data: [],
                    columnDefs: []
                };

                //blade functions
                blade.refresh = function () {
                    if (!blade.isNew) {
                        sqlQueriesApi.get({ id: blade.currentEntity.id }, function (data) {
                            blade.originalEntity = angular.copy(data);
                            blade.currentEntity = angular.copy(data);
                            blade.isLoading = false;
                        });
                    }
                    else {
                        blade.isLoading = false;
                        blade.currentEntity.parameters = [];
                    }

                    sqlQueriesApi.getDatabaseInformation({}, function (information) {
                        blade.connectionStringNames = information.connectionStringNames;
                        blade.databaseProvider = information.databaseProvider;
                        blade.editorOptions.mode = getSqlMimeType(information.databaseProvider);
                    });
                };

                blade.onClose = function (closeCallback) {
                    bladeNavigationService.showConfirmationIfNeeded(
                        isDirty(),
                        canSave(),
                        blade,
                        $scope.saveChanges,
                        closeCallback,
                        'sql-queries.dialogs.sql-query-save.title',
                        'sql-queries.dialogs.sql-query-save.message'
                    );
                };

                //scope functions
                let formScope;
                $scope.setForm = function (form) {
                    formScope = form;
                }

                $scope.saveChanges = function () {
                    blade.isLoading = true;

                    if (blade.isNew) {
                        sqlQueriesApi.create(blade.currentEntity, function () {
                            blade.parentBlade.refresh(true);
                            blade.originalEntity = angular.copy(blade.currentEntity);
                            blade.isLoading = false;
                            $scope.bladeClose();
                        }, function (error) {
                            bladeNavigationService.setError('Error ' + error.status, blade);
                            blade.isLoading = false;
                        });
                    }
                    else {
                        sqlQueriesApi.update(blade.currentEntity, function (updateResult) {
                            blade.parentBlade.refresh(true);
                            angular.copy(blade.currentEntity, updateResult);
                            blade.originalEntity = angular.copy(blade.currentEntity);
                            blade.isLoading = false;
                        }, function (error) {
                            bladeNavigationService.setError('Error ' + error.status, blade);
                            blade.isLoading = false;
                        });
                    }
                };

                $scope.addParameter = function () {
                    var params = blade.currentEntity.parameters;
                    var existingNames = _.pluck(params, 'name');
                    var index = 1;
                    while (_.contains(existingNames, 'param' + index)) {
                        index++;
                    }
                    params.push({ name: 'param' + index, type: 'ShortText' });
                };

                $scope.removeParameter = function (index) {
                    blade.currentEntity.parameters.splice(index, 1);
                    $scope.closeEditParam();
                };

                var escapeHandler = null;

                $scope.startEditParam = function (index) {
                    blade.editingParams = {};
                    blade.editingParams[index] = true;
                    bindEscapeKey();
                };

                $scope.finishEditParam = function (index) {
                    var param = blade.currentEntity.parameters[index];
                    if (param && param.name) {
                        $scope.closeEditParam();
                    }
                };

                $scope.closeEditParam = function () {
                    blade.editingParams = {};
                    unbindEscapeKey();
                };

                function bindEscapeKey() {
                    unbindEscapeKey();
                    escapeHandler = function (e) {
                        if (e.keyCode === 27) {
                            $scope.$apply(function () {
                                $scope.closeEditParam();
                            });
                        }
                    };
                    document.addEventListener('keydown', escapeHandler);
                }

                function unbindEscapeKey() {
                    if (escapeHandler) {
                        document.removeEventListener('keydown', escapeHandler);
                        escapeHandler = null;
                    }
                }

                $scope.$on('$destroy', function () {
                    unbindEscapeKey();
                });

                blade.openTestDatepicker = function ($event, paramName) {
                    $event.preventDefault();
                    $event.stopPropagation();
                    blade.testDatepickers[paramName] = true;
                };

                $scope.syncTestDatepickers = function () {
                    var currentParams = blade.currentEntity.parameters || [];
                    blade.testDatepickers = {};
                    currentParams.forEach(function (param) {
                        if (param.type === 'DateTime') {
                            blade.testDatepickers[param.name] = false;
                        }
                    });
                };

                $scope.syncTestParamDefaults = function () {
                    var currentParams = blade.currentEntity.parameters || [];
                    currentParams.forEach(function (param) {
                        if (!isEmptyValue(blade.testParamValues[param.name])) {
                            return;
                        }

                        switch (param.type) {
                            case 'DateTime': blade.testParamValues[param.name] = getToday(); break;
                            case 'Integer':
                            case 'Decimal': blade.testParamValues[param.name] = 0; break;
                            case 'Boolean': blade.testParamValues[param.name] = false; break;
                        }
                    });
                };

                function isEmptyValue(value) {
                    return angular.isUndefined(value) || value === null || value === '';
                }

                function getToday() {
                    var today = new Date();
                    today.setHours(0, 0, 0, 0);
                    return today;
                }

                $scope.runTestQuery = function () {
                    blade.testError = null;
                    blade.testResults = null;
                    blade.testLoading = true;

                    var parameters = (blade.currentEntity.parameters || []).map(function (param) {
                        return {
                            name: param.name,
                            type: param.type,
                            value: blade.testParamValues[param.name]
                        };
                    });

                    var request = {
                        query: blade.currentEntity.query,
                        connectionStringName: blade.currentEntity.connectionStringName,
                        parameters: parameters,
                        maxRows: blade.testMaxRows || 100
                    };

                    sqlQueriesApi.executeQuery(request, function (result) {
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

                $scope.$watchCollection('blade.currentEntity.parameters', function () {
                    $scope.syncTestDatepickers();
                    $scope.syncTestParamDefaults();
                });

                //local functions
                function isDirty() {
                    return !angular.equals(blade.currentEntity, blade.originalEntity);
                }

                function canSave() {
                    return isDirty() && formScope && formScope.$valid;
                }

                function reset() {
                    angular.copy(blade.originalEntity, blade.currentEntity);
                }

                function initializeToolbar() {
                    blade.toolbarCommands = [
                        {
                            name: 'platform.commands.save',
                            icon: 'fas fa-save',
                            executeMethod: function () {
                                $scope.saveChanges();
                            },
                            canExecuteMethod: canSave,
                            permission: getSavePermission()
                        }
                    ];

                    if (!blade.isNew) {
                        blade.toolbarCommands.push({
                            name: 'platform.commands.reset',
                            icon: 'fa fa-undo',
                            executeMethod: reset,
                            canExecuteMethod: isDirty
                        });
                    }
                }

                function getSavePermission() {
                    return blade.isNew ? 'sql-queries:create' : 'sql-queries:update';
                }

                function getSqlMimeType(provider) {
                    switch (provider) {
                        case 'SqlServer': return 'text/x-mssql';
                        case 'MySql': return 'text/x-mysql';
                        case 'PostgreSql': return 'text/x-pgsql';
                        default: return 'text/x-sql';
                    }
                }

                //calls
                initializeToolbar();
                blade.refresh();
            }
        ]
    );
