# AIForged Integration Boilerplate

This is a .NET Core Web API boilerplate for integrating with the AIForged platform. It provides a foundation for document processing, management, and extraction capabilities through AIForged's services.

## Prerequisites

- .NET 6.0 or later
- AIForged API credentials (API Key)
- Basic understanding of RESTful APIs

## Authentication Setup

1. Create an `appsettings.json` file in your project root if it doesn't exist
2. Add the following configuration:

```json
{
  "AIForgedSettings": {
    "APIKey": "your-api-key-here",
    "BaseUrl": "https://portal.aiforged.com"
  }
}
```

**Note**: Both `APIKey` and `BaseUrl` are mandatory fields. The application will throw an exception if either is missing.

## Document Processing Flow

The integration supports two main ways of processing documents:

### 1. Webhook-Based Processing (Push)

Documents are automatically processed when AIForged sends webhook notifications to your `/AIForged/Incoming` endpoint. The flow is:

1. AIForged sends a webhook notification when a document is ready for processing
2. The system validates the document status
3. If status is "Verification", it acknowledges the verification check
4. For other statuses, it extracts and processes the document fields
5. Returns the extracted field values

### 2. Polling-Based Processing (Pull)

You can periodically check for new documents using the `/AIForged/GetDocuments` endpoint. The flow is:

1. Query documents based on date range, project ID, and other filters
2. Process the retrieved documents as needed
3. Extract information from the documents

## Main Operations

### 1. Upload Documents
```http
POST /AIForged/Upload
```
- Upload single or multiple files
- Requires project ID and service type definition ID (stpdId)
- Supports additional metadata like classification and usage type

### 2. Process Documents
```http
POST /AIForged/Process
```
- Initiates processing for specified document IDs
- Requires project ID and service type definition ID
- Can process multiple documents in a single request

### 3. Extract Information
```http
GET /AIForged/Extract/{docId}
```
- Extracts structured information from processed documents
- Returns field-value pairs based on the document type and configuration

### 4. Delete Documents
```http
DELETE /AIForged/DeleteDoc/{docId}
```
- Removes documents from the system
- Requires document ID

## Error Handling

The API implements comprehensive error handling:
- 400 Bad Request for invalid inputs
- 500 Internal Server Error for system-level issues
- Detailed error messages for debugging

## Advanced Features

### Document Management
- Get current user information
- Retrieve documents based on complex filters
- Track document status and processing progress

### Webhook Integration
- Automatic document processing
- Status verification support
- Field extraction and processing

## Development Setup

1. Clone the repository
2. Configure your `appsettings.json` with AIForged credentials
3. Run the application:
```bash
dotnet run
```

## API Documentation

When running in development mode, Swagger UI is available at `/swagger` endpoint, providing interactive API documentation.

## Health Checks

The application performs an authentication check on startup to ensure:
- Valid API credentials
- Successful connection to AIForged services
- Current user authentication status

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## Support

For support with the AIForged platform, please contact [AIForged Support] https://aiforged.com/contact/.

For issues with this integration boilerplate, please open an issue in the repository.