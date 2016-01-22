'use strict';
angular.module('carwashApp')
.controller('homeCtrl', ['$scope', 'adalAuthenticationService', '$location', 'calendarSvc', function ($scope, adalService, $location, calendarSvc) {
    $scope.login = function () {
        adalService.login();
    };

    $scope.logout = function () {
        adalService.logOut();
    };

    $scope.isActive = function (viewLocation) {
        return viewLocation === $location.path();
    };

    $scope.init = function () {
        calendarSvc.setValue('offset', 0);
    };

    $scope.showCalendar = function (offset) {
        calendarSvc.setValue('offset', 0);
        $location.url('/Calendar');
    };
}]);
