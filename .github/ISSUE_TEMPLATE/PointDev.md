---
name: Point dev checklist
about: 'Used by the point dev as a checklist for responsibilties during a sprint.'
title: 'Point dev checklist'
labels: 'Process'
assignees: ''

---

Sprint checklist for point dev.

- [ ] Helium <https://aka.ms/heliumdashboard>
  - look for anything abnormal. normal looks like
    - green "Availability tests summary"
    - relatively straight lines for requests and response times
    - 0 failed requests
- [ ] Helium Smokers [Portal](https://ms.portal.azure.com/#@microsoft.onmicrosoft.com/resource/subscriptions/fc127bd9-b9bd-4a86-a502-6e1d554bed0a/resourceGroups/helium-e2e-smoker-rg/overview)
  - Check that ACI containers are running
  - Check container events
  - Check logs
  - look into making progress on a smokers dashboard for helium
- [ ] Check GitHub actions
  - check scheduled actions are running, and look for deprecation warnings
    - <https://github.com/retaildevcrews/ngsa/actions>
    - <https://github.com/retaildevcrews/helium/actions>
    - <https://github.com/retaildevcrews/helium-csharp/actions>
    - <https://github.com/retaildevcrews/helium-java/actions>
    - <https://github.com/retaildevcrews/helium-typescript/actions>
    - <https://github.com/retaildevcrews/helium-terraform/actions>
- [ ] Run credscan on repos that don't have it automated with GitHub actions
  - aka.ms/credscan
- [ ] Check repos for issues that are not in a project board.
- [ ] Check Helium triage.
  - call out if triage list is getting long
  - call out items you feel are important
- [ ] [Dependency Warnings](https://github.com/orgs/retaildevcrews/insights/dependencies?query=is:vulnerable+sort:vulnerabilities-desc)
  - check for warnings in Helium and NGSA repos
- [ ] Delete old ghcr.io tags
