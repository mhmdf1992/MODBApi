using MO.MODB;
using MO.MODBApi.DataModels.Sys;

namespace MO.MODBApi{
    public static class Extensions{
        public static string ExtractUserCollection(this Microsoft.AspNetCore.Http.IHeaderDictionary header, IDBCollection sysCollection){
            header.TryGetValue("ApiKey", out var apikey);
            var user = System.Text.Json.JsonSerializer.Deserialize<SystemUser>(sysCollection.Get("users").Get(apikey.ToString()));
            return user.Collection;
        }
    }
}