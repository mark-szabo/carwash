(function () {
    var validParameterNames = ['httpStatus', 'failureName', 'failureUrl'];

    function parseQueryParameters() {
        var query = location.search.slice(1);
        return query.split('&').reduce(function (queryParameters, rawPair) {
            var pair = rawPair.split('=').map(decodeURIComponent);
            queryParameters[pair[0]] = pair[1];
            return queryParameters;
        }, {});
    }

    function initialize() {
        var queryParameters = parseQueryParameters();
        var errorInfo = 'timestamp: ' + (new Date()).toJSON() + ' \n';
        errorInfo += 'online: ' + navigator.onLine + ' \n';
        validParameterNames.forEach(function (parameterName) {
            var parameterValue = queryParameters[parameterName] || 'N/A';
            errorInfo += parameterName + ': ' + parameterValue + ' \n';
        });

        document.getElementById('errorinfo').value = errorInfo;

        if (!navigator.onLine) document.getElementById('errormessage').textContent = 'You are offline.';
    }

    document.addEventListener('DOMContentLoaded', initialize);
}());
