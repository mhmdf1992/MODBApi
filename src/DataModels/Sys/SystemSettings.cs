using System.Collections.Generic;

namespace MO.MODBApi.DataModels.Sys{
    public class SystemSettings{
        public string ApiKey {get; set;}
        public IEnumerable<string> Path {get; set;}
    }
}