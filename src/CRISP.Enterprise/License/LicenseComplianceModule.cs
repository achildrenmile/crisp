using CRISP.Adr;
using Microsoft.Extensions.Logging;

namespace CRISP.Enterprise.License;

/// <summary>
/// Adds the correct license file, contributing guidelines, and optionally
/// license headers to source files.
/// </summary>
public sealed class LicenseComplianceModule : IEnterpriseModule
{
    private readonly ILogger<LicenseComplianceModule> _logger;

    public LicenseComplianceModule(ILogger<LicenseComplianceModule> logger)
    {
        _logger = logger;
    }

    public string Id => "license-compliance";
    public string DisplayName => "License & Compliance";
    public int Order => 300;

    public bool ShouldRun(ProjectContext context) => true;

    public async Task<ModuleResult> ExecuteAsync(ProjectContext context, CancellationToken cancellationToken = default)
    {
        var filesCreated = new List<string>();
        var filesModified = new List<string>();

        try
        {
            // Generate LICENSE file
            var licensePath = Path.Combine(context.WorkspacePath, "LICENSE");
            var licenseContent = GetLicenseText(context.LicenseSpdx, context.OrganizationName);
            await File.WriteAllTextAsync(licensePath, licenseContent, cancellationToken);
            filesCreated.Add("LICENSE");

            // Generate CONTRIBUTING.md
            var contributingPath = Path.Combine(context.WorkspacePath, "CONTRIBUTING.md");
            var contributingContent = GenerateContributingMd(context);
            await File.WriteAllTextAsync(contributingPath, contributingContent, cancellationToken);
            filesCreated.Add("CONTRIBUTING.md");

            // Add license headers if configured
            if (context.AddLicenseHeaders)
            {
                var modifiedFiles = await AddLicenseHeadersAsync(context, cancellationToken);
                filesModified.AddRange(modifiedFiles);
            }

            // Record ADR
            var licenseName = GetLicenseName(context.LicenseSpdx);
            context.DecisionCollector.Record(
                title: $"License project under {context.LicenseSpdx}",
                context: "Every project needs a clear license to define terms of use, modification, and distribution.",
                decision: $"Use {licenseName} license with CONTRIBUTING.md guidelines{(context.AddLicenseHeaders ? " and license headers in source files" : "")}.",
                rationale: GetLicenseRationale(context.LicenseSpdx),
                category: AdrCategory.Compliance,
                alternatives: new Dictionary<string, string>
                {
                    ["MIT"] = context.LicenseSpdx == "MIT" ? "Selected for simplicity and permissiveness" : "Permissive but requires attribution",
                    ["Apache-2.0"] = context.LicenseSpdx == "Apache-2.0" ? "Selected for patent protection" : "Provides patent grant but more complex",
                    ["UNLICENSED"] = context.LicenseSpdx == "UNLICENSED" ? "Selected for proprietary distribution" : "Would restrict all use and modification"
                },
                consequences: [
                    $"Code is distributed under {licenseName} terms",
                    "Contributors must agree to license terms",
                    "CONTRIBUTING.md provides clear contribution guidelines"
                ],
                relatedFiles: filesCreated
            );

            return new ModuleResult
            {
                ModuleId = Id,
                Success = true,
                FilesCreated = filesCreated,
                FilesModified = filesModified
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "License compliance module failed");
            return ModuleResult.Failed(Id, ex.Message);
        }
    }

