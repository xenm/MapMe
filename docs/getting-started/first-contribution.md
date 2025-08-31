# First Contribution Guide

This guide will walk you through making your first contribution to MapMe, from setup to pull request submission.

## Prerequisites

Before making your first contribution:
- ✅ Complete [Local Development Setup](./local-development.md)
- ✅ Verify application runs locally
- ✅ All tests pass (`dotnet test MapMe/MapMe.Tests`)
- ✅ Familiar with Git basics

## Step-by-Step First Contribution

### 1. Find an Issue to Work On

#### Good First Issues
Look for GitHub issues labeled:
- `good first issue` - Beginner-friendly tasks
- `documentation` - Documentation improvements
- `bug` - Small bug fixes
- `enhancement` - Minor feature additions

#### Create Your Own Issue
If you have an idea:
1. Search existing issues to avoid duplicates
2. Create new issue with clear description
3. Wait for team feedback before starting work

### 2. Set Up Your Development Branch

```bash
# Ensure you're on main branch
git checkout main

# Pull latest changes
git pull origin main

# Create feature branch
git checkout -b feature/your-feature-name
# or for bug fixes:
git checkout -b bugfix/issue-description
```

### 3. Make Your Changes

#### Code Changes
```bash
# Make your changes using your preferred IDE
# Follow existing code patterns and conventions

# Test your changes frequently
dotnet test MapMe/MapMe.Tests

# Run the application to verify functionality
dotnet run --project MapMe/MapMe/MapMe.csproj
```

#### Documentation Changes
```bash
# Update relevant documentation
# Follow markdown conventions
# Update navigation links if needed
```

### 4. Write Tests

#### For New Features
```csharp
// Add unit tests in MapMe.Tests/Unit/
[Fact]
public void YourNewFeature_ShouldWork_WhenConditionMet()
{
    // Arrange
    var service = new YourService();
    
    // Act
    var result = service.YourMethod();
    
    // Assert
    Assert.True(result.Success);
}
```

#### For Bug Fixes
```csharp
// Write test that reproduces the bug first
[Fact]
public void BugFix_ShouldNotFail_WhenEdgeCaseOccurs()
{
    // This test should fail before your fix
    // and pass after your fix
}
```

### 5. Commit Your Changes

#### Commit Message Format
```bash
# Use conventional commit format
git add .
git commit -m "feat: add user profile validation

- Add email format validation
- Add required field validation
- Update error messages
- Add unit tests for validation logic

Closes #123"
```

#### Commit Message Types
- `feat:` - New features
- `fix:` - Bug fixes
- `docs:` - Documentation changes
- `test:` - Test additions or modifications
- `refactor:` - Code refactoring
- `style:` - Code style changes
- `chore:` - Maintenance tasks

### 6. Push and Create Pull Request

```bash
# Push your branch
git push origin feature/your-feature-name

# GitHub will show a link to create pull request
# Or go to GitHub repository and click "Compare & pull request"
```

#### Pull Request Template
```markdown
## Description
Brief description of changes made.

## Type of Change
- [ ] Bug fix (non-breaking change that fixes an issue)
- [ ] New feature (non-breaking change that adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Documentation update

## Testing
- [ ] All existing tests pass
- [ ] New tests added for new functionality
- [ ] Manual testing completed

## Checklist
- [ ] Code follows project style guidelines
- [ ] Self-review completed
- [ ] Documentation updated if needed
- [ ] No breaking changes without discussion

## Related Issues
Closes #123
```

### 7. Code Review Process

#### What to Expect
1. **Automated Checks**: CI/CD pipeline runs tests and quality checks
2. **Team Review**: Team members review your code
3. **Feedback**: You may receive suggestions for improvements
4. **Iteration**: Make requested changes and push updates
5. **Approval**: Once approved, your PR will be merged

#### Responding to Feedback
```bash
# Make requested changes
# Commit with descriptive messages
git add .
git commit -m "address review feedback: improve error handling"

# Push updates
git push origin feature/your-feature-name
```

## Example First Contributions

### 1. Documentation Improvement
```bash
# Good first contribution: Fix typos or improve clarity
git checkout -b docs/improve-readme
# Edit README.md or documentation files
git add .
git commit -m "docs: fix typos in setup instructions"
git push origin docs/improve-readme
```

### 2. Simple Bug Fix
```bash
# Fix a small bug with existing tests
git checkout -b bugfix/fix-validation-message
# Fix the bug and ensure tests pass
git add .
git commit -m "fix: correct validation message for empty email"
git push origin bugfix/fix-validation-message
```

### 3. Add Unit Test
```bash
# Improve test coverage
git checkout -b test/add-profile-service-tests
# Add missing unit tests
git add .
git commit -m "test: add unit tests for UserProfileService edge cases"
git push origin test/add-profile-service-tests
```

## Development Best Practices

### Code Quality
- **Follow Existing Patterns**: Match the style of surrounding code
- **Keep Changes Small**: Smaller PRs are easier to review
- **Write Clear Comments**: Explain complex logic
- **Use Meaningful Names**: Variables and methods should be self-documenting

### Testing
- **Test Your Changes**: Ensure your code works as expected
- **Run All Tests**: Verify you haven't broken existing functionality
- **Add New Tests**: Cover new functionality with appropriate tests
- **Test Edge Cases**: Consider boundary conditions and error scenarios

### Git Practices
- **Atomic Commits**: Each commit should represent a single logical change
- **Clear Messages**: Write descriptive commit messages
- **Keep History Clean**: Use interactive rebase if needed
- **Stay Updated**: Regularly pull from main to avoid conflicts

## Common Pitfalls to Avoid

### Technical Issues
- **Not Running Tests**: Always run tests before committing
- **Ignoring Warnings**: Address compiler warnings and static analysis issues
- **Breaking Changes**: Avoid changes that break existing functionality
- **Missing Documentation**: Update docs when adding new features

### Process Issues
- **Working on Wrong Branch**: Always create feature branches from main
- **Large Pull Requests**: Keep PRs focused and reasonably sized
- **No Issue Reference**: Link PRs to relevant GitHub issues
- **Ignoring Feedback**: Address all review comments

## Getting Help

### Before Asking for Help
1. **Check Documentation**: Review relevant documentation sections
2. **Search Issues**: Look for similar problems in GitHub issues
3. **Read Error Messages**: Understand what the error is telling you
4. **Try Debugging**: Use debugger to understand the problem

### Where to Get Help
- **GitHub Issues**: Create issue with `help wanted` label
- **Code Review**: Ask questions in PR comments
- **Team Chat**: Reach out in development channels
- **Documentation**: Check [Troubleshooting Guide](../troubleshooting/README.md)

## After Your First Contribution

### Celebrate! 🎉
You've made your first contribution to MapMe! Here's what's next:

### Continue Contributing
- **Look for More Issues**: Find other issues to work on
- **Suggest Improvements**: Propose enhancements based on your experience
- **Help Others**: Review other contributors' pull requests
- **Share Knowledge**: Update documentation based on your learnings

### Grow Your Skills
- **Learn the Architecture**: Study [Architecture Documentation](../architecture/README.md)
- **Understand Testing**: Review [Testing Documentation](../testing/README.md)
- **Explore Advanced Features**: Work on more complex issues
- **Mentor Others**: Help new contributors get started

## Recognition

Contributors are recognized in:
- **GitHub Contributors**: Automatic recognition on repository
- **Release Notes**: Significant contributions mentioned in releases
- **Team Acknowledgment**: Recognition in team communications

---

**Estimated Time**: 2-4 hours for first contribution  
**Last Updated**: 2025-08-30  
**Maintained By**: Development Team
