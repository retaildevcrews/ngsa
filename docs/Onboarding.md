# Next Generation Symmetric Apps Onboarding

Welcome to the team! The goal of this document is to get you onboarded and ready to contribute as quickly as possible.

## Review

- [Working Agreement](./WorkingAgreement.md)
- [Bill of Materials](./BoM.md)
- [Application Architecture](./ApplicationArch.md)
- [Observability](./Observability.md)
- [Links, including dashboards](https://github.com/retaildevcrews/ngsa/discussions/545)

## Upskilling

- k8s upskilling videos - <https://aka.ms/rdc-k8s>
- demo videos - <https://aka.ms/rdc-demos>
- k8s quick start - <https://aka.ms/k8sqs>

## Overview of Repos

- [ngsa](https://github.com/retaildevcrews/ngsa)
  - This is our main dev repo
- [ngsa-app](https://github.com/retaildevcrews/ngsa-app)
  - This is the repo for the application based on `helium-csharp`
- [loderunner](https://github.com/retaildevcrews/loderunner)
  - This is the Load Service repo
- [akdc](https://github.com/microsoft/kubernetes-developer-cluster-kubeadm)
  - This script sets up a single-node Kubernetes development cluster on an Azure VM.
- [akdc-kind](https://github.com/retaildevcrews/akdc-kind)
  - This will setup a Kubernetes developer cluster using `Kind` and `GitHub Codespaces`

## Feature Crews

We use Feature Crews to break up themes and epics into pairable tasks. FCs foster more collaboration opportunities, allow for faster development, and shorter Dev Crew duration. Each FC should be cross-Dev Crew to promote collaboration.

FCs should manage their own process and may choose to run their own scrum. They are encouraged to pair program or [mob program](https://www.agilealliance.org/glossary/mob-programming). FCs will provide status updates on their progress/blockers at the daily standup.

You can find a discussion of the proposed Feature Crews here: <https://github.com/orgs/retaildevcrews/teams/nextgen-team/discussions/2>

## Board & Task management

Our main Project board is at <https://github.com/orgs/retaildevcrews/projects/5>. All tasks and user stories are currently tracked here. There is also a [Project Overview board](https://github.com/orgs/retaildevcrews/projects/5) that focuses on Themes & Epics. You will find a description of the board columns in the [Working Agreement](./WorkingAgreement.md#backlogs-and-dashboards).

Each Sprint will have a Sprint Tracking issue that will include all of the planned tasks for each Feature Crew. View [Sprint 2 tracking](https://github.com/retaildevcrews/ngsa/issues/501) for an example.

If you are working on a task, be sure to self-assign and move to the `In progress` column.

## Point Dev

We assign "Point Dev" status to a Feature Crew each Sprint. The FC will be responsible for everything in Point Dev Checklist. E.g. keeping a watch on running applications, deleting old tags, and triaging any security issues that may arise during the sprint. Point Devs are not soley responsible for fixing every issue, but they are responsible for ensuring they do not fall through the cracks.
