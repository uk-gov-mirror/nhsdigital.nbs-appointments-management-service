Feature: Create recurring availability

  Scenario: Can create a recurrence
    Given the default site exists
    When I create the following recurring availability at the default site
      | StartDate           | EndDate              | ByDay                    | From     | Until     | SlotLength | Capacity | Services | Label      |
      | Monday in 3 weeks   | Friday in 5 weeks    | Monday,Wednesday,Friday  | 09:00    | 17:00     | 5          | 1        | COVID    | Jonny Test |
    Then the call should be successful
    And the following latest created recurring availability exists at the default site
      | StartDate           | EndDate              | ByDay                    | From     | Until     | SlotLength | Capacity | Services | Label      |
      | Monday in 3 weeks   | Friday in 5 weeks    | Monday,Wednesday,Friday  | 09:00    | 17:00     | 5          | 1        | COVID    | Jonny Test |

  Scenario: Can create a weekend recurrence with multiple services
    Given the default site exists
    When I create the following recurring availability at the default site
      | StartDate           | EndDate              | ByDay             | From     | Until     | SlotLength | Capacity | Services      | Label         |
      | 2 days from today   | 60 days from today   | Saturday,Sunday   | 10:00    | 14:00     | 15         | 2        | FLU,COVID     | Weekend Shift |
    Then the call should be successful
    And the following latest created recurring availability exists at the default site
      | StartDate           | EndDate              | ByDay             | From     | Until     | SlotLength | Capacity | Services      | Label         |
      | 2 days from today   | 60 days from today   | Saturday,Sunday   | 10:00    | 14:00     | 15         | 2        | FLU,COVID     | Weekend Shift |

  Scenario: Can create duplicate recurring availabilities
    Given the default site exists
    When I create the following recurring availability at the default site
      | StartDate     | EndDate              | ByDay          | From     | Until     | SlotLength  | Capacity | Services | Label        |
      | Tomorrow      | 20 days from today   | Monday,Friday  | 09:00    | 17:00     | 10          | 1        | COVID    | Recurrence 1 |
    Then the call should be successful
    And the following latest created recurring availability exists at the default site
      | StartDate     | EndDate              | ByDay          | From     | Until     | SlotLength  | Capacity | Services | Label        |
      | Tomorrow      | 20 days from today   | Monday,Friday  | 09:00    | 17:00     | 10          | 1        | COVID    | Recurrence 1 |
    And I create the following recurring availability at the default site
      | StartDate     | EndDate              | ByDay          | From     | Until     | SlotLength  | Capacity | Services | Label        |
      | Tomorrow      | 20 days from today   | Monday,Friday  | 09:00    | 17:00     | 10          | 1        | COVID    | Recurrence 2 |
    Then the call should be successful
    And the following latest created recurring availability exists at the default site
      | StartDate     | EndDate              | ByDay          | From     | Until     | SlotLength  | Capacity | Services | Label        |
      | Tomorrow      | 20 days from today   | Monday,Friday  | 09:00    | 17:00     | 10          | 1        | COVID    | Recurrence 2 |

  Scenario: Can create similar recurring availabilities that don't merge together - capacity
    Given the default site exists
    When I create the following recurring availability at the default site
      | StartDate     | EndDate              | ByDay   | From     | Until     | SlotLength  | Capacity | Services | Label        |
      | Tomorrow      | 20 days from today   | Monday  | 09:00    | 17:00     | 10          | 1        | COVID    | Recurrence 1 |
    Then the call should be successful
    And the following latest created recurring availability exists at the default site
      | StartDate     | EndDate              | ByDay   | From     | Until     | SlotLength  | Capacity | Services | Label        |
      | Tomorrow      | 20 days from today   | Monday  | 09:00    | 17:00     | 10          | 1        | COVID    | Recurrence 1 |
    And I create the following recurring availability at the default site
      | StartDate     | EndDate              | ByDay   | From     | Until     | SlotLength  | Capacity | Services | Label        |
      | Tomorrow      | 20 days from today   | Monday  | 09:00    | 17:00     | 10          | 2        | COVID    | Recurrence 2 |
    Then the call should be successful
    And the following latest created recurring availability exists at the default site
      | StartDate     | EndDate              | ByDay   | From     | Until     | SlotLength  | Capacity | Services | Label        |
      | Tomorrow      | 20 days from today   | Monday  | 09:00    | 17:00     | 10          | 2        | COVID    | Recurrence 2 |

  Scenario: Can create similar recurring availabilities that don't merge together - different days
    Given the default site exists
    When I create the following recurring availability at the default site
      | StartDate     | EndDate              | ByDay    | From     | Until     | SlotLength | Capacity | Services | Label        |
      | Tomorrow      | 20 days from today   | Monday   | 09:00    | 17:00     | 10         | 1        | COVID    | Recurrence 1 |
    Then the call should be successful
    And the following latest created recurring availability exists at the default site
      | StartDate     | EndDate              | ByDay    | From     | Until     | SlotLength | Capacity | Services | Label        |
      | Tomorrow      | 20 days from today   | Monday   | 09:00    | 17:00     | 10         | 1        | COVID    | Recurrence 1 |
    And I create the following recurring availability at the default site
      | StartDate     | EndDate              | ByDay    | From     | Until     | SlotLength | Capacity | Services | Label        |
      | Tomorrow      | 20 days from today   | Tuesday  | 09:00    | 17:00     | 10         | 1        | COVID    | Recurrence 2 |
    Then the call should be successful
    And the following latest created recurring availability exists at the default site
      | StartDate     | EndDate              | ByDay    | From     | Until     | SlotLength | Capacity | Services | Label        |
      | Tomorrow      | 20 days from today   | Tuesday  | 09:00    | 17:00     | 10         | 1        | COVID    | Recurrence 2 |

  Scenario: Can create similar recurring availabilities that don't merge together - different services
    Given the default site exists
    When I create the following recurring availability at the default site
      | StartDate     | EndDate              | ByDay   | From     | Until     | SlotLength | Capacity | Services | Label        |
      | Tomorrow      | 20 days from today   | Monday  | 09:00    | 17:00     | 10         | 1        | COVID    | Recurrence 1 |
    Then the call should be successful
    And the following latest created recurring availability exists at the default site
      | StartDate     | EndDate              | ByDay   | From     | Until     | SlotLength | Capacity | Services | Label        |
      | Tomorrow      | 20 days from today   | Monday  | 09:00    | 17:00     | 10         | 1        | COVID    | Recurrence 1 |
    And I create the following recurring availability at the default site
      | StartDate     | EndDate              | ByDay   | From     | Until     | SlotLength | Capacity | Services | Label        |
      | Tomorrow      | 20 days from today   | Monday  | 09:00    | 17:00     | 10         | 1        | FLU      | Recurrence 2 |
    Then the call should be successful
    And the following latest created recurring availability exists at the default site
      | StartDate     | EndDate              | ByDay   | From     | Until     | SlotLength | Capacity | Services | Label        |
      | Tomorrow      | 20 days from today   | Monday  | 09:00    | 17:00     | 10         | 1        | FLU      | Recurrence 2 |

  Scenario: Can create similar recurring availabilities that don't merge together - sequential dates
    Given the default site exists
    When I create the following recurring availability at the default site
      | StartDate     | EndDate              | ByDay   | From     | Until     | SlotLength | Capacity | Services | Label        |
      | Tomorrow      | 20 days from today   | Monday  | 09:00    | 17:00     | 10         | 1        | COVID    | Recurrence 1 |
    Then the call should be successful
    And the following latest created recurring availability exists at the default site
      | StartDate     | EndDate              | ByDay   | From     | Until     | SlotLength | Capacity | Services | Label        |
      | Tomorrow      | 20 days from today   | Monday  | 09:00    | 17:00     | 10         | 1        | COVID    | Recurrence 1 |
    And I create the following recurring availability at the default site
      | StartDate           | EndDate              | ByDay   | From     | Until     | SlotLength | Capacity | Services | Label        |
      | 21 days from today  | 40 days from today   | Monday  | 09:00    | 17:00     | 10         | 1        | COVID    | Recurrence 2 |
    Then the call should be successful
    And the following latest created recurring availability exists at the default site
      | StartDate           | EndDate              | ByDay   | From     | Until     | SlotLength | Capacity | Services | Label        |
      | 21 days from today  | 40 days from today   | Monday  | 09:00    | 17:00     | 10         | 1        | COVID    | Recurrence 2 |

  Scenario: Can create similar recurring availabilities with different slot lengths
    Given the default site exists
    When I create the following recurring availability at the default site
      | StartDate | EndDate            | ByDay  | From  | Until | SlotLength | Capacity | Services | Label        |
      | Tomorrow  | 20 days from today | Monday | 09:00 | 17:00 | 10         | 1        | COVID    | Short Slots  |
    Then the call should be successful
    And the following latest created recurring availability exists at the default site
      | StartDate | EndDate            | ByDay  | From  | Until | SlotLength | Capacity | Services | Label        |
      | Tomorrow  | 20 days from today | Monday | 09:00 | 17:00 | 10         | 1        | COVID    | Short Slots  |
    And I create the following recurring availability at the default site
      | StartDate | EndDate            | ByDay  | From  | Until | SlotLength | Capacity | Services | Label        |
      | Tomorrow  | 20 days from today | Monday | 09:00 | 17:00 | 20         | 1        | COVID    | Long Slots   |
    Then the call should be successful
    And the following latest created recurring availability exists at the default site
      | StartDate | EndDate            | ByDay  | From  | Until | SlotLength | Capacity | Services | Label        |
      | Tomorrow  | 20 days from today | Monday | 09:00 | 17:00 | 20         | 1        | COVID    | Long Slots   |
    
  Scenario: Can create a recurrence spanning a leap day
    Given the default site exists
    When I create the following recurring availability at the default site
      | StartDate  | EndDate     | ByDay           | From  | Until | SlotLength | Capacity | Services | Label     |
      | 2028-02-01 | 2028-03-01  | Thursday,Friday | 09:00 | 17:00 | 10         | 1        | COVID    | Leap Test |
    Then the call should be successful
    And the following latest created recurring availability exists at the default site
      | StartDate  | EndDate     | ByDay           | From  | Until | SlotLength | Capacity | Services | Label     |
      | 2028-02-01 | 2028-03-01  | Thursday,Friday | 09:00 | 17:00 | 10         | 1        | COVID    | Leap Test |
