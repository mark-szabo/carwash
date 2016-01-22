'use strict';
angular.module('carwashApp')
.controller('reservationCtrl', ['$scope', '$location', 'calendarSvc', 'adalAuthenticationService', function ($scope, $location, calendarSvc, adalService) {
    $scope.error = "";
    $scope.reservationViewModel = null;
    $scope.dataLoaded = false;

    $scope.init = function () {
        $scope.getReservations();
    };

    $scope.getReservations = function () {
        calendarSvc.getReservations().then(function (response) {
            $scope.reservationViewModel = response.data;
            $scope.dataLoaded = true;
        }, function (response) {
            $scope.error = getErrorMessage(response);
            $scope.dataLoaded = true;
        });
    };

    $scope.deleteReservation = function (reservationId) {
        calendarSvc.deleteReservation(reservationId).then(function (response) {
            $scope.getReservations();
        }, function (response) {
            $scope.error = getErrorMessage(response);
        });
    }

}]);