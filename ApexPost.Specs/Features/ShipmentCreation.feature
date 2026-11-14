@shipment-creation
Feature: Shipment creation
  As an admin user
  I want to create shipments through the API
  So that courier operations have valid shipments to work with

  Background:
    Given the API is running with a fresh test database
    And I have an admin token

  Scenario: Create a valid shipment
    When I create a shipment with tracking number "APX-70000001" and weight 2.5 kg
    Then the response status is 201
    And the response has a Location header
    And the response body tracking number is "APX-70000001"

  Scenario: Duplicate tracking number is rejected
    Given a shipment "APX-70000002" already exists
    When I create a shipment with tracking number "APX-70000002" and weight 1.0 kg
    Then the response status is 409
    And the response content type is "application/problem+json"

  Scenario Outline: Invalid shipment data is rejected with 400
    When I create a shipment with tracking number "<tracking>" and weight <weight> kg
    Then the response status is 400
    And the response content type is "application/problem+json"

    Examples:
      | tracking       | weight |
      | NOT-A-TRACKING | 1.0    |
      | APX-70000003   | 0      |
      | APX-70000004   | 71     |
