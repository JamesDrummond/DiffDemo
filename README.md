# DiffDemo

A Blazor Server application for managing and versioning AI prompts with built-in diff viewing capabilities.

## Features

- **Prompt Management**: Create, edit, and manage AI prompts
- **Version Control**: Automatic versioning of prompts with history tracking
- **Diff Viewer**: Visual comparison of prompt versions using side-by-side and inline diff views
- **MongoDB Integration**: Persistent storage using MongoDB
- **Modern UI**: Responsive design built with Bootstrap

## Prerequisites

- [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) or later
- MongoDB instance (local or cloud-based like MongoDB Atlas)
- Visual Studio 2022, Visual Studio Code, or any IDE with .NET support

## Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd DiffDemo
```

### 2. Configure MongoDB

Add to the `appsettings.Development.json` file with your MongoDB connection string:

```json
{
  "MongoDB": {
    "ConnectionString": "mongodb+srv://<username>:<password>@ai-prompts.ytl4utt.mongodb.net/?appName=AI-Prompts"
  }
}
```

For MongoDB Atlas, use a connection string in the format:
```
mongodb+srv://username:password@cluster.mongodb.net/?appName=AI-Prompts
```

### 3. Restore Dependencies

```bash
dotnet restore
```

### 4. Run the Application

```bash
dotnet run
```

The application will be available at:
- HTTP: `http://localhost:5054`
- HTTPS: `https://localhost:7020`

## Project Structure

```
DiffDemo/
├── Components/          # Reusable Blazor components
│   └── DiffViewer.razor # Diff visualization component
├── Data/               # Data services
├── Models/             # Data models
│   ├── Prompt.cs       # Prompt entity
│   └── PromptHistory.cs
├── Pages/              # Blazor pages/routes
│   ├── Index.razor
│   ├── EditPrompt.razor
│   ├── PromptsList.razor
│   └── PromptHistory.razor
├── Services/           # Business logic services
│   └── MongoDbService.cs
├── Shared/             # Shared components and layouts
├── wwwroot/            # Static files (CSS, JS, images)
└── Program.cs          # Application entry point
```

## Technologies Used

- **.NET 6.0**: Framework
- **Blazor Server**: Web UI framework
- **MongoDB.Driver 2.28.0**: MongoDB client library
- **DiffPlex 1.7.4**: Text diffing library
- **Bootstrap**: CSS framework

## Configuration

### MongoDB Settings

The application requires MongoDB configuration in `appsettings.json`:

- `ConnectionString`: MongoDB connection string
- `DatabaseName`: Name of the database (default: "PromptEditor")

### Logging

Logging levels can be configured in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

## Usage

1. **View All Prompts**: Navigate to `/` to see all saved prompts
2. **Create Prompt**: Click "Create New Prompt" to add a new prompt
3. **Edit Prompt**: Click "Edit" on any prompt to modify it
4. **View History**: Click "History" to see version history and compare changes using the diff viewer

## Development

### Building the Project

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

## License

[Add your license information here]

