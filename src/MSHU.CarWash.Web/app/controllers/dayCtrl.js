'use strict';
angular.module('carwashApp')
.controller('dayCtrl', ['$scope', '$location', 'calendarSvc', 'adalAuthenticationService', function ($scope, $location, calendarSvc, adalService) {
    $scope.error = "";
    $scope.dayDetailsViewModel = null;
    $scope.dataLoaded = false;
    $scope.vehiclePlateNumberFormat = /[a-zA-Z]{3}-[0-9]{3}$/;
    $scope.kulsoBelsoKarpitDisabled = function () {
        return ($scope.dayDetailsViewModel != null && $scope.dayDetailsViewModel.AvailableSlots != null && $scope.dayDetailsViewModel.AvailableSlots < 2);
    }

    $scope.init = function () {
        $scope.getDay(calendarSvc.getValue('day'), calendarSvc.getValue('offset'));
    };

    $scope.getDay = function (day, offset) {
        calendarSvc.getDay(day, offset).then(function (response) {
            $scope.dayDetailsViewModel = response.data;
            $scope.dataLoaded = true;
        }, function (response) {
            $scope.error = getErrorMessage(response);
            $scope.dataLoaded = true;
        });
    };

    $scope.showCalendar = function (offset) {
        calendarSvc.setValue('offset', offset);
        $location.url('/Calendar');
    };

    $scope.saveReservation = function () {
        calendarSvc.saveReservation($scope.dayDetailsViewModel.NewReservation).then(function (response) {
            $scope.getDay(calendarSvc.getValue('day'), calendarSvc.getValue('offset'));
            $('#collapseExample').collapse('hide');
        }, function (response) {
            $scope.error = getErrorMessage(response);
        });
    }

    $scope.showReservation = function () {
        return ($scope.dayDetailsViewModel != null && $scope.dayDetailsViewModel.ReservationIsAllowed);
    }

    $scope.deleteReservation = function (reservationId) {
        calendarSvc.deleteReservation(reservationId).then(function (response) {
            $scope.getDay(calendarSvc.getValue('day'), calendarSvc.getValue('offset'));
        }, function (response) {
            $scope.error = getErrorMessage(response);
        });
    }

    $scope.autoCompleteEmployeeName = function () {
        $("#inputName").autocomplete({
            source: function (request, response) {

                $scope.dayDetailsViewModel.NewReservation.EmployeeId = "";
                $scope.dayDetailsViewModel.NewReservation.VehiclePlateNumber = "";

                var searchTerm = request.term;

                calendarSvc.getEmployees(searchTerm).then(function (resp) {
                    response($.map(resp.data, function (item) {
                        return { value: item.Name, id: item.Id, vehiclePlateNumber: item.VehiclePlateNumber }
                    }))
                }, function (resp) {
                    $scope.error = getErrorMessage(resp);
                });
            },
            minLength: 2,
            select: function (event, ui) {
                $scope.dayDetailsViewModel.NewReservation.EmployeeId = ui.item.id;
                $scope.dayDetailsViewModel.NewReservation.VehiclePlateNumber = ui.item.vehiclePlateNumber;
                return false;
            }
        });
    }

    //browser F5 esetén elvesznek a scope változók
    $scope.checkParamValues = function () {
        var offsetValue = calendarSvc.getValue('offset');
        if (offsetValue == null || offsetValue == 'undefined') {
            $location.url('/Home');
        }

        var dayValue = calendarSvc.getValue('day');
        if (dayValue == null || dayValue == 'undefined') {
            $location.url('/Home');
        }
    }

    $scope.checkParamValues();

}]);