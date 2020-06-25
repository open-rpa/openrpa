using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.OpenFlowDB
{
    public class DataRowConverter : JsonConverter<DataRow>
    {
        public override DataRow ReadJson(JsonReader reader, Type objectType, DataRow existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException(string.Format("{0} is only implemented for writing.", this));
        }
        public override void WriteJson(JsonWriter writer, DataRow row, JsonSerializer serializer)
        {
            var table = row.Table;
            if (table == null)
                throw new JsonSerializationException("no table");
            var contractResolver = serializer.ContractResolver as DefaultContractResolver;

            writer.WriteStartObject();
            foreach (DataColumn col in row.Table.Columns)
            {
                var value = row[col];

                if (serializer.NullValueHandling == NullValueHandling.Ignore && (value == null || value == DBNull.Value))
                    continue;

                writer.WritePropertyName(contractResolver != null ? contractResolver.GetResolvedPropertyName(col.ColumnName) : col.ColumnName);
                serializer.Serialize(writer, value);
            }
            writer.WriteEndObject();
        }
    }
}
