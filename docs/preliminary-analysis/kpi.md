
# KPI

## Version
- Version: 1.0.0
- Date: 2026-02-02

## Change History
| Date       | Version | Description                | Author       |
|------------|---------|----------------------------|--------------|
| 2026-02-02 | 1.0.0   | Initial KPI document       | TirsvadWeb   |

---

## Requirements
| ID     | Type                      | Requirement                                                                 | FURPS+ ID(s)         |
|--------|---------------------------|------------------------------------------------------------------------------|----------------------|
| KPI-R-001  | Functional requirement    | The system must allow users to create, edit, view, and delete projects.      | [FURPS-F-002]             |
| KPI-R-002  | Functional requirement    | The system must support categorization and tagging of projects.              | [FURPS-F-003]             |
| KPI-R-003  | Functional requirement    | The system must support user authentication and role-based access.           | [FURPS-F-001], [FURPS-F-007]    |
| KPI-R-004  | Non-functional requirement| The system must be user-friendly and accessible (WCAG 2.1 AA).               | [FURPS-U-001], [FURPS-U-003]    |
| KPI-R-005  | Non-functional requirement| The system must be reliable and available.                                   | [FURPS-R-001]             |
| KPI-R-006  | Non-functional requirement| CRUD operations must be performed within an acceptable time frame.           | [FURPS-P-001], [FURPS-P-004]    |
| KPI-R-007  | Business requirement      | The system must support integration with external services (e.g., GitHub).   | [FURPS-F-005], [FURPS-X-005]    |
| KPI-R-008  | Business requirement      | The system must support future expansion and new features.                   | [FURPS-S-005]             |
| KPI-R-009  | Business requirement      | The system must be maintainable and extensible.                              | [FURPS-S-001], [FURPS-S-005]    |
| KPI-R-010  | Functional requirement    | The system must allow users to create, edit, view, and delete user profiles. | [FURPS-F-011]             |

## Functional KPI measurements
| ID     | KPI                                 | Measurement method                | Target         | Frequency            | FURPS+ ID(s)         |
|--------|-------------------------------------|-----------------------------------|----------------|----------------------|----------------------|
| KPI-F-001  | Project CRUD operations             | Number of successful operations   | 100% success   | Monthly              | [FURPS-F-002], [FURPS-P-001], [FURPS-P-004] |
| KPI-F-002  | Project categorization/tagging      | % of projects with tags/categories| >= 90%         | Quarterly            | [FURPS-F-003]              |
| KPI-F-003  | User authentication success         | Login success rate                | >= 99%         | Monthly              | [FURPS-F-001], [FURPS-F-007]     |
| KPI-F-011  | User profile CRUD operations        | Number of successful operations   | 100% success   | Monthly              | [FURPS-F-011]              |

## Non-functional KPI measurements
| ID     | KPI                                 | Measurement method                | Target                 | Frequency            | FURPS+ ID(s)         |
|--------|-------------------------------------|-----------------------------------|------------------------|----------------------|----------------------|
| KPI-NF-004  | Usability                           | User survey (1-5 scale)           | avg >= 4               | Annual               | [FURPS-U-001], [FURPS-U-002]     |
| KPI-NF-005  | Accessibility                       | Accessibility audit               | WCAG 2.1 AA compliant  | Annual               | [FURPS-U-003]              |
| KPI-NF-006  | System reliability                  | Uptime monitoring                 | >= 99.5% uptime        | Monthly              | [FURPS-R-001]              |
| KPI-NF-007  | CRUD operation response time        | Average response time             | < 500 milliseconds     | Monthly              | [FURPS-P-001], [FURPS-P-004]     |

## Business KPI measurements
| ID     | KPI                                 | Measurement method                | Target                 | Frequency            | FURPS+ ID(s)         |
|--------|-------------------------------------|-----------------------------------|------------------------|----------------------|----------------------|
| KPI-B-008  | Integration with external services   | Number of integrations            | >= 2 integrations      | Annual               | [FURPS-F-005], [FURPS-X-005]     |
| KPI-B-009  | Support for future expansion        | Time to add new feature           | < 1 month              | Annual               | [FURPS-S-005]              |
| KPI-B-010  | Maintainability                     | Code review/technical debt score  | Score >= 8/10          | Quarterly            | [FURPS-S-001], [FURPS-S-005]     |

<!-- Links -->

<!-- FURPS+ ID Links -->
[FURPS-F-001]: ../preliminary-analysis/furps.md "FURPS + Functionality"
[FURPS-F-002]: ../preliminary-analysis/furps.md "FURPS + Functionality"
[FURPS-F-003]: ../preliminary-analysis/furps.md "FURPS + Functionality"
[FURPS-F-004]: ../preliminary-analysis/furps.md "FURPS + Functionality"
[FURPS-F-005]: ../preliminary-analysis/furps.md "FURPS + Functionality"
[FURPS-F-006]: ../preliminary-analysis/furps.md "FURPS + Functionality"
[FURPS-F-007]: ../preliminary-analysis/furps.md "FURPS + Functionality"
[FURPS-F-008]: ../preliminary-analysis/furps.md "FURPS + Functionality"
[FURPS-F-009]: ../preliminary-analysis/furps.md "FURPS + Functionality"
[FURPS-F-010]: ../preliminary-analysis/furps.md "FURPS + Functionality"
[FURPS-F-011]: ../preliminary-analysis/furps.md "FURPS + Functionality"
[FURPS-U-001]: ../preliminary-analysis/furps.md "FURPS + Usability"
[FURPS-U-002]: ../preliminary-analysis/furps.md "FURPS + Usability"
[FURPS-U-003]: ../preliminary-analysis/furps.md "FURPS + Usability"
[FURPS-U-004]: ../preliminary-analysis/furps.md "FURPS + Usability"
[FURPS-U-005]: ../preliminary-analysis/furps.md "FURPS + Usability"
[FURPS-R-001]: ../preliminary-analysis/furps.md "FURPS + Reliability"
[FURPS-R-002]: ../preliminary-analysis/furps.md "FURPS + Reliability"
[FURPS-R-003]: ../preliminary-analysis/furps.md "FURPS + Reliability"
[FURPS-R-004]: ../preliminary-analysis/furps.md "FURPS + Reliability"
[FURPS-R-005]: ../preliminary-analysis/furps.md "FURPS + Reliability"
[FURPS-P-001]: ../preliminary-analysis/furps.md "FURPS + Performance"
[FURPS-P-002]: ../preliminary-analysis/furps.md "FURPS + Performance"
[FURPS-P-003]: ../preliminary-analysis/furps.md "FURPS + Performance"
[FURPS-P-004]: ../preliminary-analysis/furps.md "FURPS + Performance"
[FURPS-S-001]: ../preliminary-analysis/furps.md "FURPS + Supportability"
[FURPS-S-002]: ../preliminary-analysis/furps.md "FURPS + Supportability"
[FURPS-S-003]: ../preliminary-analysis/furps.md "FURPS + Supportability"
[FURPS-S-004]: ../preliminary-analysis/furps.md "FURPS + Supportability"
[FURPS-S-005]: ../preliminary-analysis/furps.md "FURPS + Supportability"
[FURPS-X-001]: ../preliminary-analysis/furps.md "FURPS + Extensibility"
[FURPS-X-002]: ../preliminary-analysis/furps.md "FURPS + Extensibility"
[FURPS-X-003]: ../preliminary-analysis/furps.md "FURPS + Extensibility"
[FURPS-X-004]: ../preliminary-analysis/furps.md "FURPS + Extensibility"
[FURPS-X-005]: ../preliminary-analysis/furps.md "FURPS + Extensibility"
