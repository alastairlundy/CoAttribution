# CoAttribution Glossary

This document defines the domain language and core concepts of the project. It is intended to ensure consistency across the codebase and provide unambiguous definitions for agents and developers.

## Core Concepts

### Commit Trailer

A structured key-value suffix appended to Git commit messages after the body, separated by a blank line. The project manages two trailer types: `Co-authored-by` (equal contribution to the content) and `Assisted-by` (guidance, review, or supervision without writing the content). These follow the `git trailer` convention but the tool appends them directly; they are not natively handled by Git.

### Contributor Classification

Every contributor in the registry is classified as either a Human or an Agent. This classification is stored in the TOML author registry file under the `[agents]` and `[humans]` sections respectively. The classification affects behavior: agent-type contributors require per-host identity overrides to resolve name and email for specific git hosts, while human contributors do not.

### Attribution

The management of *attribution metadata* — the 'Who' and 'How' of contribution credit. Attribution answers which contributors are credited and in what role (co-author or assistant). The tool never generates commit message content, only appends attribution trailers.

### Default Attribution Type

Each contributor in the registry carries a default attribution role that determines which trailer type is used when the contributor is included without an explicit role override. This allows a contributor to be consistently credited as either a co-author or assistant unless explicitly specified otherwise per invocation.

### Host Resolution

The process of determining which git hosting platform (e.g. GitHub, GitLab, Bitbucket) a given repository targets, so that per-host identity overrides can be applied. Host resolution is needed because an agent may present different name and email credentials on different platforms.

### Attribution Resolution Priority

The order in which explicitly requested contributors are assigned their final attribution role. Contributors can be requested in different groups (explicit co-author, explicit assistant, or default-role), and the priority determines which group takes precedence when resolving each contributor's final trailer type.
