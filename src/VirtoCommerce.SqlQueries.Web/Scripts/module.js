// Call this to register your module to main application
var moduleName = 'VirtoCommerce.SqlQueries';

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
                                id: 'blade1',
                                controller: 'VirtoCommerce.SqlQueries.helloWorldController',
                                template: 'Modules/$(VirtoCommerce.SqlQueries)/Scripts/blades/hello-world.html',
                                isClosingDisabled: true,
                            };
                            bladeNavigationService.showBlade(newBlade);
                        }
                    ]
                });
        }
    ])
    .run(['platformWebApp.mainMenuService', '$state',
        function (mainMenuService, $state) {
            //Register module in main menu
            var menuItem = {
                path: 'browse/sql-queries',
                icon: 'fa fa-cube',
                title: 'SqlQueries',
                priority: 100,
                action: function () { $state.go('workspace.SqlQueriesState'); },
                permission: 'sql-queries:access',
            };
            mainMenuService.addMenuItem(menuItem);
        }
    ]);
