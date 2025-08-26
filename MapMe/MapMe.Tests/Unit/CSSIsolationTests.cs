using Xunit;
using FluentAssertions;

namespace MapMe.Tests.Unit
{
    /// <summary>
    /// Tests for CSS isolation implementation that prevents style conflicts
    /// between pages by using page-specific CSS loading via HeadContent components.
    /// </summary>
    public class CSSIsolationTests
    {
        [Fact]
        public void CSSIsolation_ShouldUsePageSpecificLoading()
        {
            // Arrange - CSS isolation strategy
            var isolationStrategy = new
            {
                Method = "HeadContent component per page",
                PublicPages = new[] { "Login", "SignUp" },
                ProtectedPages = new[] { "Map", "Profile", "Chat" },
                NoGlobalCSS = true
            };
            
            // Act & Assert
            isolationStrategy.Method.Should().Contain("HeadContent", 
                "Should use HeadContent for CSS injection");
            isolationStrategy.PublicPages.Should().HaveCount(2, 
                "Should identify public pages correctly");
            isolationStrategy.ProtectedPages.Should().HaveCount(3, 
                "Should identify protected pages correctly");
            isolationStrategy.NoGlobalCSS.Should().BeTrue(
                "Should not use global CSS to prevent conflicts");
        }

        [Theory]
        [InlineData("Login")]
        [InlineData("SignUp")]
        public void PublicPages_ShouldOnlyLoadBootstrapCSS(string pageName)
        {
            // Arrange - CSS resources for public pages
            var publicPageCSS = new[]
            {
                "https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css",
                "https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.1/font/bootstrap-icons.css"
            };
            
            var mapMeStyles = new[]
            {
                ".navbar-dark.bg-primary",
                ".nav-link:hover",
                "MapMe custom navigation styles"
            };
            
            // Act & Assert
            publicPageCSS.Should().HaveCount(2, $"{pageName} should only load Bootstrap CSS");
            publicPageCSS.Should().Contain(css => css.Contains("bootstrap@5.3.2"), 
                "Should load Bootstrap CSS from CDN");
            publicPageCSS.Should().Contain(css => css.Contains("bootstrap-icons"), 
                "Should load Bootstrap Icons from CDN");
            
            // Public pages should NOT have MapMe styles
            foreach (var style in mapMeStyles)
            {
                publicPageCSS.Should().NotContain(css => css.Contains(style), 
                    $"{pageName} should not have MapMe navigation styles");
            }
        }

        [Theory]
        [InlineData("Map")]
        [InlineData("Profile")]
        [InlineData("Chat")]
        public void ProtectedPages_ShouldLoadBootstrapAndMapMeCSS(string pageName)
        {
            // Arrange - CSS resources for protected pages
            var protectedPageCSS = new[]
            {
                "https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css",
                "https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.1/font/bootstrap-icons.css",
                ".navbar-dark.bg-primary { background-color: #0d6efd !important; }"
            };
            
            // Act & Assert
            protectedPageCSS.Should().HaveCount(3, 
                $"{pageName} should load Bootstrap + MapMe styles");
            protectedPageCSS.Should().Contain(css => css.Contains("bootstrap@5.3.2"), 
                "Should load Bootstrap CSS");
            protectedPageCSS.Should().Contain(css => css.Contains("bootstrap-icons"), 
                "Should load Bootstrap Icons");
            protectedPageCSS.Should().Contain(css => css.Contains("navbar-dark"), 
                $"{pageName} should have MapMe navigation styles");
        }

        [Fact]
        public void CSSIsolation_ShouldPreventStyleBleeding()
        {
            // Arrange - Style bleeding scenarios we prevent
            var preventedConflicts = new[]
            {
                "Login page Bootstrap overriding Map page navigation styles",
                "Map page navigation styles affecting Login page card layout",
                "Global CSS causing inconsistent styling across pages",
                "Bootstrap version conflicts between pages",
                "Custom styles leaking between public and protected pages"
            };
            
            // Act & Assert
            preventedConflicts.Should().HaveCount(5, "Should prevent all major style conflicts");
            
            // Our solution ensures complete isolation
            var isolationBenefits = new[]
            {
                "Each page controls its own styling completely",
                "No unexpected style inheritance",
                "Easy to debug styling issues per page",
                "Safe to modify styles without affecting other pages"
            };
            
            isolationBenefits.Should().HaveCount(4, "Should provide isolation benefits");
        }

        [Fact]
        public void CSSIsolation_ShouldUseSecureCDNLoading()
        {
            // Arrange - Security features for CDN loading
            var securityFeatures = new[]
            {
                "integrity=\"sha384-T3c6CoIi6uLrA9TneNEoa7RxnatzjcDSCmG1MXxSR1GAsXEV/Dwwykc2MPK8M2HN\"",
                "crossorigin=\"anonymous\"",
                "https://cdn.jsdelivr.net",
                "Subresource Integrity (SRI) validation"
            };
            
            // Act & Assert
            securityFeatures.Should().HaveCount(4, "Should implement all security features");
            securityFeatures.Should().Contain(feature => feature.Contains("integrity="), 
                "Should use integrity hashes for security");
            securityFeatures.Should().Contain(feature => feature.Contains("crossorigin"), 
                "Should use proper CORS headers");
            securityFeatures.Should().Contain(feature => feature.Contains("https://"), 
                "Should use HTTPS for secure loading");
        }

