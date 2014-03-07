lucidChart = {
    issueId: '',

    projectId: '',

    projectCode: '',

    url: '',

    appId: '',

    controlId: '',

    init: function (appId, controlId, url, issueId, projectId, projectCode)
    {
        this.appId = appId;
        this.controlId = controlId;
        this.url = url;
        this.issueID = issueId;
        this.projectId = projectId;
        this.projectCode = projectCode;

        $('.deleteHover .delete-chart').click(function () {
            var params = $(this).attr('id').split(',');

            gemini_popup.modalConfirm("Delete " + $(this).parent().find('.thumbnail .chart-name').text() + ' ?', null,
                    function () {
                        gemini_ajax.call('apps/lucidchart', 'deletedocument?projectcode=' + projectCode + '&projectId=' + projectId + '&issueid=' + issueId + '&documentid=' + params[1],
                          function (response) {
                              if (response.Success) {
                                  gemini_item.getAppControlValue(issueId, appId, controlId, 'value');
                              }
                          });
                    }
                );

         
        });
    }
};
//# sourceURL=LucidChart.js