angular.module('VirtoCommerce.SqlQueriesModule')
    .factory('VirtoCommerce.SqlQueriesModule.reportExportService',
        [
            '$q', 'VirtoCommerce.SqlQueriesModule.sqlQueriesApi',
            function ($q, sqlQueriesApi) {
                // PDF is produced in the browser: the HTML report is printed and the user picks "Save as PDF"
                var pdfFormat = 'pdf';
                var htmlFormat = 'html';
                // Printable width in CSS px of the narrower of A4 and Letter landscape with the report's 10 mm margins
                var printableWidth = 980;
                // Below this the text is too small to read, so wider reports overflow the page instead of shrinking further
                var minPrintZoom = 0.5;
                var printFrame = null;
                var printPageStyle = null;

                function getFormats() {
                    return sqlQueriesApi.getFormats().$promise.then(function (formats) {
                        var result = _.filter(formats, function (format) {
                            return format !== pdfFormat;
                        });

                        var htmlIndex = result.indexOf(htmlFormat);
                        if (htmlIndex !== -1) {
                            result.splice(htmlIndex + 1, 0, pdfFormat);
                        }

                        return result;
                    });
                }

                function exportReport(id, format, parameters) {
                    var isPdf = format === pdfFormat;
                    var deferred = $q.defer();

                    sqlQueriesApi.executeReport({ id: id, format: isPdf ? htmlFormat : format }, parameters, function (response) {
                        var fileName = getFileName(response.headers['content-disposition'], format);

                        if (isPdf) {
                            printHtml(new TextDecoder('utf-8').decode(response.data), stripExtension(fileName));
                        } else {
                            downloadFile(response.data, response.headers['content-type'], fileName);
                        }

                        deferred.resolve();
                    }, function (error) {
                        console.error('Report export failed:', error);
                        deferred.reject(error);
                    });

                    return deferred.promise;
                }

                function getFileName(contentDisposition, format) {
                    var fileName = 'report.' + format;

                    if (contentDisposition && contentDisposition.indexOf('attachment') !== -1) {
                        var fileNameRegex = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/;
                        var matches = fileNameRegex.exec(contentDisposition);
                        if (matches != null && matches[1]) {
                            fileName = matches[1].replace(/['"]/g, '');
                        }
                    }

                    return fileName;
                }

                function stripExtension(fileName) {
                    var dotIndex = fileName.lastIndexOf('.');
                    return dotIndex > 0 ? fileName.substring(0, dotIndex) : fileName;
                }

                function downloadFile(data, contentType, fileName) {
                    var blob = new Blob([data], { type: contentType });
                    var downloadUrl = URL.createObjectURL(blob);

                    var link = document.createElement('a');
                    link.href = downloadUrl;
                    link.download = fileName;
                    document.body.appendChild(link);
                    link.click();
                    document.body.removeChild(link);

                    URL.revokeObjectURL(downloadUrl);
                }

                function printHtml(html, title) {
                    // The previous frame is kept until the next print: some browsers render the print preview asynchronously
                    cleanUpPrint();

                    // Sandboxed without allow-scripts: the report is static markup and must never run code in the admin origin
                    printFrame = document.createElement('iframe');
                    printFrame.setAttribute('sandbox', 'allow-same-origin allow-modals');
                    printFrame.setAttribute('aria-hidden', 'true');
                    // Off screen but laid out at page width, so the report can be measured before printing
                    printFrame.style.cssText = 'position: fixed; left: -10000px; top: 0; width: ' + printableWidth + 'px; height: 100px; border: 0;';

                    printFrame.onload = function () {
                        var frameWindow = printFrame.contentWindow;
                        // The browser suggests the document title as the PDF file name
                        frameWindow.document.title = title;
                        fitToPageWidth(frameWindow.document);

                        // Some browsers take the page size from the top document when printing a frame
                        printPageStyle = document.createElement('style');
                        printPageStyle.textContent = '@page { size: landscape; margin: 12mm 10mm; }';
                        document.head.appendChild(printPageStyle);
                        frameWindow.addEventListener('afterprint', removePrintPageStyle);

                        frameWindow.focus();
                        frameWindow.print();
                    };

                    // The frame only ever renders for print, so the print rules also apply while it is measured
                    printFrame.srcdoc = html.replace(/@media\s+print/g, '@media all');
                    document.body.appendChild(printFrame);
                }

                function fitToPageWidth(frameDocument) {
                    var table = frameDocument.querySelector('table');
                    if (!table) {
                        return;
                    }

                    table.style.width = 'min-content';
                    var minWidth = table.offsetWidth;
                    table.style.width = 'max-content';
                    var maxWidth = table.offsetWidth;
                    table.style.width = '';

                    // Some room above the narrowest layout keeps wrapped values such as ids and dates readable
                    var targetWidth = Math.min(maxWidth, minWidth * 1.3);
                    if (targetWidth > printableWidth) {
                        frameDocument.documentElement.style.zoom = Math.max(minPrintZoom, printableWidth / targetWidth);
                    }
                }

                function removePrintPageStyle() {
                    if (printPageStyle) {
                        printPageStyle.remove();
                        printPageStyle = null;
                    }
                }

                function cleanUpPrint() {
                    removePrintPageStyle();

                    if (printFrame) {
                        printFrame.remove();
                        printFrame = null;
                    }
                }

                return {
                    getFormats: getFormats,
                    exportReport: exportReport
                };
            }
        ]);
