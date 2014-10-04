define(['./_module'], function (app) {

    'use strict';

    return app.controller('SignInCtrl', [
		'$scope', '$rootScope', '$state', '$location', 'AuthService', 'MessageService',
		function ($scope, $rootScope, $state, $location, authService, msg) {

			$scope.log = {
				username: '',
				password: '',
				server: $location.host() + ':' + $location.port()
			};
			$scope.signIn = function () {
				if ($scope.login.$invalid) {
					msg.warn('Please fix all validation errors');
					return;
				}

				authService.validate($scope.log.username, $scope.log.password, $scope.log.server)
				.success(function (info) {
					$rootScope.esVersion = info.esVersion || '0.0.0.0';
					authService.setCredentials($scope.log.username, $scope.log.password, $scope.log.server);
					redirectToPreviousState();
				})
				.error(function () {
					msg.warn('Server does not exists or wrong user data');
				});
			};


			function redirectToPreviousState () {
				if($rootScope.currentState) {
					$state.go($rootScope.currentState)
				} else {
					$state.go('dashboard.list');
				}
			}

			function checkCookie () {
				
				authService.existsAndValid()
				.then(function () {
					redirectToPreviousState();
				});
			}

			checkCookie();

		}
	]);


});