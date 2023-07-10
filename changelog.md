# Changelog

## [v2.0.0-rc.2](https://github.com/devlooped/Merq/tree/v2.0.0-rc.2) (2023-07-10)

[Full Changelog](https://github.com/devlooped/Merq/compare/v2.0.0-rc.1...v2.0.0-rc.2)

:sparkles: Implemented enhancements:

- Observability: add caller information to main API for improved telemetry [\#102](https://github.com/devlooped/Merq/issues/102)
- Avoid losing caller information when invoking extension methods [\#104](https://github.com/devlooped/Merq/pull/104) (@kzu)
- Add caller information to main API for improved telemetry [\#103](https://github.com/devlooped/Merq/pull/103) (@kzu)

:twisted_rightwards_arrows: Merged:

- Add public api analyzers to avoid inadvertent breaking changes [\#105](https://github.com/devlooped/Merq/pull/105) (@kzu)

## [v2.0.0-rc.1](https://github.com/devlooped/Merq/tree/v2.0.0-rc.1) (2023-07-07)

[Full Changelog](https://github.com/devlooped/Merq/compare/v2.0.0-beta.4...v2.0.0-rc.1)

:sparkles: Implemented enhancements:

- Improve observability [\#99](https://github.com/devlooped/Merq/issues/99)
- Add command and event payloads to activity [\#101](https://github.com/devlooped/Merq/pull/101) (@kzu)
- Improve observability [\#100](https://github.com/devlooped/Merq/pull/100) (@kzu)

## [v2.0.0-beta.4](https://github.com/devlooped/Merq/tree/v2.0.0-beta.4) (2023-07-06)

[Full Changelog](https://github.com/devlooped/Merq/compare/v2.0.0-beta.3...v2.0.0-beta.4)

:sparkles: Implemented enhancements:

- Preserve original exception stacktrace after recording telemetry error [\#79](https://github.com/devlooped/Merq/issues/79)

:bug: Fixed bugs:

- Don't allow mappers to map to same type [\#80](https://github.com/devlooped/Merq/issues/80)
- Set activity status to error when an exception is recorded [\#77](https://github.com/devlooped/Merq/issues/77)

:twisted_rightwards_arrows: Merged:

- Rename operations to Execute and Notify [\#97](https://github.com/devlooped/Merq/pull/97) (@kzu)
- Bump dependencies, switch to flexible central package versions [\#96](https://github.com/devlooped/Merq/pull/96) (@kzu)

## [v2.0.0-beta.3](https://github.com/devlooped/Merq/tree/v2.0.0-beta.3) (2022-11-19)

[Full Changelog](https://github.com/devlooped/Merq/compare/v2.0.0-beta.2...v2.0.0-beta.3)

:sparkles: Implemented enhancements:

- Add exception telemetry recording [\#76](https://github.com/devlooped/Merq/issues/76)
- Add basic telemetry support in core message bus implementation  [\#73](https://github.com/devlooped/Merq/issues/73)
- Issue a warning for non-public commands [\#71](https://github.com/devlooped/Merq/issues/71)
- Set activity status to error when an exception is recorded [\#78](https://github.com/devlooped/Merq/pull/78) (@kzu)
- Add basic telemetry support in core message bus implementation [\#74](https://github.com/devlooped/Merq/pull/74) (@kzu)
- Issue a warning for non-public commands [\#72](https://github.com/devlooped/Merq/pull/72) (@kzu)

## [v2.0.0-beta.2](https://github.com/devlooped/Merq/tree/v2.0.0-beta.2) (2022-11-18)

[Full Changelog](https://github.com/devlooped/Merq/compare/v2.0.0-alpha...v2.0.0-beta.2)

:sparkles: Implemented enhancements:

- Fix support for non-void command handlers in duck typing [\#69](https://github.com/devlooped/Merq/issues/69)

## [v2.0.0-alpha](https://github.com/devlooped/Merq/tree/v2.0.0-alpha) (2022-11-16)

[Full Changelog](https://github.com/devlooped/Merq/compare/v1.3.0...v2.0.0-alpha)

:sparkles: Implemented enhancements:

- Make duck-typing support pluggable with no built-in implementation [\#57](https://github.com/devlooped/Merq/issues/57)
- Add support for dynamic record creation including list-like properties [\#49](https://github.com/devlooped/Merq/issues/49)
- Support hierarchical record creation from generated dynamic factory   [\#47](https://github.com/devlooped/Merq/issues/47)
- Provide analyzer + code fix for turning sync command to async and viceversa [\#38](https://github.com/devlooped/Merq/issues/38)
- Provide analyzers + code fixes for common command authoring errors  [\#37](https://github.com/devlooped/Merq/issues/37)
- Add support for duck typing of commands [\#33](https://github.com/devlooped/Merq/issues/33)
- Add support for duck typing on events [\#31](https://github.com/devlooped/Merq/issues/31)
- Allow derived message bus implementation to monitor used event contracts [\#19](https://github.com/devlooped/Merq/issues/19)

:hammer: Other:

- Upgrade to centrally managed package versions [\#51](https://github.com/devlooped/Merq/issues/51)
- Add unit tests for analyzers [\#45](https://github.com/devlooped/Merq/issues/45)

:twisted_rightwards_arrows: Merged:

- Add pages and oss artifacts [\#62](https://github.com/devlooped/Merq/pull/62) (@kzu)
- Update to modern color [\#61](https://github.com/devlooped/Merq/pull/61) (@kzu)
- Make duck-typing support pluggable [\#60](https://github.com/devlooped/Merq/pull/60) (@kzu)
- Upgrade to centrally managed package versions [\#52](https://github.com/devlooped/Merq/pull/52) (@kzu)
- Add support for collections in dynamic record creation factories [\#50](https://github.com/devlooped/Merq/pull/50) (@kzu)
- Add support for hierarchical record creation from generated factories [\#48](https://github.com/devlooped/Merq/pull/48) (@kzu)
- Add comprehensive tests for analyzers and code fixes [\#46](https://github.com/devlooped/Merq/pull/46) (@kzu)
- Provide analyzer + code fix for turning sync command to async and viceversa [\#39](https://github.com/devlooped/Merq/pull/39) (@kzu)
- Support sync/async execute fixer on returning commands too [\#35](https://github.com/devlooped/Merq/pull/35) (@kzu)
- Duck typing of events and commands [\#34](https://github.com/devlooped/Merq/pull/34) (@kzu)
- Allow derived message bus implementation to monitor used event contracts [\#20](https://github.com/devlooped/Merq/pull/20) (@kzu)
- Unified IMessageBus interface [\#16](https://github.com/devlooped/Merq/pull/16) (@kzu)
- Add dynamic OS matrix [\#15](https://github.com/devlooped/Merq/pull/15) (@kzu)

## [v1.3.0](https://github.com/devlooped/Merq/tree/v1.3.0) (2022-07-28)

[Full Changelog](https://github.com/devlooped/Merq/compare/v1.5.0...v1.3.0)

## [v1.5.0](https://github.com/devlooped/Merq/tree/v1.5.0) (2022-07-28)

[Full Changelog](https://github.com/devlooped/Merq/compare/v1.2.0-beta...v1.5.0)

:sparkles: Implemented enhancements:

- Introduce IMessageBus unifying interface for Commands+Events [\#5](https://github.com/devlooped/Merq/issues/5)

:twisted_rightwards_arrows: Merged:

- Docs and packaging improvements [\#7](https://github.com/devlooped/Merq/pull/7) (@kzu)
- Introduce IMessageBus unifying interface for Commands+Events [\#6](https://github.com/devlooped/Merq/pull/6) (@kzu)

## [v1.2.0-beta](https://github.com/devlooped/Merq/tree/v1.2.0-beta) (2022-07-20)

[Full Changelog](https://github.com/devlooped/Merq/compare/v1.2.0-alpha...v1.2.0-beta)

:twisted_rightwards_arrows: Merged:

- Ensure we strong-name the assemblies with the previous key [\#4](https://github.com/devlooped/Merq/pull/4) (@kzu)

## [v1.2.0-alpha](https://github.com/devlooped/Merq/tree/v1.2.0-alpha) (2022-07-16)

[Full Changelog](https://github.com/devlooped/Merq/compare/9aed78c8a37c830093a8dbeb15981df3640dd350...v1.2.0-alpha)



\* *This Changelog was automatically generated by [github_changelog_generator](https://github.com/github-changelog-generator/github-changelog-generator)*
