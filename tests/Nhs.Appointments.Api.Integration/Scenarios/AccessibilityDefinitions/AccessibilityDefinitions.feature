Feature: Get Accessibility Definitions

  Scenario: Retrieve list of accessibility definitions
  Given There are existing system accessibilities
    | Id                                          | DisplayName                 |
    | accessibility/accessible_toilet             | Accessible toilet           |
    | accessibility/braille_translation_service   | Braille translation service |
    | accessibility/disabled_car_parking          | Disabled car parking        |
    | accessibility/car_parking                   | Car parking                 |
    | accessibility/induction_loop                | Induction loop              |
    | accessibility/sign_language_service         | Sign language service       |
    | accessibility/step_free_access              | Step free access            |
    | accessibility/text_relay                    | Text relay                  |
    | accessibility/wheelchair_access             | Wheelchair access           |
  When I query for all accessibility definitions
  Then the following accessibility definitions are returned
    | Id                                          | DisplayName                 |
    | accessibility/accessible_toilet             | Accessible toilet           |
    | accessibility/braille_translation_service   | Braille translation service |
    | accessibility/disabled_car_parking          | Disabled car parking        |
    | accessibility/car_parking                   | Car parking                 |
    | accessibility/induction_loop                | Induction loop              |
    | accessibility/sign_language_service         | Sign language service       |
    | accessibility/step_free_access              | Step free access            |
    | accessibility/text_relay                    | Text relay                  |
    | accessibility/wheelchair_access             | Wheelchair access           |
