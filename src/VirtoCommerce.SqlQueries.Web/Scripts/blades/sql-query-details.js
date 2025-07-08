angular.module('VirtoCommerce.SqlQueriesModule')
    .controller('VirtoCommerce.SqlQueriesModule.sqlQueryDetailsController',
        [
            '$scope',
            'platformWebApp.bladeNavigationService', 'platformWebApp.dialogService', 'uiGridConstants', 'platformWebApp.uiGridHelper',
            'VirtoCommerce.SqlQueriesModule.sqlQueriesApi',
            function (
                $scope,
                bladeNavigationService, dialogService, uiGridConstants, uiGridHelper,
                sqlQueriesApi)
            {
                const blade = $scope.blade;
                $scope.uiGridConstants = uiGridConstants;

                //blade properties
                blade.connectionStringNames = [];
                blade.types = ['ShortText', 'DateTime', 'Boolean', 'Integer', 'Decimal'];
                blade.title = blade.isNew ? 'sql-queries.blades.sql-query-details.title-add' : 'sql-queries.blades.sql-query-details.title';
                blade.titleValues = { name: blade.currentEntity.name ?? "" };
            
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
                    }

                    sqlQueriesApi.getDatabaseInformation({}, function (information) {
                        blade.connectionStringNames = information.connectionStringNames;
                        blade.databaseProvider = information.databaseProvider;
                    });
                };

                blade.onClose = function (closeCallback) {
                    bladeNavigationService.showConfirmationIfNeeded(
                        isDirty(),
                        canSave(),
                        blade,
                        $scope.saveChanges,
                        closeCallback,
                        'sql-queries.dialogs.news-article-save.title',
                        'sql-queries.dialogs.news-article-save.message'
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

                $scope.openReportExecutionBlade = function () {
                    const newBlade = {
                        id: 'reportExecutionBlade',
                        controller: 'VirtoCommerce.SqlQueriesModule.reportExecutionController',
                        template: 'Modules/$(VirtoCommerce.SqlQueries)/Scripts/blades/report-execution.html',
                        currentEntity: blade.currentEntity,
                        formats: []
                    };

                    bladeNavigationService.showBlade(newBlade, blade);
                }

                $scope.deleteRows = function (rows) {
                    var dialog = {
                        id: 'confirmDelete',
                        title: 'sql-queries.dialogs.parameters-delete.title',
                        message: 'sql-queries.dialogs.parameters-delete.message',
                        callback: function (remove) {
                            if (remove) {
                                _.each(rows, function (row) {
                                    blade.currentEntity.parameters.splice(blade.currentEntity.parameters.indexOf(row), 1);
                                });
                            }
                        }
                    }
                    dialogService.showConfirmationDialog(dialog);
                };

                $scope.setGridOptions = function (gridOptions) {
                    uiGridHelper.initialize($scope, gridOptions,
                        function (gridApi) {
                        });
                };

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
                        },
                        {
                            name: 'sql-queries.commands.add-parameter',
                            icon: 'fa fa-plus',
                            executeMethod: function () {
                                blade.currentEntity.parameters.push({ name: '', type: 'ShortText' });
                            },
                            canExecuteMethod: function () {
                                return true;
                            }
                        }
                    ];

                    if (!blade.isNew) {
                        blade.toolbarCommands.push({
                            name: 'platform.commands.reset',
                            icon: 'fa fa-undo',
                            executeMethod: reset,
                            canExecuteMethod: isDirty
                        });

                        blade.toolbarCommands.push({
                            name: 'sql-queries.commands.try-query',
                            icon: 'fa fa-play',
                            executeMethod: function () {
                                $scope.openReportExecutionBlade();
                            },
                            canExecuteMethod: function () {
                                return !isDirty() && formScope && formScope.$valid;
                            }
                        });
                    }
                }

                function getSavePermission() {
                    return blade.isNew ? 'sql-queries:create' : 'sql-queries:update';
                }

                //calls
                initializeToolbar();
                blade.refresh();
            }
        ]
    );
