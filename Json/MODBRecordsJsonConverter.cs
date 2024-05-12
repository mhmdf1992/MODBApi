using System;
using System.Text.Json;
using MODB.FlatFileDB;

namespace MODB.Api.Json{
    public class MODBRecordsJsonConverter : System.Text.Json.Serialization.JsonConverter<PagedList<string>>
    {
        public override PagedList<string> Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) =>
                new PagedList<string>();

        public override void Write(
            Utf8JsonWriter writer,
            PagedList<string> value,
            JsonSerializerOptions options){
                var opt = new JsonSerializerOptions();
                opt.Converters.Add(new MODBRecordJsonConverter());
                writer.WriteRawValue(JsonSerializer.Serialize(value, options: opt));
            }
    }
}