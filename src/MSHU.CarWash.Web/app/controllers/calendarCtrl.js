'use strict';
angular.module('carwashApp')
.controller('calendarCtrl', ['$scope', '$location', 'calendarSvc', 'adalAuthenticationService', function ($scope, $location, calendarSvc, adalService) {
    $scope.error = "";
    $scope.currentUser = null;
    $scope.weekViewModel = null;
    $scope.dataLoaded = false;

    $scope.init = function () {
        $scope.getCurrentUser();
        $scope.getweek(calendarSvc.getValue('offset'));       
    };

    $scope.getCurrentUser = function () {
        var currentUser = calendarSvc.getValue('currentUser');
        if (currentUser == null) {
            calendarSvc.getCurrentUser().then(function (response) {
                $scope.currentUser = response.data;
                calendarSvc.setValue('currentUser', response.data);
            }, function (response) {
                $scope.error = getErrorMessage(response);
            })
        }
    };

    $scope.getweek = function (offset) {
        calendarSvc.getWeek(offset).then(function (response) {
            $scope.weekViewModel = response.data;
            $scope.dataLoaded = true;
        }, function (response) {
            $scope.error = getErrorMessage(response);
            $scope.dataLoaded = true;
        })
    };

    $scope.showDay = function (day, offset) {
        calendarSvc.setValue('day', day);
        calendarSvc.setValue('offset', offset);
        $location.url('/Day');
    };

    //browser F5 esetén elvesznek a scope változók
    $scope.checkParamValues = function () {
        var offsetValue = calendarSvc.getValue('offset');
        if (offsetValue == null || offsetValue == 'undefined') {
            $location.url('/Home');
        }
    }

    $scope.checkParamValues();    
}]);