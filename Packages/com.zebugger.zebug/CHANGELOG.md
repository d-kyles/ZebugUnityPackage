# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/).

## [0.8.6] - 2024-04-22

### Improved
* Perf: Found an unused matrix mult in a hot path
* VCS: Ignore weird MacOS files

### Fixed
* Tracked down that crappy issue where changing channel prefs wouldn't be persisted!
* Color parameter not being passed through correctly

## [0.8.5] - 2023-12-08

### Added
 * New DrawPlus method, and new DrawBox(bounds) api
 * Jobbified line drawing to improve performance when drawing thousands of lines per frame


## [0.8.0] - 2023-07-06

### Breaking Changes
* Upgraded miniumum supported Unity version to ` Unity 2021.3.17f1 ` 

### Added
 * A `AssertOnce` method which won't spam the log on subsequent calls.
 * A `DrawRay` function as a convenience API for `DrawLine`
 * A work in progress automatic debug/dev GUI

### Fixed
 * a compile error on PC



## [0.7.0] - 2023-03-22

### Added
- `Zebug.DrawX` functions now optionally run in non-editor development builds!
- DrawX can use a channel defined width, 4 variants of how width changes with distance.
* `Adaptive` --- Default. Feels good, costs a bit more.
* `World` --- Costs the same as Adaptive, good depth cues. Disappears in the distance.
* `Pixels` --- Long distance lines may feel cluttered and odd. The way the width changes conflicts
               with expected depth cues, so doesn't always feel great       
* `SinglePixel` --- Cheap, feels like it disappears up close, hard to see on high DPI screens

### Fixed
* ShouldLog wasn't used internally, but returned the wrong value.
* Simple-Color Shader now discoverable in build.

## [0.6.5] - 2023-01-24

### Added
- Add a useful utility instance method for getting simpler ILogger interfaces from a ZebugChannel
- Potential fix for editor window throwing it's toys when you drop this package over an old version with the window open

## [0.6.4] - 2022-11-08

### Added
- Optionally add a prefix to all logs on iOS. Allows the dev to match various syntax highlighting formats to help readability of XCode's logs. 

## [0.6.3] - 2022-10-26

### Added
- New scroll view added to the main Zebug window.

## [0.6.2] - 2022-10-26

### Fixed
- `channelExpandedSet` could be null when new.

## [0.6.1] - 2022-10-26

### Fixed
- Tag number format was incorrectly stated in the readme.

## [0.6.0] - 2022-10-26

### Added
- Advanced options foldout:
  - New TestChannels hiding option, test channels hidden by default now.
  - Option for clearning old/missing channel data now under this foldout 

### Updated
- Refresh window button now hardcoded to be hidden. (It's for Dev only)
- Default resources location changed to Assets/Resources by default (was Assets/Ignore/Resources)
- Updated docs re:git-ignore of log channels preferences object. 

### Fixed
- Channel Expansion should now survive window reload

## [0.5.0] - 2022-10-26
### Added
- The Zebug channel enabled state is now read from a project asset (resource).
- New gizmo shapes
  - StarBurst
  - Transform Locator
- Disabled channels now look disabled in the Zebug Editor Window
- New component for adding a gizmo to a transform at runtime (Editor)
- Hint to Resharper _et al_ to syntax highlight the format string.
- Test Scene behaviours
- More ideas

### Removed
- Channel enabled state no longer stored in Editor PlayerPreferences
- UIElements. We're back to IMGUI for now.

### Fixed
- Line color drawing issues 

### Updated
- Updated CoreUtils
- Documentation
- Copyright year to 2022
- Open brace to new-line. Shrug.
- Update to Unity 2020.3.33f1

## [0.4.2] - 2021-01-21
### Removed
 - Duplicate license file

## [0.4.1] - 2021-01-21

### Added
 - A new `InitializeOnLoad` method will now refresh `ZebugEditorWindow`  

## [0.4.0] - 2021-01-21

### Added
 - Project can now be functionally used as a Unity Package

## [0.3.0] - 2020-01-19

### Updated
- EditorWindow: Functional UIElements based tree
