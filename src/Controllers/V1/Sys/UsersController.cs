using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using MO.MODBApi.Attributes;
using MO.MODB;
using MO.MODBApi.DTOs;
using System;
using MO.MODBApi.DataModels.Sys;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace MO.MODBApi.Controllers.V1.Sys
{
    [AdminApiKey]
    [ApiController]
    [Route("api/v1/System/Users")]
    public class SystemUsersController : ControllerBase
    {
        readonly SystemSettings _settings;
        readonly Dictionary<string, DBCollection> _collections;
        readonly IDBCollection _sysDBCollection;
        public SystemUsersController(IDBCollection sysDBCollection, Dictionary<string, DBCollection> collections, SystemSettings settings)
        {
            _sysDBCollection = sysDBCollection;
            _collections = collections;
            _settings = settings;
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
        [ProducesResponseType(typeof(MODBResponse<PagedList<SystemUser>>), 200)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 400)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 401)]
        public async Task<IActionResult> GetUsersAsync([FromQuery] FilterObjectList obj){
            Request.Headers.TryGetValue("result-type", out var resultType);
            try{
                var res = Utilities.StopWatch(() => _sysDBCollection.Get("users").Filter(obj.IndexName, obj.CompareOperator, obj.Value, obj.Page, obj.PageSize));
                return await Task.FromResult(OKMODBRecords(res.Result, res.ProcessingTime, resultType));
            }
            catch(ArgumentException ex){
                throw new Exceptions.ApplicationValidationErrorException(ex, HttpContext.TraceIdentifier);
            }
        }

        [HttpGet("{key}")]
        [ProducesResponseType(typeof(MODBResponse<SystemUser>), 200)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 401)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 404)]
        public async Task<IActionResult> GetUserAsync([FromRoute] string key)
        {
            try{
                Request.Headers.TryGetValue("result-type", out var resultType);
                var res = Utilities.StopWatch(() => _sysDBCollection.Get("users").Get(key));
                return await Task.FromResult(OKMODBRecord(res.Result, res.ProcessingTime, resultType));
            }
            catch(MO.MODB.Exceptions.KeyNotFoundException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.NotFound, System.Net.HttpStatusCode.NotFound.ToString(), ex.Message);
            }
        }
        
        [HttpPost]
        [ProducesResponseType(typeof(MODBResponse), 200)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ValidationError), 400)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 401)]
        public async Task<IActionResult> SetUserAsync([FromBody] SetUserObject obj){
            try{
                var res = Utilities.StopWatch(() => {
                    var dbCollection = obj.Collection.ToLower();
                    if(!_sysDBCollection.Get("collections").Exists(dbCollection)){
                        _sysDBCollection.Get("collections").Set(dbCollection, dbCollection);
                        _collections.TryAdd(dbCollection, new DBCollection(path: Path.Combine(_settings.Path.Concat<string>(new string[]{dbCollection}).ToArray())));
                    }
                    while(true){
                        var key = Guid.NewGuid().ToString();
                        if(!_sysDBCollection.Get("users").Exists(key)){
                            var user = new SystemUser(){
                                Key = key,
                                Name = obj.Name,
                                Collection = obj.Collection
                            };
                            _sysDBCollection.Get("users").Set(
                                key: key, 
                                value: System.Text.Json.JsonSerializer.Serialize(user), 
                                keyType: typeof(string).Name, 
                                new InsertIndexItem("name", obj.Name, typeof(string).Name),
                                new InsertIndexItem("collection", obj.Collection, typeof(string).Name));
                            return;
                        }
                        continue;
                    }
            });
            return await Task.FromResult(OKResult(res.ProcessingTime));
            }catch(ArgumentException ex){
                throw new Exceptions.ApplicationValidationErrorException(ex, HttpContext.TraceIdentifier);
            }
        }

        [HttpDelete("{key}")]
        [ProducesResponseType(typeof(MODBResponse), 200)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 401)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 404)]
        public async Task<IActionResult> DeleteUserAsync([FromRoute] string key)
        {
            try{
                var res = Utilities.StopWatch(() => {
                    _sysDBCollection.Get("users").Delete(key);
                });
                return await Task.FromResult(OKResult(res.ProcessingTime));
            }
            catch(MODB.Exceptions.KeyNotFoundException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.NotFound, System.Net.HttpStatusCode.NotFound.ToString(), ex.Message);
            }
        }
    }
}
