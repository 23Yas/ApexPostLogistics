@scan-sync
Feature: Offline scan sync
  As a courier user
  I want to upload scans recorded offline
  So that shipment status reflects what happened in the field

  Background:
    Given the API is running with a fresh test database
    And I have an admin token
    And I have a courier token for "courier-bdd-1"
    And a shipment "APX-71000001" exists in status "OutForDelivery"

  Scenario: Valid scans are accepted and advance the shipment
    When I upload a "Delivered" scan for "APX-71000001" as courier "courier-bdd-1"
    Then the upload response accepted count is 1
    And the shipment "APX-71000001" status is "Delivered"

  Scenario: Re-uploading the same scan is skipped as a duplicate
    Given I have already uploaded scan "11111111-2222-3333-4444-555555555555" for "APX-71000001"
    When I upload the same scan "11111111-2222-3333-4444-555555555555" again
    Then the upload response duplicates skipped count is 1
    And the upload response accepted count is 0

  Scenario: A scan for a different tracking number is rejected
    When I upload a "Delivered" scan with tracking "APX-99999999" to shipment "APX-71000001" as courier "courier-bdd-1"
    Then the upload response rejected count is 1

  Scenario Outline: Terminal scans require OutForDelivery status
    Given a shipment "APX-71<seq>" exists in status "<status>"
    When I upload a "Delivered" scan for "APX-71<seq>" as courier "courier-bdd-1"
    Then the upload response <outcome> count is 1

    Examples:
      | seq   | status         | outcome   |
      | 000002 | Created        | rejected  |
      | 000003 | PickedUp       | rejected  |
      | 000004 | InTransit      | rejected  |
      | 000005 | OutForDelivery | accepted  |
