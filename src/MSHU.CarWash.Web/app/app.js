'use strict';
angular.module('carwashApp', ['ngRoute', 'AdalAngular', 'ngMessages'])
.config(['$routeProvider', '$httpProvider', 'adalAuthenticationServiceProvider', function ($routeProvider, $httpProvider, adalProvider) {

    $routeProvider.when("/Home", {
        controller: "homeCtrl",
        templateUrl: "/app/views/home.html",
    }).when("/Calendar", {
        controller: "calendarCtrl",
        templateUrl: "/app/views/calendar.html",
        requireADLogin: true,
    }).when("/Reservation", {
        controller: "reservationCtrl",
        templateUrl: "/app/views/reservation.html",
        requireADLogin: true,
    }).when("/Day", {
        controller: "dayCtrl",
        templateUrl: "/app/views/day.html",
        requireADLogin: true,
    }).when("/UserData", {
        controller: "userDataCtrl",
        templateUrl: "/app/views/userData.html",
        requireADLogin: true,
    }).otherwise({ redirectTo: "/Home" });

    adalProvider.init(
        {
            tenant: 'microsoft.onmicrosoft.com',
            clientId: '5d63f09b-56e3-4be2-8a58-a9af7ceb7c27',
        },
        $httpProvider
    );

}]);

angular.module('carwashApp')
.directive('loading', ['$http', '$timeout', function ($http, $timeout) {
    return {
        restrict: 'A',
        link: function (scope, elm, attrs) {
            var showTimer;

            scope.isLoading = function () {
                return $http.pendingRequests.length > 0;
            };

            scope.$watch(scope.isLoading, function (v) {
                v ? showSpinner() : hideSpinner();
            });

            function showSpinner() {
                //If showing is already in progress just wait
                if (showTimer) return;

                //Set up a timeout based on our configured delay to show
                // the element (our spinner)
                showTimer = $timeout(function () {
                    elm.show();
                }, 300);
            }

            function hideSpinner() {
                //This is important. If the timer is in progress
                // we need to cancel it to ensure everything stays
                // in sync.
                if (showTimer) {
                    $timeout.cancel(showTimer);
                }

                showTimer = null;

                elm.hide();
            }
        }
    };
}]);

function getErrorMessage(response) {
    var ret = response.statusText;
    if (response.data != null && response.data.ModelState != null) {
        var modelState = response.data.ModelState;
        var errorsString = "";
        for (var key in modelState) {
            if (modelState.hasOwnProperty(key)) {
                errorsString = (errorsString == "" ? "" : errorsString + "<br/>") + modelState[key];
            }
        }
        ret = errorsString;
    }
    return ret;
}
