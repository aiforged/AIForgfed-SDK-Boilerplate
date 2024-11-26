using AIForged.API;
using AIForgfed_Intergation_Boilerplate.Models;
using LarcAI;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.ObjectModel;
using System.Runtime.ConstrainedExecution;
using System.Threading.Tasks;

namespace AIForged_Integration_Boilerplate.Controllers
{
    /// <summary>
    /// Controller for handling AIForged-related operations including document processing,
    /// simple user management, and file operations.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class AIForgedController : ControllerBase
    {
        private readonly Context _context;
        private const string GenericErrorMessage = "An unexpected error occurred while processing your request.";

        public AIForgedController(Context context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Retrieves the current user's information
        /// </summary>
        [HttpGet("GetCurrentUser")]
        [ProducesResponseType(typeof(UserViewModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<UserViewModel>> GetCurrentUser()
        {
            try
            {
                var response = await _context.AccountClient.GetCurrentUserAsync();
                return Ok(response.Result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"{GenericErrorMessage} Details: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes incoming (webhooks) document requests and extracts fields
        /// </summary>
        [HttpPost("Incoming")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ProcessIncoming([FromBody] DocumentRequest request)
        {
            try
            {
                // Validate request
                if (request == null || request.DocId <= 0)
                {
                    return BadRequest("Invalid document request.");
                }

                // Check document verification status
                if (request.Status == "Verification")
                {
                    return Ok("Document verification in progress.");
                }

                // Extract and process fields
                var fieldsResponse = await _context.ParametersClient.ExtractAsync(request.DocId);
                var fields = fieldsResponse.Result
                    .Select(field => new KeyValuePair<string, string>(field.Name, field.Value))
                    .ToList();

                string responseText = string.Join(", ", fields.Select(f => $"Field: {f.Key} Value: {f.Value}"));
                return Ok(responseText);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"{GenericErrorMessage} Details: {ex.Message}");
            }
        }


        /// <summary>
        /// Retrieves documents based on specified criteria
        /// </summary>
        [HttpGet("GetDocuments")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ObservableCollection<DocumentViewModel>>> GetDocuments(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] int projectId,
            [FromQuery] int serviceId,
            [FromQuery] UsageType usage,
            [FromQuery] List<DocumentStatus> status)
        {
            try
            {
                if (projectId <= 0 || serviceId <= 0)
                {
                    return BadRequest("Invalid projectId or serviceId.");
                }

                var userResponse = await _context.AccountClient.GetCurrentUserAsync();
                var user = userResponse.Result;

                var response = await _context.DocumentClient.GetExtendedAsync(
                    user.Id,
                    projectId,
                    serviceId,
                    usage,
                    status,
                    null, null, null,
                    startDate,
                    endDate,
                    null, null, null, null, null, null, null, null, null, null, null, null, null, null);

                if (response?.Result == null)
                {
                    return StatusCode(500, $"{GenericErrorMessage} No documents returned.");
                }

                return Ok(response.Result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"{GenericErrorMessage} Details: {ex.Message}");
            }
        }

        /// <summary>
        /// Uploads files to the AIForged system
        /// </summary>
        [HttpPost("Upload")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<DocumentViewModel>>> UploadFile(
            IFormFileCollection files,
            int stpdId,
            int projectId,
            int? classId,
            DocumentStatus status,
            UsageType usage,
            int? masterId,
            string? comment = null,
            string? externalId = null,
            string? result = null,
            string? resultId = null,
            int? resultIndex = null,
            Guid? guid = null)
        {
            try
            {
                // Validate input parameters
                if (files == null || !files.Any())
                {
                    return BadRequest("No files provided for upload.");
                }

                if (stpdId <= 0 || projectId <= 0)
                {
                    return BadRequest("Invalid stpdId or projectId.");
                }

                // Get current user
                var userResponse = await _context.AccountClient.GetCurrentUserAsync();
                var user = userResponse.Result;

                // upload each file
                foreach (var file in files)
                {
                    using var memoryStream = new MemoryStream();
                    await file.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    var fileParam = new FileParameter(memoryStream, file.FileName);
                    var response = await _context.DocumentClient.UploadFileAsync(
                        stpdId,
                        user.Id,
                        projectId,
                        classId,
                        status,
                        usage,
                        masterId,
                        comment,
                        externalId,
                        result,
                        resultId,
                        resultIndex,
                        guid ?? Guid.NewGuid(),
                        fileParam);

                    if (response.StatusCode != 200)
                    {
                        return BadRequest($"Error uploading file {file.FileName}");
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"{GenericErrorMessage} Details: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes documents with specified IDs
        /// </summary>
        [HttpPost("Process")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ProcessDocument(
            int? stpdId,
            int? projectId,
            [FromQuery] List<int> docIds)
        {
            try
            {
                // Validate input parameters
                if (!stpdId.HasValue || stpdId <= 0 ||
                    !projectId.HasValue || projectId <= 0 ||
                    docIds == null || !docIds.Any())
                {
                    return BadRequest("Invalid input parameters.");
                }

                await _context.ServicesClient.ProcessAsync(
                    _context.CurrentUserId,
                    projectId.Value,
                    stpdId.Value,
                    docIds,
                    null, null, null, null, null, null, null, null);

                return Ok("Documents processed successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"{GenericErrorMessage} Details: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the status of a document
        /// </summary>
        [HttpPut("SetDocStatus")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SetDocStatus(int docId, [FromQuery] DocumentStatus status)
        {
            try
            {
                // Validate input parameters
                if (docId <= 0)
                {
                    return BadRequest("Invalid document ID.");
                }

                // Get the current document
                var response = await _context.DocumentClient.GetDocumentAsync(docId);
                if (response?.Result == null)
                {
                    return BadRequest($"Document with ID {docId} not found.");
                }

                // Update the document status
                DocumentViewModel doc = response.Result;
                doc.Status = status;

                // Perform the update
                var updateResponse = await _context.DocumentClient.UpdateAsync(doc);
                if (updateResponse?.StatusCode != 200)
                {
                    return StatusCode(500, $"Failed to update document status. Status code: {updateResponse?.StatusCode}");
                }

                return Ok($"Document {docId} status updated to {status} successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"{GenericErrorMessage} Details: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a document by ID
        /// </summary>
        [HttpDelete("DeleteDoc")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteDoc(int docId, bool deleteRecursive)
        {
            try
            {
                if (docId <= 0)
                {
                    return BadRequest("Invalid document ID.");
                }

                var response = await _context.DocumentClient.DeleteAsync(docId, deleteRecursive,false);
                return Ok($"Document {docId} deleted successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"{GenericErrorMessage} Details: {ex.Message}");
            }
        }

        /// <summary>
        /// Extracts information from a document
        /// </summary>
        [HttpGet("Extract")]
        [ProducesResponseType(typeof(ObservableCollection<DocumentExtraction>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ObservableCollection<DocumentExtraction>>> Extract(int docId)
        {
            try
            {
                if (docId <= 0)
                {
                    return BadRequest("Invalid document ID.");
                }

                var response = await _context.ParametersClient.ExtractAsync(docId);
                return Ok(response.Result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"{GenericErrorMessage} Details: {ex.Message}");
            }
        }
    }
}