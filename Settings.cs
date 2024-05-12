using System.Collections.Generic;

namespace MODB.Api{
    public class Settings{
        public string ApiKey {get; set;}
        public IEnumerable<string> Path {get; set;}
    }
}