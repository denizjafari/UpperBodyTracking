# Phase 5 — Welcome & Preferences (Scenes 1 + 2)

## What landed

```
Assets/Scripts/UI/
└── UiBindings.cs                                       # OnClick / OnToggled / OnChanged extensions

Assets/Scripts/Scenes/Welcome/
└── WelcomeSceneController.cs                           # Scene 1 entry point

Assets/Scripts/Scenes/Preferences/
└── PreferencesSceneController.cs                       # Scene 2 entry point
```

## Welcome scene setup
1. `File → New Scene → Empty` → save as `Assets/Scenes/Welcome.unity`.
2. Add a single root GameObject `[Welcome]`. Attach `WelcomeSceneController`.
3. Build a world-space Canvas (NEVER screen-space in VR) at ~1.2 m in front of the
   `OVRCameraRig` head. Set canvas scale to 0.001.
4. Add inside the canvas:
   - `TMP_Dropdown` for **profile selector** → drag into `profileDropdown`.
   - `TMP_InputField` for **new profile name** → drag into `newProfileNameField`.
   - `TMP_Dropdown` with two options "English" / "Français" → drag into `newProfileLanguage`.
   - `Button` "Create" → `createButton`.
   - `Button` "Continue" → `continueButton`.
   - `TMP_Text` "Status" → `statusText`.
5. Add to Build Settings under `Core.unity`.

## Preferences scene setup
1. New scene `Assets/Scenes/UserPreferences.unity`.
2. Root GameObject `[UserPreferences]` with `PreferencesSceneController`.
3. World-space canvas with:
   - 4× `Toggle` (Text / Audio / Visual / Haptic) → drag into the four toggle slots.
   - `TMP_Dropdown` (en / fr) → `languageDropdown`.
   - `Button` "Re-run IMU bias" → `rerunBiasButton`.
   - `Button` "Re-run ROM" → `rerunRomButton`.
   - `Button` "Done" → `doneButton`.
   - `TMP_Text` status line → `statusText`.

## Routing rule (per design)
`WelcomeSceneController.OnContinueClicked()`:
- If user has both `imuBiasComplete == true` AND `IsRomCalibrationRequired() == false`
  → load `SessionSetup`.
- Otherwise → load `Calibration_A`.

## Verification gate 5
- [ ] Cold launch with no profiles: dropdown shows "(no profiles)", typing a name +
      Create produces a folder under `<persistentDataPath>/Users/<id>/` containing
      `profile.json`, `user_preferences.json`, `calibration.json`.
- [ ] Re-launch: previously created profile appears in the dropdown.
- [ ] Toggling a preference and exiting/re-entering the Preferences scene shows the
      new value persisted.
- [ ] Continue from Welcome with a fresh profile loads `Calibration_A`.
- [ ] `git tag phase-5-profile && git push --tags`.
