# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

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
