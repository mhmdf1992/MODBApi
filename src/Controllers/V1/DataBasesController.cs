using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using MO.MODBApi.Attributes;
using MO.MODBApi.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using MO.MODB;

namespace MO.MODBApi.Controllers.V1
{
    [ApiKey]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class DataBasesController : ControllerBase
    {
        readonly IDBCollection _sysCollection;
        readonly Dictionary<string, DBCollection> _collections;
        OkObjectResult OKResult<T> (T res, string processingTime){
            Response.Headers.Add("processing-time", processingTime);
           return Ok(new MODBResponse<T>(res));
        }
        OkObjectResult OKMODBRecord(string res, string processingTime, string resultType){
            Response.Headers.Add("processing-time", processingTime);
            Response.Headers.Add("result-type", resultType);
           return Ok(resultType == "json" ? new MODBRecordJsonResponse(res) : new MODBRecordResponse(res));
        }
        OkObjectResult OKMODBRecords(PagedList<string> res, string processingTime, string resultType){
            Response.Headers.Add("processing-time", processingTime);
            Response.Headers.Add("result-type", resultType);
           return Ok(resultType == "json" ? new MODBRecordsJsonResponse(res) : new MODBRecordsResponse(res));
        }
        OkObjectResult OKResult (string processingTime){
            Response.Headers.Add("processing-time", processingTime);
            return Ok(new MODBResponse());
        }
        public DataBasesController(IDBCollection sysCollection, Dictionary<string, DBCollection> collections)
        {
            _sysCollection = sysCollection;
            _collections = collections;
        }

        [HttpGet]
        [ProducesResponseType(typeof(MODBResponse<IEnumerable<string>>), 200)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 401)]
        public async Task<IActionResult> GetDBsAsync()
        {
            var collection = Request.Headers.ExtractUserCollection(_sysCollection);
            var res = Utilities.StopWatch(() => _collections[collection].All().Select(db => db.Name));
            return await Task.FromResult(OKResult(res.Result, res.ProcessingTime));
        }

