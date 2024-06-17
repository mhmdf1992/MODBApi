using System.Collections.Generic;
using MO.MODB;
using MO.MODBApi.Json;

namespace MO.MODBApi.DTOs{
    public class FilterObjectList : FilterObject{
        public int Page {get; set;} = 1;
        public int PageSize {get; set;} = 10;
    }

    public class FilterObject{
        public string IndexName {get; set;}
        public CompareOperators? CompareOperator {get; set;}
        public string Value {get; set;}
    }

    public class SetUserObject{
        public string Name {get; set;}
        public string Collection {get; set;}
    }

    public class SetObject{
        public string Key {get; set;}
        public string Type {get; set;}
        public string Value {get; set;}
        public IEnumerable<SetObjectIndexItem> Indices {get; set;}
        public bool CreateDb {get; set;} = false;
    }
    
    public class SetObjectIndexItem{
        public string Name {get; set;}
        public string Value {get; set;}
        public string Type {get; set;}
    }
    
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
        public IEnumerable<Index> Indices {get; set;}
    }

    public class CreateDBObject{
        public string Name {get; set;} 
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