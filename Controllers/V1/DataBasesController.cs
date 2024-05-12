using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using MODB.Api.Attributes;
using MODB.FlatFileDB;
using MODB.Api.DTOs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace MODB.Api.Controllers.V1
{
    [ApiKey]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class DataBasesController : ControllerBase
    {
        readonly char[] KEY_ALLOWED_CHARS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_.-".ToArray();
        readonly ConcurrentDictionary<string, ConcurrentDictionary<string, FlatFileKeyValDB>> _dbs;
        readonly Settings _settings;
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
        public DataBasesController(ConcurrentDictionary<string, ConcurrentDictionary<string, FlatFileKeyValDB>> dbs, Settings settings)
        {
            _dbs = dbs;
            _settings = settings;
        }

        [HttpGet]
        [ProducesResponseType(typeof(MODBResponse<IEnumerable<string>>), 200)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 401)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 500)]
        public async Task<IActionResult> GetDBsAsync()
        {
            Request.Headers.TryGetValue("ApiKey", out var apikey);
            var res = Utilities.StopWatch(() => _dbs[apikey].Keys);
            return await Task.FromResult(OKResult(res.Result, res.ProcessingTime));
        }

        [HttpGet("{name}")]
        [ProducesResponseType(typeof(MODBResponse<DBInformation>), 200)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ValidationError), 400)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 401)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 404)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 500)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 503)]
        public async Task<IActionResult> GetDBAsync([FromRoute] string name)
        {
            Request.Headers.TryGetValue("ApiKey", out var apikey);
            try{
                var res = Utilities.StopWatch(() =>{
                        var clientsDB = _dbs[apikey];
                        if(!clientsDB.ContainsKey(name))
                            throw new Exceptions.KeyNotFoundException(name);
                        var db = clientsDB[name];
                        if(db.Status != DBStatus.READY)
                            throw new Exceptions.DBNotReadyException(name, db.Status.ToString());
                        return new DBInformation(){Name = name, Size = db.Size, Manifests = db.Config.NumberOfManifests, LastClean = db.LastClean};
                    });
                return await Task.FromResult(OKResult(res.Result, res.ProcessingTime));
            }catch(ArgumentException ex){
                throw new Exceptions.ApplicationValidationErrorException(ex, HttpContext.TraceIdentifier);
            }
            catch(Exceptions.KeyNotFoundException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.NotFound, System.Net.HttpStatusCode.NotFound.ToString(), ex.Message);
            }catch(Exceptions.DBNotReadyException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.ServiceUnavailable, System.Net.HttpStatusCode.ServiceUnavailable.ToString(), ex.Message);
            }
        }

        [HttpPost("{name}/clean")]
        [ProducesResponseType(typeof(MODBResponse<DBInformation>), 200)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ValidationError), 400)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 401)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 404)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 500)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 503)]
        public async Task<IActionResult> CleanDBAsync([FromRoute] string name)
        {
            Request.Headers.TryGetValue("ApiKey", out var apikey);
            try{
                var res = Utilities.StopWatch(() =>{
                        var clientsDB = _dbs[apikey];
                        if(!clientsDB.ContainsKey(name))
                            throw new Exceptions.KeyNotFoundException(name);
                        var db = clientsDB[name];
                        if(db.Status != DBStatus.READY)
                            throw new Exceptions.DBNotReadyException(name, db.Status.ToString());
                        var cloneDB = new FlatFileKeyValDB(Path.Combine(_settings.Path.Concat(new string[]{apikey, $"{db.Name}.cln"}).ToArray()), db.Config.NumberOfManifests, DBStatus.CLEANING);
                        clientsDB[name] = cloneDB;
                        db.Clone(cloneDB);
                        db.Delete();
                        cloneDB.Rename(name);
                        cloneDB.Delete();
                        db = new FlatFileKeyValDB(Path.Combine(_settings.Path.Concat(new string[]{apikey, $"{name}"}).ToArray()));
                        clientsDB[name] = db;
                    });
                return await Task.FromResult(OKResult(res.ProcessingTime));
            }catch(ArgumentException ex){
                throw new Exceptions.ApplicationValidationErrorException(ex, HttpContext.TraceIdentifier);
            }
            catch(Exceptions.KeyNotFoundException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.NotFound, System.Net.HttpStatusCode.NotFound.ToString(), ex.Message);
            }catch(Exceptions.DBNotReadyException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.ServiceUnavailable, System.Net.HttpStatusCode.ServiceUnavailable.ToString(), ex.Message);
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(MODBResponse), 200)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ValidationError), 400)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 401)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 500)]
        public async Task<IActionResult> CreateDBAsync([FromQuery] CreateDBQueryParams obj){
            Request.Headers.TryGetValue("ApiKey", out var apikey);
            try{
                var res = Utilities.StopWatch(() => {
                        ValidateKey(obj.Name);
                        var clientDBs = _dbs[apikey];
                        if(clientDBs.ContainsKey(obj.Name))
                            throw new Exceptions.UniqueKeyConstraintException(obj.Name);
                        _dbs[apikey].TryAdd(obj.Name, new FlatFileKeyValDB(Path.Combine(_settings.Path.Concat(new string[]{apikey, obj.Name}).ToArray()), obj.Manifests));
                    });
                return await Task.FromResult(OKResult(res.ProcessingTime));
            }catch(ArgumentException ex){
                throw new Exceptions.ApplicationValidationErrorException(ex, HttpContext.TraceIdentifier);
            }
            catch(Exceptions.UniqueKeyConstraintException ex){
                throw new Exceptions.ApplicationValidationErrorException(new ArgumentException(ex.Message, paramName: nameof(obj.Name)), HttpContext.TraceIdentifier);
            }catch(Exceptions.KeyNotFoundException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.NotFound, System.Net.HttpStatusCode.NotFound.ToString(), ex.Message);
            }
        }

        [HttpPost("{db}/Keys")]
        [ProducesResponseType(typeof(MODBResponse), 200)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ValidationError), 400)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 401)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 404)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 500)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 503)]
        public async Task<IActionResult> SetKeyAsync([FromRoute] string db, [FromQuery] SetKeyQueryParams obj, [FromBody] Stream stream){
            Request.Headers.TryGetValue("ApiKey", out var apikey);
            try{
                var res = Utilities.StopWatch(() => {
                        var clientDBs = _dbs[apikey];
                        if(!clientDBs.ContainsKey(db) && obj.CreateDb == true)
                            _dbs[apikey].TryAdd(db, new FlatFileKeyValDB(Path.Combine(_settings.Path.Concat(new string[]{apikey, db}).ToArray())));
                        if(!clientDBs.ContainsKey(db))
                            throw new Exceptions.KeyNotFoundException(db);
                        var database = _dbs[apikey][db];
                        if(database.Status != DBStatus.READY)
                            throw new Exceptions.DBNotReadyException(db, database.Status.ToString());
                        database.Set(obj.Key, stream, obj.Tags, obj.TimeStamp);
                    });
                return await Task.FromResult(OKResult(res.ProcessingTime));
            }catch(ArgumentException ex){
                throw new Exceptions.ApplicationValidationErrorException(ex, HttpContext.TraceIdentifier);
            }
            catch(Exceptions.KeyNotFoundException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.NotFound, System.Net.HttpStatusCode.NotFound.ToString(), ex.Message);
            }catch(Exceptions.DBNotReadyException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.ServiceUnavailable, System.Net.HttpStatusCode.ServiceUnavailable.ToString(), ex.Message);
            }
        }

        [HttpPost("{db}/Keys/seed")]
        [ProducesResponseType(typeof(MODBResponse), 200)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ValidationError), 400)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 401)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 404)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 500)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 503)]
        public async Task<IActionResult> SeedAsync([FromRoute] string db, [FromQuery] SetKeyQueryParams obj){
            Request.Headers.TryGetValue("ApiKey", out var apikey);
            try{
                var res = Utilities.StopWatch(() => {
                        var clientDBs = _dbs[apikey];
                        if(!clientDBs.ContainsKey(db) && obj.CreateDb == true)
                            _dbs[apikey].TryAdd(db, new FlatFileKeyValDB(Path.Combine(_settings.Path.Concat(new string[]{apikey, db}).ToArray())));
                        if(!clientDBs.ContainsKey(db))
                            throw new Exceptions.KeyNotFoundException(db);
                        var database = _dbs[apikey][db];
                        if(database.Status != DBStatus.READY)
                            throw new Exceptions.DBNotReadyException(db, database.Status.ToString());
                        for(int i = 1; i<= 1000; i ++){
                            database.Set($"{i}", $"{i}  seed ljhnoiadhsio joaiosdjhfio asioj hsiao jhihsjfioddddddddddddddddddddddddddddoijoijio joij oij osijio sjaijfoisjfoijsdoifjiosjafoijsaofijsaoifjoasjfoi j asoifjoisaj foisajfoijasdofjdoasifjoiasjdfoijasofidjoaiejfioajsdifjm kjsfidjoidsaj ojfsioj dsoifjiosdajfiojsdaiofjioasdjfoijasiojeriofjoiasjeiofje8ijfioajndsoijmnfinmjsaio jiojsoaifjiojaioejsiofjioasjdiofji asjfiodasjfiojs aiofjio asjd iofjsadoijfio dsjaoifjoiejiofjiojsaoisjfoi jasoifisdjaiofjdsoiajfioajeiojfoijasoi jfioasjfdioj fijasoidfjiowsejoifjasiofjdios jfoijsa oifjsioejfiojasiofjiof{i}", obj.Tags, obj.TimeStamp);
                        }
                    });
                return await Task.FromResult(OKResult(res.ProcessingTime));
            }catch(ArgumentException ex){
                throw new Exceptions.ApplicationValidationErrorException(ex, HttpContext.TraceIdentifier);
            }
            catch(Exceptions.KeyNotFoundException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.NotFound, System.Net.HttpStatusCode.NotFound.ToString(), ex.Message);
            }catch(Exceptions.DBNotReadyException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.ServiceUnavailable, System.Net.HttpStatusCode.ServiceUnavailable.ToString(), ex.Message);
            }
        }

        [HttpGet("{db}/Keys")]
        [ProducesResponseType(typeof(MODBResponse<PagedList<string>>), 200)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ValidationError), 400)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 404)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 401)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 500)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 503)]
        public async Task<IActionResult> GetKeysAsync([FromRoute] string db, [FromQuery] GetFilteredQueryParams obj){
            Request.Headers.TryGetValue("ApiKey", out var apikey);
            try{
                var res = Utilities.StopWatch(() => {
                        var clientDBs = _dbs[apikey];
                        if(!clientDBs.ContainsKey(db))
                            throw new Exceptions.KeyNotFoundException(db);
                        var database = _dbs[apikey][db];
                        if(database.Status != DBStatus.READY)
                            throw new Exceptions.DBNotReadyException(db, database.Status.ToString());
                        return database.GetKeys(obj.Tags, obj.From, obj.To, obj.Page, obj.PageSize);
                    });
                return await Task.FromResult(OKResult(res.Result, res.ProcessingTime));
            }catch(ArgumentException ex){
                throw new Exceptions.ApplicationValidationErrorException(ex, HttpContext.TraceIdentifier);
            }
            catch(Exceptions.KeyNotFoundException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.NotFound, System.Net.HttpStatusCode.NotFound.ToString(), ex.Message);
            }catch(Exceptions.DBNotReadyException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.ServiceUnavailable, System.Net.HttpStatusCode.ServiceUnavailable.ToString(), ex.Message);
            }
        }

        [HttpGet("{db}/Values")]
        [ProducesResponseType(typeof(MODBResponse<PagedList<string>>), 200)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ValidationError), 400)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 404)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 401)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 500)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 503)]
        public async Task<IActionResult> GetValuesAsync([FromRoute] string db, [FromQuery] GetFilteredQueryParams obj){
            Request.Headers.TryGetValue("ApiKey", out var apikey);
            Request.Headers.TryGetValue("result-type", out var resultType);
            try{
                var res = Utilities.StopWatch(() => {
                        var clientDBs = _dbs[apikey];
                        if(!clientDBs.ContainsKey(db))
                            throw new Exceptions.KeyNotFoundException(db);
                        var database = _dbs[apikey][db];
                        if(database.Status != DBStatus.READY)
                            throw new Exceptions.DBNotReadyException(db, database.Status.ToString());
                        return database.Get(obj.Tags, obj.From, obj.To, obj.Page, obj.PageSize);
                    });
                return await Task.FromResult(OKMODBRecords(res.Result, res.ProcessingTime, resultType));
            }catch(ArgumentException ex){
                throw new Exceptions.ApplicationValidationErrorException(ex, HttpContext.TraceIdentifier);
            }
            catch(Exceptions.KeyNotFoundException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.NotFound, System.Net.HttpStatusCode.NotFound.ToString(), ex.Message);
            }catch(Exceptions.DBNotReadyException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.ServiceUnavailable, System.Net.HttpStatusCode.ServiceUnavailable.ToString(), ex.Message);
            }
        }

        [HttpGet("{db}/Keys/{key}")]
        [ProducesResponseType(typeof(MODBResponse<string>), 200)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ValidationError), 400)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 404)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 401)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 500)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 503)]
        public async Task<IActionResult> GetKeyAsync([FromRoute] string db, [FromRoute] string key){
            Request.Headers.TryGetValue("ApiKey", out var apikey);
            Request.Headers.TryGetValue("result-type", out var resultType);
            try{
                var res = Utilities.StopWatch(() => {
                        var clientDBs = _dbs[apikey];
                        if(!clientDBs.ContainsKey(db))
                            throw new Exceptions.KeyNotFoundException(db);
                        var database = _dbs[apikey][db];
                        if(database.Status != DBStatus.READY)
                            throw new Exceptions.DBNotReadyException(db, database.Status.ToString());
                        return database.Get(key);
                    });
                return await Task.FromResult(OKMODBRecord(res.Result, res.ProcessingTime, resultType));
            }catch(ArgumentException ex){
                throw new Exceptions.ApplicationValidationErrorException(ex, HttpContext.TraceIdentifier);
            }
            catch(FlatFileDB.Exceptions.KeyNotFoundException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.NotFound, System.Net.HttpStatusCode.NotFound.ToString(), ex.Message);
            }
            catch(Exceptions.KeyNotFoundException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.NotFound, System.Net.HttpStatusCode.NotFound.ToString(), ex.Message);
            }catch(Exceptions.DBNotReadyException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.ServiceUnavailable, System.Net.HttpStatusCode.ServiceUnavailable.ToString(), ex.Message);
            }
        }

        [HttpGet("{db}/Keys/{key}/exists")]
        [ProducesResponseType(typeof(MODBResponse<bool>), 200)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ValidationError), 400)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 401)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 500)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 503)]
        public async Task<IActionResult> KeyExistsAsync([FromRoute] string db, [FromRoute] string key){
            Request.Headers.TryGetValue("ApiKey", out var apikey);
            try{
                var res = Utilities.StopWatch(() => {
                        var clientDBs = _dbs[apikey];
                        if(!clientDBs.ContainsKey(db))
                            return false;
                        var database = _dbs[apikey][db];
                        if(database.Status != DBStatus.READY)
                            throw new Exceptions.DBNotReadyException(db, database.Status.ToString());
                        return database.Exists(key);
                    });
                return await Task.FromResult(OKResult(res.Result, res.ProcessingTime));
            }catch(ArgumentException ex){
                throw new Exceptions.ApplicationValidationErrorException(ex, HttpContext.TraceIdentifier);
            }catch(Exceptions.DBNotReadyException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.ServiceUnavailable, System.Net.HttpStatusCode.ServiceUnavailable.ToString(), ex.Message);
            }
        }

        [HttpDelete("{db}/Keys/{key}")]
        [ProducesResponseType(typeof(MODBResponse), 200)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ValidationError), 400)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 404)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 401)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 500)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 503)]
        public async Task<IActionResult> DeleteKeyAsync([FromRoute] string db, [FromRoute] string key){
            Request.Headers.TryGetValue("ApiKey", out var apikey);
            try{
                var res = Utilities.StopWatch(() => {
                        var clientDBs = _dbs[apikey];
                        if(!clientDBs.ContainsKey(db))
                            throw new Exceptions.KeyNotFoundException(db);
                        var database = _dbs[apikey][db];
                        if(database.Status != DBStatus.READY)
                            throw new Exceptions.DBNotReadyException(db, database.Status.ToString());
                        database.Delete(key);
                    });
                return await Task.FromResult(OKResult(res.ProcessingTime));
            }catch(ArgumentException ex){
                throw new Exceptions.ApplicationValidationErrorException(ex, HttpContext.TraceIdentifier);
            }
            catch(Exceptions.KeyNotFoundException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.NotFound, System.Net.HttpStatusCode.NotFound.ToString(), ex.Message);
            }catch(Exceptions.DBNotReadyException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.ServiceUnavailable, System.Net.HttpStatusCode.ServiceUnavailable.ToString(), ex.Message);
            }
        }

        [HttpGet("{db}/Tags")]
        [ProducesResponseType(typeof(MODBResponse<PagedList<string>>), 200)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ValidationError), 400)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 404)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 401)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 500)]
        [ProducesResponseType(typeof(ConsistentApiResponseErrors.ConsistentErrors.ExceptionError), 503)]
        public async Task<IActionResult> GetTagsAsync([FromRoute] string db, [FromQuery] GetTagsFilteredQueryParams obj){
            Request.Headers.TryGetValue("ApiKey", out var apikey);
            try{
                var res = Utilities.StopWatch(() => {
                        var clientDBs = _dbs[apikey];
                        if(!clientDBs.ContainsKey(db))
                            throw new Exceptions.KeyNotFoundException(db);
                        var database = _dbs[apikey][db];
                        if(database.Status != DBStatus.READY)
                            throw new Exceptions.DBNotReadyException(db, database.Status.ToString());
                        return database.GetTags(obj.Text, obj.Page, obj.PageSize);
                    });
                return await Task.FromResult(OKResult(res.Result, res.ProcessingTime));
            }catch(ArgumentException ex){
                throw new Exceptions.ApplicationValidationErrorException(ex, HttpContext.TraceIdentifier);
            }
            catch(Exceptions.KeyNotFoundException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.NotFound, System.Net.HttpStatusCode.NotFound.ToString(), ex.Message);
            }catch(Exceptions.DBNotReadyException ex){
                throw new Exceptions.ApplicationErrorException((int)System.Net.HttpStatusCode.ServiceUnavailable, System.Net.HttpStatusCode.ServiceUnavailable.ToString(), ex.Message);
            }
        }

        bool ValidateKey(string key) => string.IsNullOrEmpty(key) || key.Any(x => !KEY_ALLOWED_CHARS.Contains(x)) ? throw new ArgumentException($"{key} is not a valid key. keys must match ^[a-zA-Z0-9_.-]+$", nameof(key)) : true;
    }
}