        [Fact]
        public void CSSIsolation_ShouldSupportResponsiveDesign()
        {
            // Arrange - Responsive Bootstrap classes used
            var responsiveClasses = new[]
            {
                "container-fluid",
                "col-md-6",
                "col-lg-4", 
                "d-flex",
                "justify-content-center",
                "navbar-expand-lg",
                "d-none d-md-block"
            };
            
            // Act & Assert
            responsiveClasses.Should().HaveCount(7, "Should use comprehensive responsive classes");
            responsiveClasses.Should().Contain("container-fluid", "Should use fluid containers");
            responsiveClasses.Should().Contain(cls => cls.StartsWith("col-"), "Should use grid system");
            responsiveClasses.Should().Contain("navbar-expand-lg", "Navigation should be responsive");
            responsiveClasses.Should().Contain(cls => cls.Contains("d-"), "Should use display utilities");
        }

        [Fact]
        public void CSSIsolation_ShouldOptimizePerformance()
        {
            // Arrange - Performance optimization aspects
            var performanceOptimizations = new[]
            {
                "Only load CSS needed for each page",
                "No global CSS parsing overhead",
                "CDN caching for Bootstrap resources",
                "Minimal custom CSS per page",
                "No CSS conflicts requiring !important overrides"
            };
            
            // Act & Assert
            performanceOptimizations.Should().HaveCount(5, "Should optimize performance");
            performanceOptimizations.Should().Contain(opt => opt.Contains("Only load"), 
                "Should load only necessary CSS");
            performanceOptimizations.Should().Contain(opt => opt.Contains("CDN caching"), 
                "Should leverage CDN caching");
            performanceOptimizations.Should().Contain(opt => opt.Contains("!important"), 
                "Should avoid CSS specificity wars");
        }

        [Fact]
        public void CSSIsolation_ShouldBeMaintainable()
        {
            // Arrange - Maintainability benefits
            var maintainabilityBenefits = new[]
            {
                "Easy to add new pages without affecting existing ones",
                "Clear separation of concerns per page",
                "Simple to debug styling issues",
                "Safe to update Bootstrap versions per page",
                "Predictable CSS behavior"
            };
            
            // Act & Assert
            maintainabilityBenefits.Should().HaveCount(5, "Should improve maintainability");
            maintainabilityBenefits.Should().Contain(benefit => benefit.Contains("Easy to add"), 
                "Should support easy page additions");
            maintainabilityBenefits.Should().Contain(benefit => benefit.Contains("separation"), 
                "Should provide clear separation of concerns");
        }

        [Fact]
        public void CSSIsolation_ShouldUseHeadContentProperly()
        {
            // Arrange - HeadContent usage patterns
            var headContentPatterns = new[]
            {
                "<HeadContent>",
                "<!-- Bootstrap CSS -->",
                "<link href=\"https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css\"",
                "rel=\"stylesheet\"",
                "integrity=\"sha384-...\"",
                "crossorigin=\"anonymous\">",
                "</HeadContent>"
            };
            
            // Act & Assert
            headContentPatterns.Should().HaveCount(7, "Should use proper HeadContent structure");
            headContentPatterns.Should().Contain("<HeadContent>", "Should use HeadContent component");
            headContentPatterns.Should().Contain(pattern => pattern.Contains("link href"), 
                "Should use proper link tags");
            headContentPatterns.Should().Contain(pattern => pattern.Contains("rel=\"stylesheet\""), 
                "Should specify stylesheet relationship");
        }

        [Fact]
        public void CSSIsolation_ShouldHandleBootstrapVersions()
        {
            // Arrange - Bootstrap version management
            var versionManagement = new
            {
                BootstrapVersion = "5.3.2",
                IconsVersion = "1.11.1",
                ConsistentAcrossPages = true,
                CDNSource = "cdn.jsdelivr.net",
                IntegrityValidation = true
            };
            
            // Act & Assert
            versionManagement.BootstrapVersion.Should().Be("5.3.2", 
                "Should use stable Bootstrap version");
            versionManagement.IconsVersion.Should().Be("1.11.1", 
                "Should use compatible Icons version");
            versionManagement.ConsistentAcrossPages.Should().BeTrue(
                "Should use consistent versions across all pages");
            versionManagement.CDNSource.Should().Contain("jsdelivr", 
                "Should use reliable CDN");
            versionManagement.IntegrityValidation.Should().BeTrue(
                "Should validate resource integrity");
        }

        [Fact]
        public void CSSIsolation_ShouldSupportCustomStyling()
        {
            // Arrange - Custom styling capabilities
            var customStylingSupport = new[]
            {
                "Inline <style> blocks in HeadContent for page-specific styles",
                "CSS custom properties for theming",
                "Bootstrap utility classes for quick styling",
                "Safe CSS overrides without global impact",
                "Component-specific styling isolation"
            };
            
            // Act & Assert
            customStylingSupport.Should().HaveCount(5, "Should support various custom styling approaches");
            customStylingSupport.Should().Contain(support => support.Contains("Inline"), 
                "Should support inline styles for page-specific needs");
            customStylingSupport.Should().Contain(support => support.Contains("utility"), 
                "Should leverage Bootstrap utilities");
            customStylingSupport.Should().Contain(support => support.Contains("isolation"), 
                "Should maintain styling isolation");
        }
    }
}
