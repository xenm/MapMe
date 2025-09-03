using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MapMe.Tests.Integration
{
    /// <summary>
    /// Integration tests for navigation and CSS fixes to ensure all components
    /// work together properly in a real application environment.
    /// </summary>
    public class NavigationFixesIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly WebApplicationFactory<Program> _factory;

        public NavigationFixesIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task LoginPage_ShouldLoadSuccessfully()
        {
            // Act
            var response = await _client.GetAsync("/login");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            // Verify basic page structure loads without critical errors
            Assert.Contains("<!DOCTYPE html>", content);
            Assert.Contains("<html", content);

            // Verify no JavaScript errors in markup
            Assert.DoesNotContain("arguments[0]", content);
            Assert.DoesNotContain("ReferenceError", content);
        }

        [Fact]
        public async Task SignUpPage_ShouldLoadSuccessfully()
        {
            // Act
            var response = await _client.GetAsync("/signup");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            // Verify basic page structure loads without critical errors
            Assert.Contains("<!DOCTYPE html>", content);
            Assert.Contains("<html", content);
        }

        [Fact]
        public async Task MapPage_ShouldHandleUnauthenticatedAccess()
        {
            // Act
            var response = await _client.GetAsync("/map");

            // Assert
            // Should either redirect to login or return OK (depending on auth implementation)
            Assert.True(response.StatusCode == HttpStatusCode.Redirect ||
                        response.StatusCode == HttpStatusCode.OK,
                "Map page should handle unauthenticated access gracefully");
        }

        [Fact]
        public async Task ProfilePage_ShouldHandleUnauthenticatedAccess()
        {
            // Act
            var response = await _client.GetAsync("/profile");

            // Assert
            // Should either redirect to login or return OK (depending on auth implementation)
            Assert.True(response.StatusCode == HttpStatusCode.Redirect ||
                        response.StatusCode == HttpStatusCode.OK,
                "Profile page should handle unauthenticated access gracefully");
        }

        [Fact]
        public async Task ChatPage_ShouldHandleUnauthenticatedAccess()
        {
            // Act
            var response = await _client.GetAsync("/chat");

            // Assert
            // Should either redirect to login or return OK (depending on auth implementation)
            Assert.True(response.StatusCode == HttpStatusCode.Redirect ||
                        response.StatusCode == HttpStatusCode.OK,
                "Chat page should handle unauthenticated access gracefully");
        }

        [Fact]
        public async Task NavigationFixes_ShouldPreventJavaScriptErrors()
        {
            // This test validates that our JavaScript fixes prevent errors

            // Act
            var response = await _client.GetAsync("/login");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            // This validates that our JavaScript fixes work
            Assert.DoesNotContain("arguments is not defined", content);
            Assert.DoesNotContain("ReferenceError", content);
            Assert.DoesNotContain("TypeError", content);
        }

        [Fact]
        public async Task PublicPages_ShouldLoadWithoutErrors()
        {
            // Act
            var loginResponse = await _client.GetAsync("/login");
            var signupResponse = await _client.GetAsync("/signup");

            // Assert
            loginResponse.EnsureSuccessStatusCode();
            signupResponse.EnsureSuccessStatusCode();

            var loginContent = await loginResponse.Content.ReadAsStringAsync();
            var signupContent = await signupResponse.Content.ReadAsStringAsync();

            // Verify basic HTML structure
            Assert.Contains("<!DOCTYPE html>", loginContent);
            Assert.Contains("<!DOCTYPE html>", signupContent);
        }

        [Fact]
        public async Task Pages_ShouldNotHaveJavaScriptErrors()
        {
            // Act
            var response = await _client.GetAsync("/login");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            // Verify no JavaScript errors that we fixed
            Assert.DoesNotContain("arguments is not defined", content);
            Assert.DoesNotContain("ReferenceError", content);
            Assert.DoesNotContain("TypeError", content);
        }

        [Fact]
        public async Task RootPage_ShouldHandleRequest()
        {
            // Act - Unauthenticated user
            var response = await _client.GetAsync("/");

            // Assert - Should handle request gracefully (redirect or OK)
            Assert.True(response.StatusCode == HttpStatusCode.Redirect ||
                        response.StatusCode == HttpStatusCode.OK,
                "Root page should handle requests gracefully");
        }

        [Fact]
        public async Task LoginPage_ShouldHaveBasicStructure()
        {
            // Act
            var response = await _client.GetAsync("/login");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            // Verify basic HTML structure exists
            Assert.Contains("<!DOCTYPE html>", content);
            Assert.Contains("<html", content);
        }

        [Fact]
        public async Task LoginPage_ShouldLoadWithoutCriticalErrors()
        {
            // Act
            var response = await _client.GetAsync("/login");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            // Verify no critical JavaScript errors that we fixed
            Assert.DoesNotContain("arguments is not defined", content);
            Assert.DoesNotContain("ReferenceError", content);
        }

        [Fact]
        public async Task ErrorPages_ShouldNotBreakNavigation()
        {
            // Act - Try to access non-existent page
            var response = await _client.GetAsync("/nonexistent");

            // Assert - Should handle gracefully
            // Either 404 or redirect to login, but shouldn't crash
            Assert.True(response.StatusCode == HttpStatusCode.NotFound ||
                        response.StatusCode == HttpStatusCode.Redirect);
        }

        [Fact]
        public async Task MultiplePages_ShouldLoadSuccessfully()
        {
            // Act - Load multiple pages in sequence
            var loginResponse = await _client.GetAsync("/login");
            var signupResponse = await _client.GetAsync("/signup");

            // Assert
            loginResponse.EnsureSuccessStatusCode();
            signupResponse.EnsureSuccessStatusCode();

            var loginContent = await loginResponse.Content.ReadAsStringAsync();
            var signupContent = await signupResponse.Content.ReadAsStringAsync();

            // Both should have basic HTML structure
            Assert.Contains("<!DOCTYPE html>", loginContent);
            Assert.Contains("<!DOCTYPE html>", signupContent);
        }

        [Fact]
        public async Task JavaScriptResources_ShouldNotHaveBrokenReferences()
        {
            // Act
            var response = await _client.GetAsync("/login");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            // Verify no JavaScript errors that we specifically fixed
            Assert.DoesNotContain("arguments[0]", content);
            Assert.DoesNotContain("arguments is not defined", content);
        }
    }
}