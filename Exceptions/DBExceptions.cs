namespace MODB.Api.Exceptions{
    public class KeyNotFoundException : System.Exception{
        protected string _key;
        public string Key => _key;
        public KeyNotFoundException(string key): base($"Key {key} does not exist"){
            _key = key;
        }
    }

    public class UniqueKeyConstraintException : System.Exception{
        protected string _key;
        public string Key => _key;
        public UniqueKeyConstraintException(string key): base($"Key {key} already exist"){
            _key = key;
        }
    }

    public class DBNotReadyException : System.Exception{
        protected string _name;
        protected string _status;
        public string Status => _status;
        public string Name => _name;
        public DBNotReadyException(string name, string status): base($"DB {name} is not ready. Current status {status}..."){
            _name = name;
            _status = status;
        }
    }
}