    private static string GetLicenseText(string spdx, string? organizationName)
    {
        var year = DateTime.UtcNow.Year;
        var org = organizationName ?? "[ORGANIZATION]";

        return spdx.ToUpperInvariant() switch
        {
            "MIT" => $"""
                MIT License

                Copyright (c) {year} {org}

                Permission is hereby granted, free of charge, to any person obtaining a copy
                of this software and associated documentation files (the "Software"), to deal
                in the Software without restriction, including without limitation the rights
                to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
                copies of the Software, and to permit persons to whom the Software is
                furnished to do so, subject to the following conditions:

                The above copyright notice and this permission notice shall be included in all
                copies or substantial portions of the Software.

                THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
                IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
                FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
                AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
                LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
                OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
                SOFTWARE.
                """,

            "APACHE-2.0" => $"""
                                            Apache License
                                      Version 2.0, January 2004
                                   http://www.apache.org/licenses/

                Copyright {year} {org}

                Licensed under the Apache License, Version 2.0 (the "License");
                you may not use this file except in compliance with the License.
                You may obtain a copy of the License at

                    http://www.apache.org/licenses/LICENSE-2.0

                Unless required by applicable law or agreed to in writing, software
                distributed under the License is distributed on an "AS IS" BASIS,
                WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
                See the License for the specific language governing permissions and
                limitations under the License.
                """,

            "BSD-2-CLAUSE" => $"""
                BSD 2-Clause License

                Copyright (c) {year}, {org}

                Redistribution and use in source and binary forms, with or without
                modification, are permitted provided that the following conditions are met:

                1. Redistributions of source code must retain the above copyright notice, this
                   list of conditions and the following disclaimer.

                2. Redistributions in binary form must reproduce the above copyright notice,
                   this list of conditions and the following disclaimer in the documentation
                   and/or other materials provided with the distribution.

                THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
                AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
                IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
                DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
                FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
                DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
                SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
                CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
                OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
                OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
                """,

            "BSD-3-CLAUSE" => $"""
                BSD 3-Clause License

                Copyright (c) {year}, {org}

                Redistribution and use in source and binary forms, with or without
                modification, are permitted provided that the following conditions are met:

                1. Redistributions of source code must retain the above copyright notice, this
                   list of conditions and the following disclaimer.

                2. Redistributions in binary form must reproduce the above copyright notice,
                   this list of conditions and the following disclaimer in the documentation
                   and/or other materials provided with the distribution.

                3. Neither the name of the copyright holder nor the names of its
                   contributors may be used to endorse or promote products derived from
                   this software without specific prior written permission.

                THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
                AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
                IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
                DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
                FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
                DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
                SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
                CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
                OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
                OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
                """,

            "GPL-3.0-ONLY" => $"""
                GNU GENERAL PUBLIC LICENSE
                Version 3, 29 June 2007

                Copyright (c) {year} {org}

                This program is free software: you can redistribute it and/or modify
                it under the terms of the GNU General Public License as published by
                the Free Software Foundation, version 3 of the License.

                This program is distributed in the hope that it will be useful,
                but WITHOUT ANY WARRANTY; without even the implied warranty of
                MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
                GNU General Public License for more details.

                You should have received a copy of the GNU General Public License
                along with this program. If not, see <https://www.gnu.org/licenses/>.
                """,

            "ISC" => $"""
                ISC License

                Copyright (c) {year}, {org}

                Permission to use, copy, modify, and/or distribute this software for any
                purpose with or without fee is hereby granted, provided that the above
                copyright notice and this permission notice appear in all copies.

                THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
                WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
                MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
                ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
                WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
                ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
                OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
                """,

            _ => $"""
                Copyright (c) {year} {org}. All rights reserved.

                This software is proprietary and confidential. Unauthorized copying, distribution,
                or use of this software, via any medium, is strictly prohibited.

                For licensing inquiries, contact: [INSERT CONTACT EMAIL]
                """
        };
    }

    private static string GetLicenseName(string spdx) => spdx.ToUpperInvariant() switch
    {
        "MIT" => "MIT",
        "APACHE-2.0" => "Apache 2.0",
        "BSD-2-CLAUSE" => "BSD 2-Clause",
        "BSD-3-CLAUSE" => "BSD 3-Clause",
        "GPL-3.0-ONLY" => "GPL 3.0",
        "LGPL-3.0-ONLY" => "LGPL 3.0",
        "MPL-2.0" => "MPL 2.0",
        "ISC" => "ISC",
        _ => "Proprietary"
    };

    private static string GetLicenseRationale(string spdx) => spdx.ToUpperInvariant() switch
    {
        "MIT" => "MIT is simple, permissive, and widely understood. It allows commercial use while requiring attribution.",
        "APACHE-2.0" => "Apache 2.0 provides patent protection and is suitable for enterprise use. It's compatible with GPL 3.0.",
        "BSD-2-CLAUSE" or "BSD-3-CLAUSE" => "BSD licenses are permissive with minimal restrictions, suitable for libraries and frameworks.",
        "GPL-3.0-ONLY" => "GPL 3.0 ensures derivative works remain open source, protecting the community.",
        "ISC" => "ISC is functionally equivalent to MIT but with simpler language.",
        _ => "Proprietary license protects intellectual property and restricts unauthorized distribution."
    };