        [HttpGet("{name}")]
        [ProducesResponseType(typeof(MODBResponse<DBInformation>), 200)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ValidationError), 400)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 401)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 404)]
        public async Task<IActionResult> GetDBAsync([FromRoute] string name)
        {
            var collection = Request.Headers.ExtractUserCollection(_sysCollection);
            try{
                var res = Utilities.StopWatch(() =>{
                        var db = _collections[collection].Get(name);
                        return new DBInformation(){Name = db.Name, Size = db.Size, Indices = db.Indexes};
                    });
                return await Task.FromResult(OKResult(res.Result, res.ProcessingTime));
            }catch(ArgumentException ex){
                throw new Exceptions.ApplicationValidationErrorException(ex, HttpContext.TraceIdentifier);
            }
            catch(MODB.Exceptions.DBNotFoundException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.NotFound, System.Net.HttpStatusCode.NotFound.ToString(), ex.Message);
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(MODBResponse), 200)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ValidationError), 400)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 401)]
        public async Task<IActionResult> CreateDBAsync([FromBody] CreateDBObject obj){
            var collection = Request.Headers.ExtractUserCollection(_sysCollection);
            try{
                var res = Utilities.StopWatch(() => {
                        if(_collections[collection].Exists(obj.Name))
                            throw new ArgumentException(message:$"Database {obj.Name} already exists.", paramName: "name");
                        _collections[collection].Get(obj.Name, true);
                    });
                return await Task.FromResult(OKResult(res.ProcessingTime));
            }catch(ArgumentException ex){
                throw new Exceptions.ApplicationValidationErrorException(ex, HttpContext.TraceIdentifier);
            }
        }

        [HttpPost("{name}/values")]
        [ProducesResponseType(typeof(MODBResponse), 200)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ValidationError), 400)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 401)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 404)]
        public async Task<IActionResult> SetAsync([FromRoute] string name, [FromBody] SetObject obj)
        {
            var collection = Request.Headers.ExtractUserCollection(_sysCollection);
            try{
                var res = Utilities.StopWatch(() =>{
                        _collections[collection].Get(name, obj.CreateDb)
                            .Set(obj.Key, obj.Value, obj.Type, 
                             index: obj.Indices == null || !obj.Indices.Any() ? null : obj.Indices.Select(x => new InsertIndexItem(x.Name, x.Value, x.Type)).ToArray());
                    });
                return await Task.FromResult(OKResult(res.ProcessingTime));
            }catch(ArgumentException ex){
                throw new Exceptions.ApplicationValidationErrorException(ex, HttpContext.TraceIdentifier);
            }
            catch(MODB.Exceptions.DBNotFoundException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.NotFound, System.Net.HttpStatusCode.NotFound.ToString(), ex.Message);
            }
        }

        [HttpDelete("{name}")]
        [ProducesResponseType(typeof(MODBResponse), 200)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ValidationError), 400)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 401)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 404)]
        public async Task<IActionResult> DeleteDBAsync([FromRoute] string name)
        {
            var collection = Request.Headers.ExtractUserCollection(_sysCollection);
            try{
                var res = Utilities.StopWatch(() =>{
                        _collections[collection].Delete(name);
                    });
                return await Task.FromResult(OKResult(res.ProcessingTime));
            }catch(ArgumentException ex){
                throw new Exceptions.ApplicationValidationErrorException(ex, HttpContext.TraceIdentifier);
            }
            catch(MODB.Exceptions.DBNotFoundException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.NotFound, System.Net.HttpStatusCode.NotFound.ToString(), ex.Message);
            }
        }

        [HttpGet("{name}/values/{key}")]
        [ProducesResponseType(typeof(MODBResponse<string>), 200)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ValidationError), 400)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 401)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 404)]
        public async Task<IActionResult> GetAsync([FromRoute] string name, [FromRoute] string key)
        {
            var collection = Request.Headers.ExtractUserCollection(_sysCollection);
            try{
                var res = Utilities.StopWatch(() =>{
                        var db = _collections[collection].Get(name);
                        return db.Get(key);
                    });
                return await Task.FromResult(OKResult(res.Result, res.ProcessingTime));
            }catch(ArgumentException ex){
                throw new Exceptions.ApplicationValidationErrorException(ex, HttpContext.TraceIdentifier);
            }
            catch(MODB.Exceptions.DBNotFoundException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.NotFound, System.Net.HttpStatusCode.NotFound.ToString(), ex.Message);
            }catch(MODB.Exceptions.KeyNotFoundException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.NotFound, System.Net.HttpStatusCode.NotFound.ToString(), ex.Message);
            }
        }

        [HttpDelete("{name}/values/{key}")]
        [ProducesResponseType(typeof(MODBResponse), 200)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ValidationError), 400)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 401)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 404)]
        public async Task<IActionResult>  DeleteAsync([FromRoute] string name, [FromRoute] string key)
        {
            var collection = Request.Headers.ExtractUserCollection(_sysCollection);
            try{
                var res = Utilities.StopWatch(() =>{
                        var db = _collections[collection].Get(name);
                        db.Delete(key);
                    });
                return await Task.FromResult(OKResult(res.ProcessingTime));
            }catch(ArgumentException ex){
                throw new Exceptions.ApplicationValidationErrorException(ex, HttpContext.TraceIdentifier);
            }
            catch(MODB.Exceptions.DBNotFoundException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.NotFound, System.Net.HttpStatusCode.NotFound.ToString(), ex.Message);
            }catch(MODB.Exceptions.KeyNotFoundException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.NotFound, System.Net.HttpStatusCode.NotFound.ToString(), ex.Message);
            }
        }

        [HttpGet("{name}/filter")]
        [ProducesResponseType(typeof(MODBResponse<PagedList<string>>), 200)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ValidationError), 400)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 401)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 404)]
        public async Task<IActionResult> FilterAsync([FromRoute] string name, [FromQuery] FilterObjectList obj){
            var collection = Request.Headers.ExtractUserCollection(_sysCollection);
            Request.Headers.TryGetValue("result-type", out var resultType);
            try{
                var res = Utilities.StopWatch(() => {
                    var db = _collections[collection].Get(name);
                    return db.Filter(obj.IndexName, obj.CompareOperator, obj.Value, obj.Page, obj.PageSize);
                });
                return await Task.FromResult(OKMODBRecords(res.Result, res.ProcessingTime, resultType));
            }catch(MODB.Exceptions.DBNotFoundException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.NotFound, System.Net.HttpStatusCode.NotFound.ToString(), ex.Message);
            }catch(ArgumentException ex){
                throw new Exceptions.ApplicationValidationErrorException(ex, HttpContext.TraceIdentifier);
            }
            
        }

        [HttpGet("{name}/count")]
        [ProducesResponseType(typeof(MODBResponse<int>), 200)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ValidationError), 400)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 401)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 404)]
        public async Task<IActionResult> CountAsync([FromRoute] string name, [FromQuery] FilterObject obj)
        {
            var collection = Request.Headers.ExtractUserCollection(_sysCollection);
            try{
                var res = Utilities.StopWatch(() =>{
                        var db = _collections[collection].Get(name, false);
                        return db.Count(obj.IndexName, obj.CompareOperator, obj.Value);
                    });
                return await Task.FromResult(OKResult(res.Result, res.ProcessingTime));
            }catch(ArgumentException ex){
                throw new Exceptions.ApplicationValidationErrorException(ex, HttpContext.TraceIdentifier);
            }
            catch(MODB.Exceptions.DBNotFoundException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.NotFound, System.Net.HttpStatusCode.NotFound.ToString(), ex.Message);
            }
        }

        [HttpGet("{name}/any")]
        [ProducesResponseType(typeof(MODBResponse<bool>), 200)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ValidationError), 400)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 401)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 404)]
        public async Task<IActionResult> AnyAsync([FromRoute] string name, [FromQuery] FilterObject obj)
        {
            var collection = Request.Headers.ExtractUserCollection(_sysCollection);
            try{
                var res = Utilities.StopWatch(() =>{
                        var db = _collections[collection].Get(name, false);
                        return db.Any(obj.IndexName, obj.CompareOperator, obj.Value);
                    });
                return await Task.FromResult(OKResult(res.Result, res.ProcessingTime));
            }catch(ArgumentException ex){
                throw new Exceptions.ApplicationValidationErrorException(ex, HttpContext.TraceIdentifier);
            }
            catch(MODB.Exceptions.DBNotFoundException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.NotFound, System.Net.HttpStatusCode.NotFound.ToString(), ex.Message);
            }
        }
    }
}
