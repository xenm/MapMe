# Security Policy

## Supported Versions

The following versions of MapMe are currently supported with security updates:

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |

## Reporting a Vulnerability

We take the security of MapMe seriously. If you discover a security vulnerability, please follow these guidelines:

### Where to Report
- **Email**: [🔒 Report Security Vulnerability](mailto:adam.zaplatilek@gmail.com?subject=Security%20Vulnerability%20in%20MapMe%20-%20[SEVERITY%20LEVEL]&body=Hi%20Adam%2C%0D%0A%0D%0AI%20have%20discovered%20a%20security%20vulnerability%20in%20the%20MapMe%20application.%20Please%20find%20the%20details%20below%3A%0D%0A%0D%0A============================================%0D%0AVULNERABILITY%20DETAILS%0D%0A============================================%0D%0A%0D%0ADescription%3A%0D%0A%5BProvide%20a%20clear%20description%20of%20the%20vulnerability%5D%0D%0A%0D%0A%0D%0AAffected%20Component%3A%0D%0A%0D%0A%5B%20%5D%20Web%20Application%20%28Blazor%20WebAssembly%29%0D%0A%5B%20%5D%20Server%20%28ASP.NET%20Core%29%0D%0A%5B%20%5D%20Authentication%20System%0D%0A%5B%20%5D%20Database%20%28Cosmos%20DB%29%0D%0A%5B%20%5D%20API%20Endpoints%0D%0A%5B%20%5D%20Client-Side%20Security%0D%0A%5B%20%5D%20Other%3A%20%5BSpecify%5D%0D%0A%0D%0A%0D%0ASeverity%20Assessment%3A%0D%0A%0D%0A%5B%20%5D%20CRITICAL%20-%20Immediate%20risk%20to%20user%20data%20or%20system%20integrity%0D%0A%5B%20%5D%20HIGH%20-%20Significant%20security%20risk%0D%0A%5B%20%5D%20MEDIUM%20-%20Moderate%20security%20risk%0D%0A%5B%20%5D%20LOW%20-%20Minor%20security%20concern%0D%0A%0D%0A%0D%0A============================================%0D%0AREPRODUCTION%20STEPS%0D%0A============================================%0D%0A%0D%0A1.%20%5BStep%201%5D%0D%0A%0D%0A2.%20%5BStep%202%5D%0D%0A%0D%0A3.%20%5BStep%203%5D%0D%0A%0D%0A4.%20%5BObserved%20result%5D%0D%0A%0D%0A%0D%0A============================================%0D%0AIMPACT%20ASSESSMENT%0D%0A============================================%0D%0A%0D%0APotential%20Impact%3A%0D%0A%5BDescribe%20what%20could%20happen%20if%20this%20vulnerability%20is%20exploited%5D%0D%0A%0D%0A%0D%0AAffected%20Users%3A%0D%0A%5BEstimate%20how%20many%20users%20could%20be%20affected%5D%0D%0A%0D%0A%0D%0AData%20at%20Risk%3A%0D%0A%5BWhat%20type%20of%20data%20could%20be%20compromised%5D%0D%0A%0D%0A%0D%0A============================================%0D%0ATECHNICAL%20DETAILS%0D%0A============================================%0D%0A%0D%0AEnvironment%3A%0D%0A%0D%0A-%20MapMe%20Version%3A%20%5BVersion%20number%5D%0D%0A-%20Browser%3A%20%5BBrowser%20and%20version%5D%0D%0A-%20Operating%20System%3A%20%5BOS%20and%20version%5D%0D%0A%0D%0A%0D%0AProof%20of%20Concept%3A%0D%0A%5BInclude%20code%20snippets%2C%20screenshots%2C%20or%20detailed%20technical%20explanation%5D%0D%0A%0D%0A%0D%0A============================================%0D%0ASUGGESTED%20REMEDIATION%0D%0A============================================%0D%0A%0D%0AProposed%20Fix%3A%0D%0A%5BYour%20suggestions%20for%20fixing%20this%20vulnerability%5D%0D%0A%0D%0A%0D%0AReferences%3A%0D%0A%5BLinks%20to%20relevant%20security%20documentation%2C%20CVE%20entries%2C%20etc.%5D%0D%0A%0D%0A%0D%0A============================================%0D%0ADISCLOSURE%20PREFERENCES%0D%0A============================================%0D%0A%0D%0ACredit%20Preference%3A%0D%0A%0D%0A%5B%20%5D%20I%20would%20like%20to%20be%20credited%20in%20release%20notes%0D%0A%5B%20%5D%20I%20prefer%20to%20remain%20anonymous%0D%0A%0D%0A%0D%0AContact%20Information%3A%0D%0A%0D%0A-%20Name%3A%20%5BYour%20name%20or%20handle%5D%0D%0A-%20Email%3A%20%5BYour%20email%5D%0D%0A-%20Preferred%20Contact%20Method%3A%20%5BEmail%2FGitHub%2FEtc.%5D%0D%0A%0D%0A%0D%0AAdditional%20Notes%3A%0D%0A%5BAny%20additional%20information%20you%20think%20would%20be%20helpful%5D%0D%0A%0D%0A%0D%0A============================================%0D%0A%0D%0AThank%20you%20for%20helping%20to%20keep%20MapMe%20secure%21%0D%0A%0D%0ABest%20regards%2C%0D%0A%5BYour%20Name%5D) (preferred for sensitive issues)
- **GitHub**: [🛡️ GitHub Security Advisories](https://github.com/xenm/mapme/security/advisories) (for non-critical issues)

### What to Include
Please provide the following information in your security report:
- **Description**: Clear description of the vulnerability
- **Steps to Reproduce**: Detailed steps to reproduce the issue
- **Impact Assessment**: Potential impact and affected components
- **Proof of Concept**: Code snippets or screenshots (if applicable)
- **Suggested Fix**: If you have ideas for remediation

### Response Timeline
- **Initial Response**: Within 48 hours of report submission
- **Status Updates**: Weekly updates on investigation progress
- **Resolution Timeline**: 
  - Critical vulnerabilities: 7-14 days
  - High severity: 30 days
  - Medium/Low severity: 60-90 days

### What to Expect
**If the vulnerability is accepted:**
- We will acknowledge the issue and begin investigation
- You will receive regular updates on our progress
- We will coordinate with you on disclosure timing
- Credit will be given in release notes (unless you prefer anonymity)
- For significant findings, we may offer a bug bounty (case-by-case basis)

**If the vulnerability is declined:**
- We will provide a detailed explanation of why it was not accepted
- We may suggest alternative reporting channels if appropriate
- You are welcome to discuss the decision with our security team

### Security Best Practices
When reporting vulnerabilities, please:
- ✅ **DO** report responsibly and allow reasonable time for fixes
- ✅ **DO** provide clear reproduction steps and impact assessment
- ✅ **DO** respect user privacy and data protection
- ❌ **DON'T** publicly disclose vulnerabilities before they are patched
- ❌ **DON'T** access or modify user data without explicit permission
- ❌ **DON'T** perform testing that could impact service availability

### Scope
This security policy covers:
- **MapMe Web Application** (Blazor WebAssembly client and ASP.NET Core server)
- **Authentication System** (JWT, Google OAuth integration)
- **Database Security** (Cosmos DB data access and validation)
- **API Endpoints** (All REST API endpoints and data validation)
- **Client-Side Security** (XSS, CSRF, and other web vulnerabilities)

### Out of Scope
The following are generally considered out of scope:
- Social engineering attacks
- Physical security issues
- Denial of service attacks
- Issues in third-party dependencies (report to respective maintainers)
- Vulnerabilities requiring physical access to user devices

### Contact Information
- **Project Maintainer**: [🐙 xenm](https://github.com/xenm)
- **Security Team**: [💬 Adam Zaplatílek](mailto:adam.zaplatilek@gmail.com)

---

*This security policy is effective as of January 2025 and may be updated periodically to reflect changes in our security practices and procedures.*
