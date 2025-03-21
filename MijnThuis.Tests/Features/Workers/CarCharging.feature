Feature: Car Charging Worker

A worker that runs in the background continuesly and checks if a car is connected to the charger at home.
If a car is connected, the worker will start charging the car, but only if the home battery is charged enough,
and if there is enough solar energy available to charge the car.

@mytag
Scenario: The car is not connected to the charger
	Given The car charge port is not open
	When The worker runs a check
	Then The worker should have checked the car charge port
	And The car should not be ready to charge


Scenario: The car is not located at home
	Given The car is parked at "Work"
	When The worker runs a check
	Then The worker should have checked the car location
	And The car should not be ready to charge


Scenario: The car is connected to the charger, but the home battery is not charged enough
	Given The car is parked at "Thuis"
	And The car charge port is open
	And The home battery is charged to 95%
	When The worker runs a check
	Then The worker should have checked the car charge port
	And The worker should have checked the car location
	And The worker should have checked the home battery
	And The car should be ready to charge
	And The car should not be charging


Scenario: The car is connected to the charger, and the home battery is charged enough
	Given The car is parked at "Thuis"
	And The car charge port is open
	And The car has a maximum charging speed of 16A
	And The home battery is charged to 96%
	And the current solar power is 2.3W
	When The worker runs a check
	Then The worker should have checked the car charge port
	And The worker should have checked the car location
	And The worker should have checked the home battery
	And The car should be ready to charge
	And The car should have started charging at 10A