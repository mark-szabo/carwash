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
'use strict';
angular.module('carwashApp')
.controller('userDataCtrl', ['$scope', 'adalAuthenticationService', function ($scope, adalService) {


}]);
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