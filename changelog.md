# Changelog

## [Unreleased](https://github.com/devlooped/Merq/tree/HEAD)

[Full Changelog](https://github.com/devlooped/Merq/compare/v2.0.0-beta.3...HEAD)

**Implemented enhancements:**

- Preserve original exception stacktrace after recording telemetry error [\#79](https://github.com/devlooped/Merq/issues/79)

**Fixed bugs:**

- Don't allow mappers to map to same type [\#80](https://github.com/devlooped/Merq/issues/80)
- Set activity status to error when an exception is recorded [\#77](https://github.com/devlooped/Merq/issues/77)

**Merged pull requests:**

- ⬆️ Bump files with dotnet-file sync [\#75](https://github.com/devlooped/Merq/pull/75) ([devlooped-bot](https://github.com/devlooped-bot))

## [v2.0.0-beta.3](https://github.com/devlooped/Merq/tree/v2.0.0-beta.3) (2022-11-19)

[Full Changelog](https://github.com/devlooped/Merq/compare/v2.0.0-beta.2...v2.0.0-beta.3)

**Implemented enhancements:**

- Add exception telemetry recording [\#76](https://github.com/devlooped/Merq/issues/76)
- Add basic telemetry support in core message bus implementation  [\#73](https://github.com/devlooped/Merq/issues/73)
- Issue a warning for non-public commands [\#71](https://github.com/devlooped/Merq/issues/71)
- Set activity status to error when an exception is recorded [\#78](https://github.com/devlooped/Merq/pull/78) ([kzu](https://github.com/kzu))
- Add basic telemetry support in core message bus implementation [\#74](https://github.com/devlooped/Merq/pull/74) ([kzu](https://github.com/kzu))
- Issue a warning for non-public commands [\#72](https://github.com/devlooped/Merq/pull/72) ([kzu](https://github.com/kzu))

**Merged pull requests:**

- Bump NuGetizer from 0.9.0 to 0.9.1 [\#66](https://github.com/devlooped/Merq/pull/66) ([dependabot[bot]](https://github.com/apps/dependabot))

## [v2.0.0-beta.2](https://github.com/devlooped/Merq/tree/v2.0.0-beta.2) (2022-11-18)

[Full Changelog](https://github.com/devlooped/Merq/compare/v2.0.0-alpha...v2.0.0-beta.2)

**Implemented enhancements:**

- Fix support for non-void command handlers in duck typing [\#69](https://github.com/devlooped/Merq/issues/69)

**Merged pull requests:**

- ⬆️ Bump files with dotnet-file sync [\#63](https://github.com/devlooped/Merq/pull/63) ([devlooped-bot](https://github.com/devlooped-bot))

## [v2.0.0-alpha](https://github.com/devlooped/Merq/tree/v2.0.0-alpha) (2022-11-16)

[Full Changelog](https://github.com/devlooped/Merq/compare/v1.3.0...v2.0.0-alpha)

**Implemented enhancements:**

- Make duck-typing support pluggable with no built-in implementation [\#57](https://github.com/devlooped/Merq/issues/57)
- Add support for dynamic record creation including list-like properties [\#49](https://github.com/devlooped/Merq/issues/49)
- Support hierarchical record creation from generated dynamic factory   [\#47](https://github.com/devlooped/Merq/issues/47)
- Provide analyzer + code fix for turning sync command to async and viceversa [\#38](https://github.com/devlooped/Merq/issues/38)
- Provide analyzers + code fixes for common command authoring errors  [\#37](https://github.com/devlooped/Merq/issues/37)
- Add support for duck typing of commands [\#33](https://github.com/devlooped/Merq/issues/33)
- Add support for duck typing on events [\#31](https://github.com/devlooped/Merq/issues/31)
- Allow derived message bus implementation to monitor used event contracts [\#19](https://github.com/devlooped/Merq/issues/19)

**Closed issues:**

- Upgrade to centrally managed package versions [\#51](https://github.com/devlooped/Merq/issues/51)
- Add unit tests for analyzers [\#45](https://github.com/devlooped/Merq/issues/45)

**Merged pull requests:**

- Add pages and oss artifacts [\#62](https://github.com/devlooped/Merq/pull/62) ([kzu](https://github.com/kzu))
- Update to modern color [\#61](https://github.com/devlooped/Merq/pull/61) ([kzu](https://github.com/kzu))
- Make duck-typing support pluggable [\#60](https://github.com/devlooped/Merq/pull/60) ([kzu](https://github.com/kzu))
- ⬆️ Bump files with dotnet-file sync [\#59](https://github.com/devlooped/Merq/pull/59) ([devlooped-bot](https://github.com/devlooped-bot))
- Bump Devlooped.Extensions.DependencyInjection.Attributed from 1.1.3 to 1.2.0 [\#53](https://github.com/devlooped/Merq/pull/53) ([dependabot[bot]](https://github.com/apps/dependabot))
- Upgrade to centrally managed package versions [\#52](https://github.com/devlooped/Merq/pull/52) ([kzu](https://github.com/kzu))
- Add support for collections in dynamic record creation factories [\#50](https://github.com/devlooped/Merq/pull/50) ([kzu](https://github.com/kzu))
- Add support for hierarchical record creation from generated factories [\#48](https://github.com/devlooped/Merq/pull/48) ([kzu](https://github.com/kzu))
- Add comprehensive tests for analyzers and code fixes [\#46](https://github.com/devlooped/Merq/pull/46) ([kzu](https://github.com/kzu))
- Bump Microsoft.NET.Test.Sdk from 17.3.2 to 17.4.0 [\#42](https://github.com/devlooped/Merq/pull/42) ([dependabot[bot]](https://github.com/apps/dependabot))
- Bump Microsoft.Build.NoTargets from 3.5.6 to 3.6.0 [\#40](https://github.com/devlooped/Merq/pull/40) ([dependabot[bot]](https://github.com/apps/dependabot))
- Provide analyzer + code fix for turning sync command to async and viceversa [\#39](https://github.com/devlooped/Merq/pull/39) ([kzu](https://github.com/kzu))
- Bump Devlooped.Extensions.DependencyInjection.Attributed from 1.1.2 to 1.1.3 [\#36](https://github.com/devlooped/Merq/pull/36) ([dependabot[bot]](https://github.com/apps/dependabot))
- Support sync/async execute fixer on returning commands too [\#35](https://github.com/devlooped/Merq/pull/35) ([kzu](https://github.com/kzu))
- Duck typing of events and commands [\#34](https://github.com/devlooped/Merq/pull/34) ([kzu](https://github.com/kzu))
- Bump Devlooped.Extensions.DependencyInjection.Attributed from 1.1.2 to 1.1.3 [\#32](https://github.com/devlooped/Merq/pull/32) ([dependabot[bot]](https://github.com/apps/dependabot))
- ⬆️ Bump files with dotnet-file sync [\#28](https://github.com/devlooped/Merq/pull/28) ([devlooped-bot](https://github.com/devlooped-bot))
- ⬆️ Bump files with dotnet-file sync [\#27](https://github.com/devlooped/Merq/pull/27) ([devlooped-bot](https://github.com/devlooped-bot))
- ⬆️ Bump files with dotnet-file sync [\#26](https://github.com/devlooped/Merq/pull/26) ([devlooped-bot](https://github.com/devlooped-bot))
- Bump ThisAssembly.Project from 1.0.9 to 1.0.10 [\#25](https://github.com/devlooped/Merq/pull/25) ([dependabot[bot]](https://github.com/apps/dependabot))
- Bump Microsoft.Extensions.DependencyInjection from 6.0.0 to 6.0.1 [\#24](https://github.com/devlooped/Merq/pull/24) ([dependabot[bot]](https://github.com/apps/dependabot))
- ⬆️ Bump files with dotnet-file sync [\#23](https://github.com/devlooped/Merq/pull/23) ([devlooped-bot](https://github.com/devlooped-bot))
- Bump Microsoft.Build.NoTargets from 3.5.0 to 3.5.6 [\#22](https://github.com/devlooped/Merq/pull/22) ([dependabot[bot]](https://github.com/apps/dependabot))
- Bump RxFree from 1.1.1 to 1.1.2 [\#21](https://github.com/devlooped/Merq/pull/21) ([dependabot[bot]](https://github.com/apps/dependabot))
- Allow derived message bus implementation to monitor used event contracts [\#20](https://github.com/devlooped/Merq/pull/20) ([kzu](https://github.com/kzu))
- ⬆️ Bump files with dotnet-file sync [\#18](https://github.com/devlooped/Merq/pull/18) ([devlooped-bot](https://github.com/devlooped-bot))
- Bump Microsoft.NET.Test.Sdk from 17.3.1 to 17.3.2 [\#17](https://github.com/devlooped/Merq/pull/17) ([dependabot[bot]](https://github.com/apps/dependabot))
- Unified IMessageBus interface [\#16](https://github.com/devlooped/Merq/pull/16) ([kzu](https://github.com/kzu))
- Add dynamic OS matrix [\#15](https://github.com/devlooped/Merq/pull/15) ([kzu](https://github.com/kzu))
- Bump Microsoft.NET.Test.Sdk from 17.2.0 to 17.3.1 [\#14](https://github.com/devlooped/Merq/pull/14) ([dependabot[bot]](https://github.com/apps/dependabot))
- Bump NuGetizer from 0.8.0 to 0.9.0 [\#13](https://github.com/devlooped/Merq/pull/13) ([dependabot[bot]](https://github.com/apps/dependabot))
- Bump Microsoft.VisualStudio.SDK from 17.2.32505.173 to 17.3.32804.24 [\#12](https://github.com/devlooped/Merq/pull/12) ([dependabot[bot]](https://github.com/apps/dependabot))
- Bump Moq from 4.18.1 to 4.18.2 [\#10](https://github.com/devlooped/Merq/pull/10) ([dependabot[bot]](https://github.com/apps/dependabot))
- Bump xunit from 2.4.1 to 2.4.2 [\#9](https://github.com/devlooped/Merq/pull/9) ([dependabot[bot]](https://github.com/apps/dependabot))
- Bump files with dotnet-file sync [\#8](https://github.com/devlooped/Merq/pull/8) ([devlooped-bot](https://github.com/devlooped-bot))

## [v1.3.0](https://github.com/devlooped/Merq/tree/v1.3.0) (2022-07-28)

[Full Changelog](https://github.com/devlooped/Merq/compare/v1.5.0...v1.3.0)

## [v1.5.0](https://github.com/devlooped/Merq/tree/v1.5.0) (2022-07-28)

[Full Changelog](https://github.com/devlooped/Merq/compare/v1.2.0-beta...v1.5.0)

**Implemented enhancements:**

- Introduce IMessageBus unifying interface for Commands+Events [\#5](https://github.com/devlooped/Merq/issues/5)

**Merged pull requests:**

- Docs and packaging improvements [\#7](https://github.com/devlooped/Merq/pull/7) ([kzu](https://github.com/kzu))
- Introduce IMessageBus unifying interface for Commands+Events [\#6](https://github.com/devlooped/Merq/pull/6) ([kzu](https://github.com/kzu))

## [v1.2.0-beta](https://github.com/devlooped/Merq/tree/v1.2.0-beta) (2022-07-20)

[Full Changelog](https://github.com/devlooped/Merq/compare/v1.2.0-alpha...v1.2.0-beta)

**Merged pull requests:**

- Ensure we strong-name the assemblies with the previous key [\#4](https://github.com/devlooped/Merq/pull/4) ([kzu](https://github.com/kzu))
- Bump files with dotnet-file sync [\#3](https://github.com/devlooped/Merq/pull/3) ([kzu](https://github.com/kzu))

## [v1.2.0-alpha](https://github.com/devlooped/Merq/tree/v1.2.0-alpha) (2022-07-16)

[Full Changelog](https://github.com/devlooped/Merq/compare/9aed78c8a37c830093a8dbeb15981df3640dd350...v1.2.0-alpha)



\* *This Changelog was automatically generated by [github_changelog_generator](https://github.com/github-changelog-generator/github-changelog-generator)*