    private static string GenerateContributingMd(ProjectContext context)
    {
        var installCommand = GetInstallCommand(context);
        var testCommand = GetTestCommand(context);
        var linterDescription = GetLinterDescription(context);
        var licenseName = GetLicenseName(context.LicenseSpdx);

        return $"""
            # Contributing to {context.ProjectName}

            Thank you for your interest in contributing! This document provides guidelines
            for contributing to this project.

            ## Getting Started

            1. Clone the repository
               ```bash
               git clone {context.RepositoryUrl}
               cd {context.ProjectName}
               ```

            2. Install dependencies
               ```bash
               {installCommand}
               ```

            3. Run tests
               ```bash
               {testCommand}
               ```

            ## Development Workflow

            1. Create a feature branch from `{context.DefaultBranch}`
               ```bash
               git checkout -b feature/your-feature-name
               ```

            2. Make your changes

            3. Ensure all tests pass
               ```bash
               {testCommand}
               ```

            4. Submit a pull request

            ## Code Style

            {linterDescription}

            ## Commit Messages

            Use clear, descriptive commit messages. We recommend the
            [Conventional Commits](https://www.conventionalcommits.org/) format:

            ```
            feat: add user authentication endpoint
            fix: resolve null reference in order processing
            docs: update API documentation
            test: add integration tests for payment flow
            refactor: simplify error handling logic
            ```

            ## Pull Request Process

            1. Update documentation if your changes affect public APIs
            2. Add tests for new functionality
            3. Ensure CI passes (all checks green)
            4. Request review from a code owner
            5. Address review feedback promptly

            ## Code of Conduct

            - Be respectful and inclusive
            - Focus on constructive feedback
            - Help others learn and grow

            ## License

            By contributing to this project, you agree that your contributions will be
            licensed under the project's {licenseName} license. See [LICENSE](LICENSE) for details.

            ---

            *This contributing guide was generated by [CRISP](https://github.com/strali/crisp).*
            """;
    }

    private static string GetInstallCommand(ProjectContext context) => context.Language.ToLowerInvariant() switch
    {
        "csharp" => "dotnet restore",
        "python" => "pip install -r requirements.txt",
        "typescript" or "javascript" => "npm install",
        "java" => "mvn install -DskipTests",
        "dart" => "dart pub get",
        _ => "# Install dependencies according to project documentation"
    };

    private static string GetTestCommand(ProjectContext context) => context.Language.ToLowerInvariant() switch
    {
        "csharp" => "dotnet test",
        "python" => "pytest",
        "typescript" or "javascript" => "npm test",
        "java" => "mvn test",
        "dart" => "dart test",
        _ => "# Run tests according to project documentation"
    };

    private static string GetLinterDescription(ProjectContext context)
    {
        var linter = context.Linter?.ToLowerInvariant();

        return linter switch
        {
            "roslyn" => """
                This project uses Roslyn analyzers for code quality. Warnings are treated as errors
                in CI. Run `dotnet build` to see any issues.
                """,
            "ruff" => """
                This project uses Ruff for linting and formatting. Run before committing:
                ```bash
                ruff check .
                ruff format .
                ```
                """,
            "eslint" => """
                This project uses ESLint for linting. Run before committing:
                ```bash
                npm run lint
                npm run lint:fix  # to auto-fix issues
                ```
                """,
            "checkstyle" => """
                This project uses Checkstyle for code style enforcement. Run:
                ```bash
                mvn checkstyle:check
                ```
                """,
            _ => """
                Please follow the existing code style in the project. Consistency is key!
                """
        };
    }

    private async Task<List<string>> AddLicenseHeadersAsync(ProjectContext context, CancellationToken cancellationToken)
    {
        var modifiedFiles = new List<string>();
        var header = GetLicenseHeader(context);

        var extensions = GetSourceFileExtensions(context.Language);
        var sourceFiles = Directory.GetFiles(context.WorkspacePath, "*.*", SearchOption.AllDirectories)
            .Where(f => extensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            .Where(f => !f.Contains("obj") && !f.Contains("bin") && !f.Contains("node_modules"));

        foreach (var file in sourceFiles)
        {
            var content = await File.ReadAllTextAsync(file, cancellationToken);
            if (!content.StartsWith('/') && !content.StartsWith('#'))
            {
                var headerForFile = GetHeaderForExtension(header, Path.GetExtension(file));
                await File.WriteAllTextAsync(file, headerForFile + "\n\n" + content, cancellationToken);
                modifiedFiles.Add(Path.GetRelativePath(context.WorkspacePath, file));
            }
        }

        return modifiedFiles;
    }

    private static string GetLicenseHeader(ProjectContext context)
    {
        var year = DateTime.UtcNow.Year;
        var org = context.OrganizationName ?? "[ORGANIZATION]";
        return $"Copyright (c) {year} {org}. Licensed under the {context.LicenseSpdx}. See LICENSE file.";
    }

    private static string GetHeaderForExtension(string header, string extension) => extension.ToLowerInvariant() switch
    {
        ".cs" or ".java" or ".ts" or ".js" or ".dart" => $"// {header}",
        ".py" => $"# {header}",
        ".css" or ".scss" => $"/* {header} */",
        _ => $"// {header}"
    };

    private static string[] GetSourceFileExtensions(string language) => language.ToLowerInvariant() switch
    {
        "csharp" => [".cs"],
        "python" => [".py"],
        "typescript" => [".ts", ".tsx"],
        "javascript" => [".js", ".jsx"],
        "java" => [".java"],
        "dart" => [".dart"],
        _ => [".cs", ".py", ".ts", ".js", ".java"]
    };
}
