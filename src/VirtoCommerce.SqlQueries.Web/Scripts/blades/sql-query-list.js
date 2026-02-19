angular.module('VirtoCommerce.SqlQueriesModule')
    .controller('VirtoCommerce.SqlQueriesModule.sqlQueryListController',
        [
            '$scope',
            'platformWebApp.bladeNavigationService', 'platformWebApp.dialogService', 'uiGridConstants', 'platformWebApp.uiGridHelper',
            'VirtoCommerce.SqlQueriesModule.sqlQueriesApi',
            function (
                $scope,
                bladeNavigationService, dialogService, uiGridConstants, uiGridHelper,
                sqlQueriesApi)
            {
                $scope.uiGridConstants = uiGridConstants;
                const blade = $scope.blade;
                blade.title = 'sql-queries.blades.sql-query-list.title';
                blade.currentEntities = [];

                blade.refresh = function () {
                    blade.isLoading = true;
                    sqlQueriesApi.search({}, function (data) {
                        blade.currentEntities = data.results;
                        blade.isLoading = false;
                    }, function (error) {
                        if (error.status === 403) {
                            // Fall back to reports endpoint for users without read permission
                            sqlQueriesApi.reports({}, function (data) {
                                blade.currentEntities = data.results;
                                blade.isLoading = false;
                            }, function (error2) {
                                bladeNavigationService.setError('Error ' + error2.status, blade);
                            });
                        } else {
                            bladeNavigationService.setError('Error ' + error.status, blade);
                        }
                    });
                };

                blade.selectNode = function (sqlQuery, isNew) {
                    $scope.selectedNodeId = sqlQuery.id;

                    if (isNew) {
                        const newBlade = {
                            id: 'sqlQueryDetailsBlade',
                            parentRefresh: blade.refresh,
                            controller: 'VirtoCommerce.SqlQueriesModule.sqlQueryDetailsController',
                            template: 'Modules/$(VirtoCommerce.SqlQueries)/Scripts/blades/sql-query-details.html',
                            currentEntity: sqlQuery,
                            isNew: true
                        };
                        bladeNavigationService.showBlade(newBlade, blade);
                        return;
                    }

                    blade.openReportView(sqlQuery);
                };

                blade.openReportView = function (sqlQuery) {
                    $scope.selectedNodeId = sqlQuery.id;

                    const newBlade = {
                        id: 'reportViewBlade',
                        controller: 'VirtoCommerce.SqlQueriesModule.reportViewController',
                        template: 'Modules/$(VirtoCommerce.SqlQueries)/Scripts/blades/report-view.html',
                        currentEntity: sqlQuery
                    };
                    bladeNavigationService.showBlade(newBlade, blade);
                };

                blade.editSqlQuery = function (sqlQuery) {
                    $scope.selectedNodeId = sqlQuery.id;

                    const newBlade = {
                        id: 'sqlQueryDetailsBlade',
                        parentRefresh: blade.refresh,
                        controller: 'VirtoCommerce.SqlQueriesModule.sqlQueryDetailsController',
                        template: 'Modules/$(VirtoCommerce.SqlQueries)/Scripts/blades/sql-query-details.html',
                        currentEntity: sqlQuery
                    };
                    bladeNavigationService.showBlade(newBlade, blade);
                };

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
                                    sqlQueriesApi.delete({ ids: ids }, function () {
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
