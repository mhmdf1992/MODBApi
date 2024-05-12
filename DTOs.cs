using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MODB.Api.Json;
using MODB.FlatFileDB;
using Newtonsoft.Json;

namespace MODB.Api.DTOs{
    public class DBResponse<T>{
        public T Result {get; set;}
        public string ProcessingTime {get; set;}
    }

    public class DBResponse{
        public string ProcessingTime {get; set;}
    }

    public class DBInformation{
        public string Name {get; set;}
        public long Size {get; set;}
        public int Manifests {get; set;}
        public string LastClean {get; set;}
    }

    public class CreateDBQueryParams{
        [Required(AllowEmptyStrings = false)] public string Name {get; set;} 
        public int? Manifests {get; set;}
    }
    public class SetKeyQueryParams{
        [Required(AllowEmptyStrings = false)] public string Key {get; set;}
        public IEnumerable<string> Tags {get; set;}
        public long? TimeStamp {get; set;}
        public bool? CreateDb {get; set;}
    }
    public class GetQueryParams{
        public int Page {get; set;} = 1;
        public int PageSize {get; set;} = 10;
    }

    public class GetTagsFilteredQueryParams : GetQueryParams{
        public string Text {get; set;}
    }

    public class GetFilteredQueryParams : GetQueryParams{
        public IEnumerable<string> Tags {get; set;}
        public long? From {get; set;}
        public long? To {get; set;}
    }

    public class MODBRecordsResponse{
        public MODBRecordsResponse(PagedList<string> result){
            Result = result;
        }
        public MODBRecordsResponse(){}
        public int StatusCode {get; set;} = (int)System.Net.HttpStatusCode.OK;
        public string StatusMessage {get; set;} = System.Net.HttpStatusCode.OK.ToString();
        public PagedList<string> Result {get; set;}
    }

    public class MODBRecordResponse{
        public MODBRecordResponse(string result){
            Result = result;
        }
        public MODBRecordResponse(){}
        public int StatusCode {get; set;} = (int)System.Net.HttpStatusCode.OK;
        public string StatusMessage {get; set;} = System.Net.HttpStatusCode.OK.ToString();
        public string Result {get; set;}
    }

    public class MODBRecordsJsonResponse{
        public MODBRecordsJsonResponse(PagedList<string> result){
            Result = result;
        }
        public MODBRecordsJsonResponse(){}
        public int StatusCode {get; set;} = (int)System.Net.HttpStatusCode.OK;
        public string StatusMessage {get; set;} = System.Net.HttpStatusCode.OK.ToString();
        [System.Text.Json.Serialization.JsonConverter(typeof(MODBRecordsJsonConverter))]
        public PagedList<string> Result {get; set;}
    }

    public class MODBRecordJsonResponse{
        public MODBRecordJsonResponse(string result){
            Result = result;
        }
        public MODBRecordJsonResponse(){}
        public int StatusCode {get; set;} = (int)System.Net.HttpStatusCode.OK;
        public string StatusMessage {get; set;} = System.Net.HttpStatusCode.OK.ToString();
        [System.Text.Json.Serialization.JsonConverter(typeof(MODBRecordJsonConverter))]
        public string Result {get; set;}
    }

    public class MODBResponse{
        public int StatusCode {get; set;} = (int)System.Net.HttpStatusCode.OK;
        public string StatusMessage {get; set;} = System.Net.HttpStatusCode.OK.ToString();
    }
    public class MODBResponse<T> : MODBResponse{
        public MODBResponse(T result){
            Result = result;
        }
        public MODBResponse(){}
        public T Result {get; set;}
    }

    public class Error
    {
        public object Code { get; set; }
        public string Field { get; set; }
        public object AttemptedValue { get; set; }
        public string Message { get; set; }
        public string HelpURL { get; set; }
    }
}