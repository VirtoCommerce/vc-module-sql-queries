angular.module('VirtoCommerce.SqlQueriesModule')
    .controller('VirtoCommerce.SqlQueriesModule.sqlQueryListController',
        [
            '$scope',
            'platformWebApp.bladeNavigationService', 'platformWebApp.dialogService', 'uiGridConstants', 'platformWebApp.uiGridHelper', 'platformWebApp.authService',
            'VirtoCommerce.SqlQueriesModule.sqlQueriesApi',
            function (
                $scope,
                bladeNavigationService, dialogService, uiGridConstants, uiGridHelper, authService,
                sqlQueriesApi)
            {
                $scope.uiGridConstants = uiGridConstants;
                const blade = $scope.blade;
                blade.title = 'sql-queries.blades.sql-query-list.title';
                blade.currentEntities = [];

                blade.refresh = function () {
                    blade.isLoading = true;
                    if (authService.checkPermission('sql-queries:read')) {
                        sqlQueriesApi.search({}, function (data) {
                            blade.currentEntities = data.results;
                            blade.isLoading = false;
                        });
                    } else if (authService.checkPermission('sql-queries:reports')) {
                        sqlQueriesApi.reports({}, function (data) {
                            blade.currentEntities = data.results;
                            blade.isLoading = false;
                        });
                    }
                };

                blade.selectNode = function (sqlQuery, isNew) {
                    $scope.selectedNodeId = sqlQuery.id;

                    if (authService.checkPermission('sql-queries:read')) {
                        const newBlade = {
                            id: 'sqlQueryDetailsBlade',
                            parentRefresh: blade.refresh,
                            controller: 'VirtoCommerce.SqlQueriesModule.sqlQueryDetailsController',
                            template: 'Modules/$(VirtoCommerce.SqlQueries)/Scripts/blades/sql-query-details.html',
                            currentEntity: sqlQuery
                        };

                        if (isNew) {
                            angular.extend(newBlade, {
                                isNew: true,
                            });
                        }
                        bladeNavigationService.showBlade(newBlade, blade);
                    } else if (authService.checkPermission('sql-queries:reports')) {
                        const newBlade = {
                            id: 'reportExecutionBlade',
                            controller: 'VirtoCommerce.SqlQueriesModule.reportExecutionController',
                            template: 'Modules/$(VirtoCommerce.SqlQueries)/Scripts/blades/report-execution.html',
                            currentEntity: sqlQuery,
                        };
                        bladeNavigationService.showBlade(newBlade, blade);
                    }
                }

                blade.deleteSqlQuery = function (selection) {
                    bladeNavigationService.closeChildrenBlades(blade, function () {
                        const dialog = {
                            id: 'confirmDelete',
                            title: 'sql-queries.dialogs.sql-queries-delete.title',
                            message: 'sql-queries.dialogs.sql-queries-delete.message',
                            callback: function (remove) {
                                if (remove) {
                                    blade.isLoading = true;

                                    const ids = _.pluck(selection, 'id');
                                    sqlQueries.delete({ ids: ids }, function () {
                                        blade.refresh();
                                    },
                                        function (error) { bladeNavigationService.setError('Error ' + error.status, blade); });
                                }
                            }
                        };
                        dialogService.showConfirmationDialog(dialog);
                    });
                };

                blade.headIcon = 'fa fa-list-ol';

                blade.toolbarCommands = [
                    {
                        name: 'platform.commands.add', icon: 'fa fa-plus',
                        executeMethod: function () { blade.selectNode({}, true); },
                        canExecuteMethod: function () { return true; },
                        permission: 'sql-queries:create'
                    },
                    {
                        name: 'platform.commands.delete', icon: 'fa fa-trash-o',
                        executeMethod: function () { blade.deleteSqlQuery($scope.gridApi.selection.getSelectedRows()); },
                        canExecuteMethod: function () {
                            return $scope.gridApi && _.any($scope.gridApi.selection.getSelectedRows());
                        },
                        permission: 'sql-queries:delete'
                    },
                ];

                $scope.setGridOptions = function (gridOptions) {
                    uiGridHelper.initialize($scope, gridOptions);
                };

                blade.refresh();
            }
        ]
    );
