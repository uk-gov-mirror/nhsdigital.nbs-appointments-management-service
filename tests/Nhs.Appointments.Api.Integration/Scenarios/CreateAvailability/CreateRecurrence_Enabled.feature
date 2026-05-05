Feature: Create recurring availability

  Scenario: Can create a recurrence
    Given the default site exists
    When I create the following recurring availability at the default site
      | StartDate     | EndDate              | ByDay                    | From     | Until     | SlotLength | Capacity | Services | Label      |
      | Tomorrow      | 45 days from today   | Monday,Wednesday,Friday  | 09:00    | 17:00     | 5          | 1        | COVID    | Jonny Test |
    Then the call should be successful
