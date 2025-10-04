# Mediscribe_AI

## Overview
Mediscribe_AI is a .NET 8 web service for generating SOAP notes using AI. It provides an API endpoint for clinicians and healthcare applications to convert patient narratives into structured SOAP notes, including both JSON and HTML formats.

## Features
- Generates SOAP notes (Subjective, Objective, Assessment, Plan) from patient narratives
- Returns both JSON and HTML formats for easy integration
- Uses Gemini AI API for content generation
- CORS support for frontend integration (e.g., Angular, React, Vue)
- Swagger/OpenAPI documentation

## Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 or later

### Setup
1. Clone the repository:
2. Open the solution in Visual Studio.
3. Add your Gemini API key to `appsettings.json`:
4. Build and run the project:

## Usage
- The main service is `SoapNotesService` in `Services/SoapNotesService.cs`.
- Register and use the service via dependency injection.
- Access the API endpoints via Swagger UI at `/swagger` when running in development.

## Configuration
- CORS is enabled for `http://localhost:4200` by default. Update in `Program.cs` as needed.
- API key for Gemini must be set in `appsettings.json`.

## Contributing
Contributions are welcome! Please fork the repository and submit a pull request.

## License
This project is licensed under the MIT License.
