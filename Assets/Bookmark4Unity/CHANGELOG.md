# CHANGELOG

## v1.1.5 (2025-03-07)

### Features

- Update project version to 6000.0.38f1

### Bugfix

- Fix an issue where the scene view camera bookmarks preview capture not working in newer unity versions.

## v1.1.4 (2023-01-19)

### Features

- Added window menu items to access log/folders directly:
  - Console log
  - Folders
    - Data (`Application.dataPath`)
    - Persistent data (`Application.persistentDataPath`)
    - Streaming assets (`Application.streamingAssetsPath`)
    - Temporary cache (`Application.temporaryCachePath`)

## v1.1.3 (2023-01-15)

### Features

- Added scene object collection bookmarking.
  - When pinning multiple scene objects, you will be asked to either create a collection of scene objects or add scene object bookmarks individually.
- Guid components will not be removed automatically when un-pinning scene object bookmarks to avoid making bookmark collections invalid.
  - If you want to remove all guid components in loaded scenes, use `Bookmark4Unity Window → Clear → All Guid Components Attached`. Note that removing guid components will make scene object bookmarks invalid.

### Bugfix

- Fix a typo in scene objects editor dialogue.

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
