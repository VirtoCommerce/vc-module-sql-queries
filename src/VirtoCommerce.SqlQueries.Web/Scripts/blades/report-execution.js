angular.module('VirtoCommerce.SqlQueriesModule')
    .controller('VirtoCommerce.SqlQueriesModule.reportExecutionController', [
        '$scope', 'platformWebApp.metaFormsService', 'VirtoCommerce.SqlQueriesModule.sqlQueriesApi',
        function ($scope, metaFormsService, sqlQueriesApi) {
            const blade = $scope.blade;

            blade.formats = sqlQueriesApi.getFormats();

            //blade properties
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

                sqlQueriesApi.executeReport({ id: blade.currentEntity.id, format: blade.format }, blade.parameters, function (response) {
                    var contentType = response.headers['content-type'];
                    var blob = new Blob([response.data], { type: contentType });

                    var fileName = 'report.' + blade.format;
                    var contentDisposition = response.headers['content-disposition'];

                    if (contentDisposition && contentDisposition.indexOf('attachment') !== -1) {
                        var fileNameRegex = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/;
                        var matches = fileNameRegex.exec(contentDisposition);
                        if (matches != null && matches[1]) {
                            fileName = matches[1].replace(/['"]/g, '');
                        }
                    }

                    var downloadUrl = URL.createObjectURL(blob);

                    // Create a temporary link to trigger download
                    var link = document.createElement('a');
                    link.href = downloadUrl;
                    link.download = fileName; // Desired file name
                    document.body.appendChild(link);
                    link.click();
                    document.body.removeChild(link);

                    // Clean up
                    URL.revokeObjectURL(downloadUrl);
                    blade.isLoading = false;
                }, function (error) {
                    blade.isLoading = false;
                    console.error('File download failed:', error);
                });
            };

            function initializeToolbar() {
                blade.toolbarCommands = [
                    {
                        name: 'Execute',
                        icon: 'fas fa-save',
                        executeMethod: function () {
                            $scope.executeReport();
                        },
                        canExecuteMethod: function () {
                            return formScope && formScope.$valid;
                        },
                        //permission: getSavePermission()
                    }
                ];
            }

            const dateTimeParameters = blade.parameters.filter(p => p.type === 'DateTime');

            dateTimeParameters.forEach((parameter) => {
                blade.datepickers[parameter.name] = false;
            });

            //calls
            initializeToolbar();
            blade.refresh();
        }
    ]);
