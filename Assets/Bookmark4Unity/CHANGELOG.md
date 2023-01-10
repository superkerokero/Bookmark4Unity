# CHANGELOG

## v1.1.2 (2023-01-10)

### Features

- Added search providers to support searching bookmarks using quick search window.
  - Use `b4u:` for asset/scene object bookmarks.
  - Use `svb:` for scene view camera bookmarks.

## v1.1.1 (2022-12-26)

### Features

- Re-implemented bookmark window using UIToolkit.
  - Asset bookmarks and scene object bookmarks are now separate tabs.
  - Asset tab foldout status is remembered, while scene object foldout status updates itself according to currently loaded scenes in the hierarchy.
  - Added menu items for clearing bookmarks.
  - Assets and objects in the bookmark can now be **draged & droped** anywhere in the editor.

### Bugfix

- Fix an issue introduced with scene view camera bookmarks that causes unity editor to stall when closing.

## v1.1.0 (2022-12-22)

### Features

- Add scene view camera bookmarks.

### Bugfix

- Fix `PinSelected` not registering scene objects when invoked through keyboard shortcuts.
- Duplicated assets/scene objects are no longer registered.

## v1.0.2 (2022-09-01)

### Features

- Save collections to binary file.
- Load collections from binary file.

## v1.0.1 (2022-05-31)

### Features

- Remember the foldout status.

### Bugfix

- Fix runtime error when trying to build the project containing `Bookmark4Unity`.
- Fix the `GuidReference` editor custom drawer error.

## v1.0.0 (2022-05-19)

### Features

- Bookmark arbitrary objects in the scene.
- Bookmark arbitrary assets in the project.
