# News Aggregator

A microservices-based news aggregator application with text-to-speech and grammar analysis capabilities.

## Project Structure

- **NewsAggregator.Core**: Contains core interfaces and domain models
- **NewsAggregator.Proto**: Contains gRPC service definitions
- **NewsAggregator.Services**: Implementation of microservices
- **NewsAggregator.Gateway**: API Gateway for client applications
- **NewsAggregator.Shared**: Shared utilities and components

## Features

1. **News Aggregation**
   - Collect news from multiple sources
   - Categorize news articles
   - Search and filter capabilities

2. **Text-to-Speech**
   - Convert news articles to speech
   - Multiple voice options
   - Adjustable speech parameters

3. **Grammar Analysis**
   - Analyze text for grammar issues
   - Vocabulary explanations
   - Sentence structure analysis

## Technologies

- .NET 7.0
- gRPC for service communication
- RabbitMQ for async messaging
- Entity Framework Core for data access
- Azure Cognitive Services for Text-to-Speech
- Natural Language Processing libraries for grammar analysis

## Setup Instructions

1. **Prerequisites**
   - .NET 7.0 SDK
   - RabbitMQ Server
   - SQL Server (or preferred database)

2. **Configuration**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "your_database_connection_string"
     },
     "RabbitMQ": {
       "HostName": "localhost",
       "UserName": "guest",
       "Password": "guest"
     }
   }
   ```

3. **Build and Run**
   ```bash
   # Build solution
   dotnet build

   # Run services
   cd NewsAggregator.Services
   dotnet run

   # Run API Gateway
   cd ../NewsAggregator.Gateway
   dotnet run
   ```

## API Documentation

### News Service
- `GET /api/news`: Get news articles
- `GET /api/news/category/{category}`: Get news by category
- `POST /api/news/sources`: Add news source

### Text-to-Speech Service
- `POST /api/tts/convert`: Convert text to speech
- `GET /api/tts/voices`: Get available voices

### Grammar Service
- `POST /api/grammar/analyze`: Analyze text
- `GET /api/grammar/word/{word}`: Get word definition

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details. 