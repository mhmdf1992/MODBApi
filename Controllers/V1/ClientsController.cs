using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using MODB.Api.Attributes;
using MODB.FlatFileDB;
using MODB.Api.DTOs;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace MODB.Api.Controllers.V1
{
    [AdminApiKey]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ClientsController : ControllerBase
    {
        
        readonly IKeyValDB _clientsDB;
        readonly ConcurrentDictionary<string, ConcurrentDictionary<string, FlatFileKeyValDB>> _clientsDBs;
        public ClientsController(IKeyValDB clientsDB, ConcurrentDictionary<string, ConcurrentDictionary<string, FlatFileKeyValDB>> clientsDBs)
        {
            _clientsDB = clientsDB;
            _clientsDBs = clientsDBs;
        }

        OkObjectResult OKResult<T> (T res, string processingTime){
            Response.Headers.Add("processing-time", processingTime);
           return Ok(new MODBResponse<T>(res));
        }
        OkObjectResult OKMODBRecords(PagedList<string> res, string processingTime, string resultType){
            Response.Headers.Add("processing-time", processingTime);
            Response.Headers.Add("result-type", resultType);
           return Ok( resultType == "json" ? new MODBRecordsJsonResponse(res) : new MODBRecordsResponse(res));
        }

        OkObjectResult OKMODBRecord(string res, string processingTime, string resultType){
            Response.Headers.Add("processing-time", processingTime);
            Response.Headers.Add("result-type", resultType);
           return Ok(resultType == "json" ? new MODBRecordJsonResponse(res) : new MODBRecordResponse(res));
        }

        OkObjectResult OKResult (string processingTime){
            Response.Headers.Add("processing-time", processingTime);
            return Ok(new MODBResponse());
        }

        [HttpGet]
        [ProducesResponseType(typeof(MODBResponse<PagedList<string>>), 200)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 401)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 500)]
        public async Task<IActionResult> GetClientsAsync([FromQuery] GetFilteredQueryParams obj){
            Request.Headers.TryGetValue("result-type", out var resultType);
            var res = Utilities.StopWatch(() => _clientsDB.Get(obj.Tags, obj.From, obj.To, obj.Page, obj.PageSize));
            return await Task.FromResult(OKMODBRecords(res.Result, res.ProcessingTime, resultType));
        }   

        [HttpGet("{key}")]
        [ProducesResponseType(typeof(MODBResponse<string>), 200)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ValidationError), 400)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 401)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 404)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 500)]
        public async Task<IActionResult> GetClientAsync([FromRoute] string key)
        {
            try{
                Request.Headers.TryGetValue("result-type", out var resultType);
                var res = Utilities.StopWatch(() => _clientsDB.Get(key));
                return await Task.FromResult(OKMODBRecord(res.Result, res.ProcessingTime, resultType));
            }catch(ArgumentException ex){
                throw new Exceptions.ApplicationValidationErrorException(ex, HttpContext.TraceIdentifier);
            }
            catch(FlatFileDB.Exceptions.KeyNotFoundException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.NotFound, System.Net.HttpStatusCode.NotFound.ToString(), ex.Message);
            }
        }
        
        [HttpPost]
        [ProducesResponseType(typeof(MODBResponse), 200)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ValidationError), 400)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 401)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 500)]
        public async Task<IActionResult> SetClientAsync([FromQuery] SetKeyQueryParams obj, [FromBody] Stream stream){
            try{
                var res = Utilities.StopWatch(() => {
                    _clientsDB.Set(obj.Key, stream, obj.Tags, obj.TimeStamp);
                    _clientsDBs.TryAdd(obj.Key, new ConcurrentDictionary<string, FlatFileKeyValDB>());});
                return await Task.FromResult(OKResult(res.ProcessingTime));
            }catch(ArgumentException ex){
                throw new Exceptions.ApplicationValidationErrorException(ex, HttpContext.TraceIdentifier);
            }
            
        }

        [HttpDelete("{key}")]
        [ProducesResponseType(typeof(MODBResponse), 200)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ValidationError), 400)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 401)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 404)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 500)]
        public async Task<IActionResult> DeleteClientAsync([FromRoute] string key)
        {
            try{
                var res = Utilities.StopWatch(() => {
                    _clientsDB.Delete(key);
                    _clientsDBs.TryRemove(key, out ConcurrentDictionary<string, FlatFileKeyValDB> val);
                });
                return await Task.FromResult(OKResult(res.ProcessingTime));
            }catch(ArgumentException ex){
                throw new Exceptions.ApplicationValidationErrorException(ex, HttpContext.TraceIdentifier);
            }
            catch(FlatFileDB.Exceptions.KeyNotFoundException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.NotFound, System.Net.HttpStatusCode.NotFound.ToString(), ex.Message);
            }
        }

        [HttpGet("Tags")]
        [ProducesResponseType(typeof(MODBResponse<PagedList<string>>), 200)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ValidationError), 400)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 404)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 401)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 500)]
        public async Task<IActionResult> GetTagsAsync([FromQuery] GetTagsFilteredQueryParams obj){
            try{
                var res = Utilities.StopWatch(() => _clientsDB.GetTags(obj.Text, obj.Page, obj.PageSize));
                return await Task.FromResult(OKResult(res.Result, res.ProcessingTime));
            }catch(ArgumentException ex){
                throw new Exceptions.ApplicationValidationErrorException(ex, HttpContext.TraceIdentifier);
            }
            catch(Exceptions.KeyNotFoundException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.NotFound, System.Net.HttpStatusCode.NotFound.ToString(), ex.Message);
            }
        }
    }
}
