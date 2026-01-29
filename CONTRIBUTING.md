# Contributing to Ausgleichsliste

Thank you for considering contributing to Ausgleichsliste! We welcome contributions from everyone.

## Development Setup

1. **Prerequisites**
   - .NET 9.0 SDK
   - Git
   - A code editor (Visual Studio, VS Code, or Rider)

2. **Clone and Setup**
   ```bash
   git clone https://github.com/username/ausgleichsliste.git
   cd ausgleichsliste
   dotnet restore
   dotnet ef database update --project AusgleichslisteApp
   ```

3. **Run Tests**
   ```bash
   dotnet test
   ```

4. **Start Development Server**
   ```bash
   dotnet run --project AusgleichslisteApp
   ```

## Making Changes

1. **Fork the repository** on GitHub
2. **Create a feature branch** from main: `git checkout -b feature/my-feature`
3. **Make your changes** and write tests if applicable
4. **Run tests** to ensure everything works: `dotnet test`
5. **Commit your changes** with clear commit messages
6. **Push to your fork** and **create a Pull Request**

## Code Style

- Follow standard C# conventions and .NET naming guidelines
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Keep methods focused and classes cohesive

## Pull Request Guidelines

- **Describe your changes** clearly in the PR description
- **Reference any related issues** using `#issue-number`
- **Ensure all tests pass** before requesting review
- **Keep PRs focused** - one feature or fix per PR when possible

## Reporting Issues

- Use the **GitHub issue tracker** to report bugs or request features
- **Search existing issues** first to avoid duplicates
- **Provide clear steps to reproduce** for bug reports
- **Include system information** when relevant

## Questions?

Feel free to open an issue for questions or reach out to the maintainers.

Thank you for contributing! 🎉