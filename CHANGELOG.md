# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased
### Changed

## 2.0.0-beta-002 - 2019-11-26
### Fixed
* Fix #13: Only consume body when http code doesn't cause immediate rejection (by @WalternativE)

## 2.0.0-beta-001 - 2019-11-07
### Changed
* Fix #9: Data is now optional for all http methods including **GET** (by @SCullman)
* Fix #11: Decoders/ encoders are not cached within Json.Fetch (by @SCullman)
* Fix #7: Better error reports with `FetchError` (by @SCullman)
* Fix #5: Response type can now be `unit` (by @SCullman)

### Added
* Fix #8: Option to pass additional headers (by @SCullman)

## 1.1.0 - 2019-09-03
### Added

* Use `!^` operator to be agnostic of union case rank like `U2`, `U3`, etc. (by @SCullman)

## 1.0.0 - 2019-04-16
### Added

* Add documentation for all the methods

### Changed
* Fix #1: Rename `init` parameter to `properties`


## 1.0.0-beta-001 - 2019-04-15
### Added

* Initial release
