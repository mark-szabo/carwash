'use strict';
angular.module('carwashApp')
.factory('calendarSvc', ['$http', function ($http) {

    var hashtable = {};

    return {
        getCurrentUser: function () {
            return $http({
                method: 'GET',
                url: '/api/Employees/GetCurrentUser'
            });
        },

        getEmployees: function (searchTerm) {
            return $http({
                method: 'GET',
                url: '/api/Employees/GetEmployees?searchTerm=' + searchTerm
            });
        },

        getWeek: function (offset) {
            return $http({
                method: 'GET',
                url: '/api/Calendar/GetWeek?offSet=' + offset
            });
        },

        getDay: function (day, offset) {
            return $http({
                method: 'GET',
                url: '/api/Calendar/GetDay?day=' + day +'&offset=' + offset
            });
        },

        saveReservation: function (newReservationViewModel) {
            return $http({
                method: 'POST',
                url: '/api/Calendar/SaveReservation',
                data: newReservationViewModel
            });
        },

        deleteReservation: function (reservationId) {
            return $http({
                method: 'GET',
                url: '/api/Calendar/DeleteReservation?reservationId=' + reservationId,
            });
        },

        getReservations: function () {
            return $http({
                method: 'GET',
                url: '/api/Calendar/GetReservations'
            });
        },

        setValue: function (key, value) {
            hashtable[key] = value;
        },

        getValue: function (key) {
            return hashtable[key];
        }

    };
}]);