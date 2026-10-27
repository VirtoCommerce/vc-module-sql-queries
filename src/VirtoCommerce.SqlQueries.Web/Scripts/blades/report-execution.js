angular.module('VirtoCommerce.SqlQueriesModule')
    .controller('VirtoCommerce.SqlQueriesModule.reportExecutionController', [
        '$scope', 'platformWebApp.metaFormsService', 'VirtoCommerce.SqlQueriesModule.reportExportService',
        function ($scope, metaFormsService, reportExportService) {
            const blade = $scope.blade;

            //blade properties
            blade.formats = [];
            reportExportService.getFormats().then(function (formats) {
                blade.formats = formats;
            });
            blade.title = 'sql-queries.blades.report-execution.title';
            blade.titleValues = { name: blade.currentEntity.name };
            blade.metaFields = metaFormsService.getMetaFields('reportExecution');
            blade.parameters = angular.copy(blade.currentEntity.parameters || []);
            blade.datepickers = {};

            //blade functions
            blade.refresh = function () {
                blade.isLoading = false;
            };

            blade.open = function ($event, which) {
                $event.preventDefault();
                $event.stopPropagation();

                blade.datepickers[which] = true;
            };

            //scope functions
            let formScope;
            $scope.setForm = function (form) {
                formScope = form;
            }

            $scope.executeReport = function () {
                blade.isLoading = true;

                reportExportService.exportReport(blade.currentEntity.id, blade.format, blade.parameters)
                    .finally(function () {
                        blade.isLoading = false;
                    });
            };

            //local functions
            function initializeToolbar() {
                blade.toolbarCommands = [
                    {
                        name: 'platform.commands.save',
                        icon: 'fas fa-save',
                        executeMethod: function () {
                            $scope.executeReport();
                        },
                        canExecuteMethod: function () {
                            return formScope && formScope.$valid;
                        }
                    }
                ];
            }

            const dateTimeParameters = blade.parameters.filter(p => p.type === 'DateTime');

            dateTimeParameters.forEach((parameter) => {
                blade.datepickers[parameter.name] = false;
            });

            //apply default values when not specified
            blade.parameters.forEach(setDefaultParameterValue);

            function setDefaultParameterValue(parameter) {
                if (!isEmptyValue(parameter.value)) {
                    return;
                }

                switch (parameter.type) {
                    case 'DateTime': parameter.value = getToday(); break;
                    case 'Integer':
                    case 'Decimal': parameter.value = 0; break;
                    case 'Boolean': parameter.value = false; break;
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

            //calls
            initializeToolbar();
            blade.refresh();
        }
    ]);
