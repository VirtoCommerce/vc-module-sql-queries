// Call this to register your module to main application
var moduleName = 'VirtoCommerce.SqlQueriesModule';

if (AppDependencies !== undefined) {
    AppDependencies.push(moduleName);
}

angular.module(moduleName, [])
    .config(['$stateProvider',
        function ($stateProvider) {
            $stateProvider
                .state('workspace.SqlQueriesState', {
                    url: '/sql-queries',
                    templateUrl: '$(Platform)/Scripts/common/templates/home.tpl.html',
                    controller: [
                        'platformWebApp.bladeNavigationService',
                        function (bladeNavigationService) {
                            var newBlade = {
                                id: 'blade',
                                controller: 'VirtoCommerce.SqlQueriesModule.sqlQueryListController',
                                template: 'Modules/$(VirtoCommerce.SqlQueries)/Scripts/blades/sql-query-list.html',
                                isClosingDisabled: true,
                            };
                            bladeNavigationService.showBlade(newBlade);
                        }
                    ]
                });
        }
    ])
    .run(['platformWebApp.mainMenuService', '$state', 'platformWebApp.metaFormsService',
        function (mainMenuService, $state, metaFormsService) {
            //Register module in main menu
            var menuItem = {
                path: 'browse/sql-queries',
                icon: 'fa fa-cube',
                title: 'sql-queries.title',
                priority: 100,
                action: function () { $state.go('workspace.SqlQueriesState'); },
                permission: 'sql-queries:access',
            };
            mainMenuService.addMenuItem(menuItem);

            metaFormsService.registerMetaFields('reportExecution', [
                {
                    templateUrl: 'formatSelector.html',
                    priority: 0
                },
                {
                    templateUrl: 'queryParameters.html',
                    priority: 1
                }
            ]);
        }
    ]);
